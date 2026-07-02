using System.Globalization;
using Glinter.Modules.Experiences.Application.Abstractions;
using Glinter.Modules.Experiences.Application.Dtos;
using Glinter.Modules.Experiences.Domain.Enums;
using Glinter.Modules.Itineraries.Application.Abstractions;
using Glinter.Modules.Itineraries.Application.Dtos;
using Glinter.Modules.Itineraries.Application.Options;
using Glinter.Modules.Itineraries.Domain.Enums;
using Glinter.Modules.Regions.Application.Abstractions;
using Microsoft.Extensions.Options;

namespace Glinter.Modules.Itineraries.Application.Services;

public class ItineraryPlannerService : IItineraryPlannerService
{
    private readonly IExperienceRecommendationService _experienceRecommendationService;
    private readonly IItineraryRoutePlanner _routePlanner;
    private readonly IItineraryGroqClient _groqClient;
    private readonly IRegionRecommendationReadService _regionReadService;
    private readonly ItineraryPlanningOptions _options;
    private readonly ILogger<ItineraryPlannerService> _logger;

    public ItineraryPlannerService(
        IExperienceRecommendationService experienceRecommendationService,
        IItineraryRoutePlanner routePlanner,
        IItineraryGroqClient groqClient,
        IRegionRecommendationReadService regionReadService,
        IOptions<ItineraryPlanningOptions> options,
        ILogger<ItineraryPlannerService> logger)
    {
        _experienceRecommendationService = experienceRecommendationService;
        _routePlanner = routePlanner;
        _groqClient = groqClient;
        _regionReadService = regionReadService;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<ItineraryPlanResponse> PlanAsync(
        ItineraryPlanRequest request,
        CancellationToken cancellationToken)
    {
        await ResolveOriginAsync(request, cancellationToken);
        var normalized = NormalizeRequest(request);
        var startDate = normalized.StartDate ?? normalized.Date!.Value;
        var endDate = normalized.EndDate ?? startDate;
        var dayCount = endDate.DayNumber - startDate.DayNumber + 1;
        if (dayCount < 1 || dayCount > Math.Clamp(_options.MaxTripDays, 1, 31))
        {
            throw new ArgumentException(
                $"Trip duration must be between 1 and {Math.Clamp(_options.MaxTripDays, 1, 31)} days.");
        }

        var days = new List<ItineraryDayResponse>(dayCount);
        var usedExperienceIds = new HashSet<int>();
        decimal knownTripCost = 0;
        var remainingBudget = normalized.TotalBudget;

        for (var dayIndex = 0; dayIndex < dayCount; dayIndex++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var dayRequest = CloneForDate(normalized, startDate.AddDays(dayIndex));
            decimal? dayBudget = remainingBudget is null
                ? null
                : remainingBudget.Value / Math.Max(1, dayCount - dayIndex);
            var day = await PlanDayAsync(
                dayRequest,
                usedExperienceIds,
                dayBudget,
                cancellationToken);
            day.DayNumber = dayIndex + 1;
            days.Add(day);
            if (normalized.Start.Label == PendingFirstStopOrigin &&
                day.Stops.Count > 0)
            {
                normalized.Start = new ItineraryPointRequest
                {
                    Latitude = day.Stops[0].Latitude,
                    Longitude = day.Stops[0].Longitude,
                    Label = "First itinerary stop fallback"
                };
            }
            foreach (var stop in day.Stops)
            {
                if (stop.ExperienceId is not null)
                    usedExperienceIds.Add(stop.ExperienceId.Value);
            }
            if (day.EstimatedCost is not null)
            {
                knownTripCost += day.EstimatedCost.Value;
                remainingBudget = remainingBudget is null
                    ? null
                    : Math.Max(0, remainingBudget.Value - day.EstimatedCost.Value);
            }
        }

        var warnings = days.SelectMany(day => day.Warnings).Distinct().ToList();
        var budgetApplied = normalized.TotalBudget is not null || normalized.BudgetLevel is not null;
        var withinBudget = normalized.TotalBudget is null
            ? (bool?)null
            : knownTripCost <= normalized.TotalBudget.Value;
        if (normalized.TotalBudget is not null && days.Sum(day => day.SelectedStopsCount) == 0)
            warnings.Add("No feasible itinerary satisfied the supplied trip budget.");

        var firstDay = days[0];
        return new ItineraryPlanResponse
        {
            Date = firstDay.Date,
            StartDate = startDate,
            EndDate = endDate,
            Destination = normalized.Destination,
            Origin = normalized.Start,
            OriginSource = normalized.Start.Label ?? "Explicit origin",
            TravelMode = normalized.TravelMode,
            FallbackTravelMode = normalized.FallbackTravelMode,
            Pace = normalized.Pace,
            TotalCandidateExperiences = days.Sum(day => day.TotalCandidateExperiences),
            SelectedStopsCount = days.Sum(day => day.SelectedStopsCount),
            TotalDurationMinutes = days.Sum(day => day.TotalDurationMinutes),
            TotalTravelMinutes = days.Sum(day => day.TotalTravelMinutes),
            TotalDistanceKm = Round(days.Sum(day => day.TotalDistanceKm)),
            Score = days.Count == 0 ? 0 : Round(days.Average(day => day.Score)),
            EstimatedTotalCost = knownTripCost,
            UnknownPriceStops = days.Sum(day => day.UnknownPriceStops),
            TotalBudget = normalized.TotalBudget,
            Currency = normalized.Currency ?? "USD",
            BudgetApplied = budgetApplied,
            IsWithinBudget = withinBudget,
            Warnings = warnings,
            Stops = firstDay.Stops,
            Legs = firstDay.Legs,
            Explanation = new ItineraryExplanationResponse
            {
                Summary = days.Count == 1
                    ? firstDay.Explanation.Summary
                    : $"Built {days.Count} days with {days.Sum(day => day.SelectedStopsCount)} unique experience stops.",
                Reasons = days.SelectMany(day => day.Explanation.Reasons).Distinct().Take(5).ToList(),
                IsAiGenerated = days.Any(day => day.Explanation.IsAiGenerated)
            },
            Days = days
        };
    }

    private async Task<ItineraryDayResponse> PlanDayAsync(
        ItineraryPlanRequest normalized,
        IReadOnlySet<int> excludedExperienceIds,
        decimal? dayBudget,
        CancellationToken cancellationToken)
    {
        var dayStart = normalized.Date!.Value.ToDateTime(normalized.DayStartLocal!.Value);
        var dayEnd = normalized.Date.Value.ToDateTime(normalized.DayEndLocal!.Value);

        var recommendationRequest = BuildRecommendationRequest(normalized, dayStart);
        var recommendations = await _experienceRecommendationService.RecommendAsync(
            recommendationRequest,
            cancellationToken);

        var warnings = new List<string>();
        if (recommendations.TotalCandidates == 0 || recommendations.Items.Count == 0)
        {
            warnings.Add("No recommended experiences matched the itinerary constraints.");
            return BuildEmptyDayResponse(normalized, warnings);
        }

        var duplicateFreeRecommendations = recommendations.Items
            .Where(item => !excludedExperienceIds.Contains(item.ExperienceId))
            .ToList();
        var eligibleRecommendations = ApplyBudgetLevel(
            duplicateFreeRecommendations,
            normalized.BudgetLevel);
        if (normalized.BudgetLevel is not null &&
            eligibleRecommendations.Count < duplicateFreeRecommendations.Count)
        {
            warnings.Add($"Budget level {normalized.BudgetLevel} was applied to experience selection.");
        }
        if (normalized.Start.Label == PendingFirstStopOrigin &&
            eligibleRecommendations.Count > 0)
        {
            normalized.Start = ToPoint(eligibleRecommendations[0]);
            normalized.Start.Label = "First itinerary stop fallback";
            warnings.Add("No explicit or region origin was available; the first itinerary stop is used as the route origin.");
        }
        var schedule = await BuildScheduleAsync(
            normalized,
            eligibleRecommendations,
            dayStart,
            dayEnd,
            dayBudget,
            cancellationToken);

        warnings.AddRange(schedule.Warnings);
        if (schedule.ExperienceStops.Count == 0)
        {
            warnings.Add("Recommended experiences were found, but none fit inside the requested time window.");
        }

        var stops = BuildStops(normalized, schedule, dayStart);
        var score = CalculateItineraryScore(schedule.ExperienceStops, schedule.Legs, recommendations.Items.Count);

        var estimatedCost = schedule.ExperienceStops
            .Where(stop => stop.Item.StartingPricePerPerson is not null)
            .Sum(stop => stop.Item.StartingPricePerPerson!.Value * (normalized.GuestsCount ?? 1));
        var unknownPrices = schedule.ExperienceStops.Count(stop => stop.Item.StartingPricePerPerson is null);
        if (dayBudget is not null && estimatedCost > dayBudget)
            warnings.Add("The selected stops exceed this day's share of the trip budget.");

        return new ItineraryDayResponse
        {
            Date = normalized.Date.Value,
            TotalCandidateExperiences = recommendations.TotalMatchingCandidates,
            SelectedStopsCount = schedule.ExperienceStops.Count,
            TotalDurationMinutes = Math.Max(0, (int)Math.Round((schedule.EndTimeLocal - dayStart).TotalMinutes)),
            TotalTravelMinutes = schedule.Legs.Sum(x => x.DurationMinutes),
            TotalDistanceKm = Round(schedule.Legs.Sum(x => x.DistanceKm)),
            EstimatedCost = estimatedCost,
            UnknownPriceStops = unknownPrices,
            Score = score,
            Warnings = warnings.Distinct().ToList(),
            Stops = stops,
            Legs = schedule.Legs,
            Explanation = BuildExplanation(normalized, schedule.ExperienceStops, score)
        };
    }

    public async Task<NaturalLanguageItineraryPlanResponse> PlanFromTextAsync(
        NaturalLanguageItineraryPlanRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Text))
        {
            throw new ArgumentException("Natural-language itinerary text is required.");
        }
        if (request.Text.Length > _options.NaturalLanguageMaxCharacters)
        {
            throw new ArgumentException(
                $"Natural-language itinerary text cannot exceed {_options.NaturalLanguageMaxCharacters} characters.");
        }

        var language = NormalizeLanguage(request.PreferredLanguage) ?? DetectLanguage(request.Text);
        var aiClassification = await _groqClient.ClassifyPlanAsync(request.Text, language, cancellationToken);
        var classified = aiClassification ?? ClassifyLocally(request.Text, language);

        if (request.Adm0Gid is null && request.Adm1Gid is null &&
            request.Adm2Gid is null && request.Adm3Gid is null &&
            !string.IsNullOrWhiteSpace(classified.RegionName))
        {
            var resolution = await _regionReadService.ResolveNameAsync(
                classified.RegionName,
                cancellationToken);
            if (!resolution.IsResolved)
            {
                throw new ArgumentException(resolution.IsAmbiguous
                    ? $"Region “{classified.RegionName}” is ambiguous; select a region to continue."
                    : $"Region “{classified.RegionName}” could not be resolved.");
            }

            classified.ResolvedRegionName = resolution.DisplayName;
            request.Adm0Gid = resolution.Adm0Gid;
            request.Adm1Gid = resolution.Adm1Gid;
            request.Adm2Gid = resolution.Adm2Gid;
            request.Adm3Gid = resolution.Adm3Gid;
            request.Destination ??= resolution.DisplayName;
        }

        var interpreted = BuildRequestFromClassification(request, classified, language);
        var itinerary = await PlanAsync(interpreted, cancellationToken);

        return new NaturalLanguageItineraryPlanResponse
        {
            InputText = request.Text,
            InterpretedRequest = interpreted,
            Classification = new ItineraryPlanClassificationResponse
            {
                IsAiGenerated = aiClassification is not null,
                Confidence = classified.ClassificationConfidence,
                Notes = classified.Notes
            },
            Itinerary = itinerary
        };
    }

    private async Task<ScheduleBuildResult> BuildScheduleAsync(
        ItineraryPlanRequest request,
        IReadOnlyList<ExperienceRecommendationItemResponse> recommendedItems,
        DateTime dayStart,
        DateTime dayEnd,
        decimal? dayBudget,
        CancellationToken cancellationToken)
    {
        var candidatePoolSize = Math.Min(
            recommendedItems.Count,
            Math.Max(8, (request.MaxStops ?? _options.DefaultMaxStops) * 3));

        var remaining = recommendedItems.Take(candidatePoolSize).ToList();
        var selected = new List<ScheduledExperience>();
        var legs = new List<ItineraryLegResponse>();
        var warnings = new List<string>();
        var currentPoint = request.Start;
        var currentTime = dayStart;
        var maxStops = request.MaxStops!.Value;
        decimal knownCost = 0;

        while (selected.Count < maxStops && remaining.Count > 0)
        {
            var best = await FindBestNextStopAsync(
                request,
                remaining.Take(Math.Max(6, maxStops * 2)).ToList(),
                currentPoint,
                currentTime,
                dayEnd,
                selected,
                dayBudget is null ? null : Math.Max(0, dayBudget.Value - knownCost),
                cancellationToken);

            if (best is null)
            {
                break;
            }

            var order = selected.Count + 1;
            legs.Add(new ItineraryLegResponse
            {
                FromOrder = order - 1,
                ToOrder = order,
                Mode = best.Leg.Mode,
                Provider = best.Leg.Provider,
                DistanceKm = best.Leg.DistanceKm,
                DurationMinutes = best.Leg.DurationMinutes,
                Geometry = best.Leg.Geometry,
                Steps = best.Leg.Steps,
                Warnings = best.Leg.Warnings
            });

            selected.Add(best);
            knownCost += best.Item.StartingPricePerPerson is null
                ? 0
                : best.Item.StartingPricePerPerson.Value * (request.GuestsCount ?? 1);
            remaining.RemoveAll(x => x.ExperienceId == best.Item.ExperienceId);
            warnings.AddRange(best.Leg.Warnings);
            currentPoint = ToPoint(best.Item);
            currentTime = best.DepartureLocal;
        }

        var endPoint = ResolveEndPoint(request);
        if (selected.Count > 0 || !SamePoint(request.Start, endPoint))
        {
            var endLeg = await _routePlanner.GetRouteAsync(new ItineraryRouteRequest
            {
                From = currentPoint,
                To = endPoint,
                Mode = request.TravelMode,
                FallbackMode = request.FallbackTravelMode,
                DepartureLocal = currentTime,
                AvoidLongWalking = request.AvoidLongWalking
            }, cancellationToken);

            var endArrival = currentTime.AddMinutes(endLeg.DurationMinutes);
            var endOrder = selected.Count + 1;
            legs.Add(new ItineraryLegResponse
            {
                FromOrder = endOrder - 1,
                ToOrder = endOrder,
                Mode = endLeg.Mode,
                Provider = endLeg.Provider,
                DistanceKm = endLeg.DistanceKm,
                DurationMinutes = endLeg.DurationMinutes,
                Geometry = endLeg.Geometry,
                Steps = endLeg.Steps,
                Warnings = endLeg.Warnings
            });

            warnings.AddRange(endLeg.Warnings);
            currentTime = endArrival;

            if (currentTime > dayEnd)
            {
                warnings.Add("Returning to the end point exceeds the requested day end time.");
            }
        }

        return new ScheduleBuildResult(selected, legs, warnings, currentTime);
    }

    private async Task<ScheduledExperience?> FindBestNextStopAsync(
        ItineraryPlanRequest request,
        IReadOnlyList<ExperienceRecommendationItemResponse> candidates,
        ItineraryPointRequest currentPoint,
        DateTime currentTime,
        DateTime dayEnd,
        IReadOnlyList<ScheduledExperience> selected,
        decimal? remainingBudget,
        CancellationToken cancellationToken)
    {
        ScheduledExperience? best = null;
        var usedCategories = selected
            .Select(x => x.Item.Category)
            .GroupBy(x => x)
            .ToDictionary(x => x.Key, x => x.Count());

        foreach (var candidate in candidates)
        {
            var candidateCost = candidate.StartingPricePerPerson is null
                ? (decimal?)null
                : candidate.StartingPricePerPerson.Value * (request.GuestsCount ?? 1);
            if (remainingBudget is not null &&
                candidateCost is not null &&
                candidateCost > remainingBudget)
            {
                continue;
            }

            var route = await _routePlanner.GetRouteAsync(new ItineraryRouteRequest
            {
                From = currentPoint,
                To = ToPoint(candidate),
                Mode = request.TravelMode,
                FallbackMode = request.FallbackTravelMode,
                DepartureLocal = currentTime,
                AvoidLongWalking = request.AvoidLongWalking
            }, cancellationToken);

            var arrival = currentTime.AddMinutes(route.DurationMinutes);
            var duration = AdjustDuration(candidate.EstimatedDurationMinutes, request.Pace);
            var departure = arrival.AddMinutes(duration);
            if (departure > dayEnd)
            {
                continue;
            }

            var categoryPenalty = usedCategories.TryGetValue(candidate.Category, out var count)
                ? count * 4.0
                : 0;
            var travelPenalty = route.DurationMinutes * (request.AvoidLongWalking && route.Mode == ItineraryTravelMode.Walking ? 0.55 : 0.35);
            var value = candidate.FinalScore - travelPenalty - categoryPenalty;

            var scheduled = new ScheduledExperience(candidate, route, arrival, departure, duration, value);
            if (best is null || scheduled.SelectionScore > best.SelectionScore)
            {
                best = scheduled;
            }
        }

        return best;
    }

    private List<ItineraryStopResponse> BuildStops(
        ItineraryPlanRequest request,
        ScheduleBuildResult schedule,
        DateTime dayStart)
    {
        var stops = new List<ItineraryStopResponse>
        {
            new()
            {
                Order = 0,
                Type = ItineraryStopType.Start,
                Name = request.Start.Label ?? "Start",
                Latitude = request.Start.Latitude,
                Longitude = request.Start.Longitude,
                ArrivalLocal = FormatLocalTime(dayStart),
                DepartureLocal = FormatLocalTime(dayStart),
                DurationMinutes = 0
            }
        };

        foreach (var scheduled in schedule.ExperienceStops)
        {
            var item = scheduled.Item;
            stops.Add(new ItineraryStopResponse
            {
                Order = stops.Count,
                Type = ItineraryStopType.Experience,
                ExperienceId = item.ExperienceId,
                Name = item.Name,
                Category = item.Category,
                RegionDisplayName = item.RegionDisplayName,
                Address = item.Address,
                Latitude = item.Latitude,
                Longitude = item.Longitude,
                ArrivalLocal = FormatLocalTime(scheduled.ArrivalLocal),
                DepartureLocal = FormatLocalTime(scheduled.DepartureLocal),
                DurationMinutes = scheduled.DurationMinutes,
                EstimatedCost = item.StartingPricePerPerson is null
                    ? null
                    : item.StartingPricePerPerson.Value * (request.GuestsCount ?? 1),
                Explanation = item.Explanation.ShortExplanation,
                RecommendationScore = item.FinalScore,
                PrimaryImage = item.PrimaryImage,
                Notes = BuildStopNotes(item)
            });
        }

        var endPoint = ResolveEndPoint(request);
        if (schedule.Legs.Count > 0)
        {
            stops.Add(new ItineraryStopResponse
            {
                Order = stops.Count,
                Type = ItineraryStopType.End,
                Name = endPoint.Label ?? "End",
                Latitude = endPoint.Latitude,
                Longitude = endPoint.Longitude,
                ArrivalLocal = FormatLocalTime(schedule.EndTimeLocal),
                DepartureLocal = FormatLocalTime(schedule.EndTimeLocal),
                DurationMinutes = 0
            });
        }

        return stops;
    }

    private ItineraryPlanRequest BuildRequestFromClassification(
        NaturalLanguageItineraryPlanRequest request,
        ItineraryPlanPreferences classified,
        string language)
    {
        var preferredLanguage = NormalizeLanguage(request.PreferredLanguage) ??
                                NormalizeLanguage(classified.PreferredLanguage) ??
                                language;

        return NormalizeRequest(new ItineraryPlanRequest
        {
            Start = request.Start,
            Origin = request.Origin,
            End = request.End,
            Date = request.Date,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Destination = request.Destination ?? classified.ResolvedRegionName,
            DayStartLocal = request.DayStartLocal ?? classified.DayStartLocal,
            DayEndLocal = request.DayEndLocal ?? classified.DayEndLocal,
            TravelMode = request.TravelMode ?? classified.TravelMode ?? ItineraryTravelMode.PublicTransit,
            FallbackTravelMode = request.FallbackTravelMode ?? classified.FallbackTravelMode ?? ItineraryTravelMode.Walking,
            Pace = request.Pace ?? classified.Pace ?? ItineraryPace.Balanced,
            Categories = classified.Categories,
            Adm0Gid = request.Adm0Gid,
            Adm1Gid = request.Adm1Gid,
            Adm2Gid = request.Adm2Gid,
            Adm3Gid = request.Adm3Gid,
            MaxStops = request.MaxStops ?? classified.MaxStops,
            CandidateLimit = request.CandidateLimit,
            GuestsCount = request.GuestsCount,
            CrowdPreference = classified.CrowdPreference,
            IncludeMealBreaks = classified.IncludeMealBreaks ?? false,
            ReturnToStart = classified.ReturnToStart ?? true,
            AvoidLongWalking = classified.AvoidLongWalking ?? false,
            PreferredLanguage = preferredLanguage,
            BudgetLevel = request.BudgetLevel,
            TotalBudget = request.TotalBudget,
            Currency = request.Currency
        });
    }

    private ExperienceRecommendationRequest BuildRecommendationRequest(
        ItineraryPlanRequest request,
        DateTime visitAtLocal)
    {
        var categories = request.Categories
            .Select(x => new ExperienceRecommendationCategoryPreference
            {
                Category = x.Category,
                Weight = x.Weight
            })
            .ToList();

        if (request.IncludeMealBreaks &&
            !categories.Any(x => x.Category.Equals(nameof(ExperienceCategory.Dining), StringComparison.OrdinalIgnoreCase)))
        {
            categories.Add(new ExperienceRecommendationCategoryPreference
            {
                Category = nameof(ExperienceCategory.Dining),
                Weight = 15
            });
        }

        return new ExperienceRecommendationRequest
        {
            Categories = categories,
            Latitude = request.Start.Label == PendingFirstStopOrigin ? null : request.Start.Latitude,
            Longitude = request.Start.Label == PendingFirstStopOrigin ? null : request.Start.Longitude,
            Adm0Gid = request.Adm0Gid,
            Adm1Gid = request.Adm1Gid,
            Adm2Gid = request.Adm2Gid,
            Adm3Gid = request.Adm3Gid,
            VisitAtLocal = visitAtLocal,
            GuestsCount = request.GuestsCount,
            CrowdPreference = request.CrowdPreference,
            BookableOnly = false,
            ForItinerary = true,
            Limit = request.CandidateLimit,
            PreferredLanguage = request.PreferredLanguage
        };
    }

    private ItineraryPlanRequest NormalizeRequest(ItineraryPlanRequest request)
    {
        ValidatePoint(request.Start, "Start");
        if (request.End is not null)
        {
            ValidatePoint(request.End, "End");
        }

        request.Date ??= DateOnly.FromDateTime(DateTime.Today);
        request.StartDate ??= request.Date;
        request.EndDate ??= request.StartDate;
        if (request.EndDate.Value < request.StartDate.Value)
            throw new ArgumentException("Trip end date must be on or after the start date.");
        if (request.TotalBudget is < 0)
            throw new ArgumentException("Total trip budget cannot be negative.");
        if (request.BudgetLevel is < 1 or > 5)
            throw new ArgumentException("Budget level must be between 1 and 5.");
        request.Currency = string.IsNullOrWhiteSpace(request.Currency)
            ? "USD"
            : request.Currency.Trim().ToUpperInvariant();
        if (request.Currency.Length != 3)
            throw new ArgumentException("Currency must use a three-letter code.");
        request.DayStartLocal ??= new TimeOnly(9, 0);
        request.DayEndLocal ??= new TimeOnly(21, 0);
        if (request.DayEndLocal <= request.DayStartLocal)
        {
            throw new ArgumentException("Day end time must be after day start time.");
        }

        if (request.TravelMode == ItineraryTravelMode.PublicTransit &&
            request.FallbackTravelMode == ItineraryTravelMode.PublicTransit)
        {
            request.FallbackTravelMode = ItineraryTravelMode.Walking;
        }

        var maxStops = Math.Max(1, _options.MaxStops);
        if (request.MaxStops is not null &&
            (request.MaxStops < 1 || request.MaxStops > maxStops))
        {
            throw new ArgumentException($"Maximum stops must be between 1 and {maxStops}.");
        }
        request.MaxStops ??= Math.Clamp(_options.DefaultMaxStops, 1, maxStops);

        var maxCandidateLimit = Math.Max(request.MaxStops.Value, _options.MaxCandidateLimit);
        if (request.CandidateLimit is not null &&
            (request.CandidateLimit < request.MaxStops || request.CandidateLimit > maxCandidateLimit))
        {
            throw new ArgumentException(
                $"Candidate limit must be between {request.MaxStops} and {maxCandidateLimit}.");
        }
        request.CandidateLimit ??= Math.Clamp(
            _options.DefaultCandidateLimit,
            request.MaxStops.Value,
            maxCandidateLimit);
        if (request.GuestsCount is < 1 || request.GuestsCount > _options.MaxTravelers)
        {
            throw new ArgumentException(
                $"Traveler count must be between 1 and {_options.MaxTravelers}.");
        }
        if (request.Categories.Count > _options.MaxSelectedCategories)
        {
            throw new ArgumentException(
                $"No more than {_options.MaxSelectedCategories} categories may be selected.");
        }
        request.PreferredLanguage = NormalizeLanguage(request.PreferredLanguage) ?? "en";
        request.Categories = NormalizeCategories(request.Categories);

        return request;
    }

    private async Task ResolveOriginAsync(
        ItineraryPlanRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Origin is not null)
        {
            request.Start = request.Origin;
            request.Start.Label ??= "Explicit start point";
            return;
        }

        if (request.Start.Latitude != 0 || request.Start.Longitude != 0)
        {
            request.Start.Label ??= "Selected start point";
            return;
        }

        var centroid = await _regionReadService.ResolveCentroidAsync(
            request.Adm0Gid,
            request.Adm1Gid,
            request.Adm2Gid,
            request.Adm3Gid,
            cancellationToken);
        if (centroid is null)
        {
            request.Start = new ItineraryPointRequest
            {
                Label = PendingFirstStopOrigin
            };
            return;
        }

        request.Start = new ItineraryPointRequest
        {
            Latitude = centroid.Latitude,
            Longitude = centroid.Longitude,
            Label = $"{centroid.Name ?? "Selected region"} centroid ({centroid.AdministrativeLevel})"
        };
    }

    private static ItineraryPlanRequest CloneForDate(
        ItineraryPlanRequest source,
        DateOnly date) =>
        new()
        {
            Start = source.Start,
            Origin = source.Origin,
            End = source.End,
            Date = date,
            StartDate = source.StartDate,
            EndDate = source.EndDate,
            Destination = source.Destination,
            DayStartLocal = source.DayStartLocal,
            DayEndLocal = source.DayEndLocal,
            TravelMode = source.TravelMode,
            FallbackTravelMode = source.FallbackTravelMode,
            Pace = source.Pace,
            Categories = source.Categories,
            Adm0Gid = source.Adm0Gid,
            Adm1Gid = source.Adm1Gid,
            Adm2Gid = source.Adm2Gid,
            Adm3Gid = source.Adm3Gid,
            MaxStops = source.MaxStops,
            CandidateLimit = source.CandidateLimit,
            GuestsCount = source.GuestsCount,
            CrowdPreference = source.CrowdPreference,
            IncludeMealBreaks = source.IncludeMealBreaks,
            ReturnToStart = source.ReturnToStart,
            AvoidLongWalking = source.AvoidLongWalking,
            PreferredLanguage = source.PreferredLanguage,
            BudgetLevel = source.BudgetLevel,
            TotalBudget = source.TotalBudget,
            Currency = source.Currency
        };

    private List<ItineraryCategoryPreference> NormalizeCategories(
        IReadOnlyList<ItineraryCategoryPreference> categories)
    {
        var grouped = categories
            .Where(x => !string.IsNullOrWhiteSpace(x.Category))
            .Select(x => new
            {
                Category = ParseCategory(x.Category),
                Weight = NormalizeWeight(x.Weight ?? 0)
            })
            .GroupBy(x => x.Category)
            .Select(x => new ItineraryCategoryPreference
            {
                Category = x.Key.ToString(),
                Weight = x.Sum(y => y.Weight)
            })
            .ToList();

        if (grouped.Count == 0)
        {
            return grouped;
        }

        var total = grouped.Sum(x => x.Weight ?? 0);
        if (total <= 0)
        {
            var equal = 1.0 / grouped.Count;
            foreach (var item in grouped)
            {
                item.Weight = equal;
            }
        }
        else
        {
            foreach (var item in grouped)
            {
                item.Weight = (item.Weight ?? 0) / total;
            }
        }

        return grouped
            .OrderByDescending(x => x.Weight)
            .ThenBy(x => x.Category)
            .ToList();
    }

    private ItineraryPlanPreferences ClassifyLocally(string text, string language)
    {
        var normalized = NormalizeForMatching(text);
        var preferences = new ItineraryPlanPreferences
        {
            PreferredLanguage = language,
            ClassificationConfidence = 0.35,
            Notes = LocalFallbackNotes,
            Pace = ItineraryPace.Balanced,
            TravelMode = ItineraryTravelMode.PublicTransit,
            FallbackTravelMode = ItineraryTravelMode.Walking,
            ReturnToStart = true
        };
        preferences.RegionName = FirstMentionedRegion(normalized);

        AddCategoryIfMentioned(preferences, normalized, ExperienceCategory.Historical, "historical", "history", "تاريخي", "اثري", "آثار", "اثار");
        AddCategoryIfMentioned(preferences, normalized, ExperienceCategory.Nature, "nature", "park", "beach", "طبيعة", "حديقة", "بحر");
        AddCategoryIfMentioned(preferences, normalized, ExperienceCategory.Shopping, "shopping", "mall", "market", "تسوق", "مول", "سوق");
        AddCategoryIfMentioned(preferences, normalized, ExperienceCategory.Nightlife, "nightlife", "night", "سهر", "ليل");
        AddCategoryIfMentioned(preferences, normalized, ExperienceCategory.Dining, "dining", "restaurant", "food", "مطاعم", "مطعم", "اكل");

        if (ContainsAny(normalized, "relaxed", "light", "خفيف", "راحة", "مش كتير"))
        {
            preferences.Pace = ItineraryPace.Relaxed;
            preferences.MaxStops = 4;
        }
        else if (ContainsAny(normalized, "packed", "full", "كتير", "مليان"))
        {
            preferences.Pace = ItineraryPace.Packed;
            preferences.MaxStops = 8;
        }

        if (ContainsAny(normalized, "walk", "walking", "مشي"))
        {
            preferences.TravelMode = ContainsAny(normalized, "مش عايز مشي", "بدون مشي")
                ? ItineraryTravelMode.PublicTransit
                : ItineraryTravelMode.Walking;
        }
        else if (ContainsAny(normalized, "car", "drive", "عربية", "سيارة"))
        {
            preferences.TravelMode = ItineraryTravelMode.Driving;
        }
        else if (ContainsAny(normalized, "bike", "cycling", "عجلة"))
        {
            preferences.TravelMode = ItineraryTravelMode.Cycling;
        }

        preferences.AvoidLongWalking = ContainsAny(normalized, "مش عايز مشي", "بدون مشي", "avoid walking");
        preferences.IncludeMealBreaks = ContainsAny(normalized, "food", "lunch", "dinner", "مطعم", "اكل", "غدا", "عشا");

        if (ContainsAny(normalized, "quiet", "هادي", "مش زحمة"))
        {
            preferences.CrowdPreference = ExperienceCrowdPreference.Quiet;
        }
        else if (ContainsAny(normalized, "lively", "busy", "زحمة", "حيوي"))
        {
            preferences.CrowdPreference = ExperienceCrowdPreference.Lively;
        }

        return preferences;
    }

    private ItineraryDayResponse BuildEmptyDayResponse(ItineraryPlanRequest request, List<string> warnings) =>
        new()
        {
            Date = request.Date!.Value,
            Warnings = warnings,
            Stops =
            [
                new ItineraryStopResponse
                {
                    Order = 0,
                    Type = ItineraryStopType.Start,
                    Name = request.Start.Label ?? "Start",
                    Latitude = request.Start.Latitude,
                    Longitude = request.Start.Longitude,
                    ArrivalLocal = FormatLocalTime(request.Date.Value.ToDateTime(request.DayStartLocal!.Value)),
                    DepartureLocal = FormatLocalTime(request.Date.Value.ToDateTime(request.DayStartLocal.Value))
                }
            ],
            Explanation = new ItineraryExplanationResponse
            {
                Summary = "No itinerary could be built from the available recommendation data.",
                Reasons = warnings,
                IsAiGenerated = false
            }
        };

    private ItineraryExplanationResponse BuildExplanation(
        ItineraryPlanRequest request,
        IReadOnlyList<ScheduledExperience> stops,
        double score)
    {
        var language = request.PreferredLanguage ?? "en";
        if (language.Equals("ar", StringComparison.OrdinalIgnoreCase))
        {
            return new ItineraryExplanationResponse
            {
                Summary = stops.Count == 0
                    ? "لم يتم العثور على محطات مناسبة داخل الوقت المطلوب."
                    : $"تم بناء خطة بدرجة {score.ToString("0.#", CultureInfo.InvariantCulture)} اعتمادا على ترشيحات الأماكن، وقت التنقل، ومدة الزيارة.",
                Reasons =
                [
                    "تم ترتيب المحطات بعد حساب التنقل بين كل نقطة والتالية.",
                    "تم استخدام ترشيحات التجارب كإشارة جودة أساسية.",
                    "لو فشل مزود الخرائط يتم استخدام تقدير آمن للمسافة والزمن."
                ],
                IsAiGenerated = false
            };
        }

        return new ItineraryExplanationResponse
        {
            Summary = stops.Count == 0
                ? "No suitable stops fit inside the requested time window."
                : $"Built a {score.ToString("0.#", CultureInfo.InvariantCulture)} score itinerary using recommendation quality, travel time, and visit duration.",
            Reasons =
            [
                "Stops are ordered after estimating travel between each point.",
                "Experience recommendations provide the main quality signal.",
                "A safe straight-line estimate is used when a routing provider is unavailable."
            ],
            IsAiGenerated = false
        };
    }

    private List<string> BuildStopNotes(ExperienceRecommendationItemResponse item)
    {
        var notes = new List<string>();
        if (!string.IsNullOrWhiteSpace(item.RoutingHints.TimeOfDayPreference))
        {
            notes.Add($"Preferred time: {item.RoutingHints.TimeOfDayPreference}");
        }

        if (!item.OpenHoursDataAvailable)
        {
            notes.Add("Opening-hours data is unavailable.");
        }

        if (item.FinalScore >= 80)
        {
            notes.Add("High recommendation score.");
        }

        return notes;
    }

    private static void ValidatePoint(ItineraryPointRequest point, string name)
    {
        if (point.Latitude is < -90 or > 90 || point.Longitude is < -180 or > 180)
        {
            throw new ArgumentException($"{name} latitude or longitude is outside its valid range.");
        }
    }

    private static ItineraryPointRequest ResolveEndPoint(ItineraryPlanRequest request) =>
        request.ReturnToStart ? request.Start : request.End ?? request.Start;

    private static ItineraryPointRequest ToPoint(ExperienceRecommendationItemResponse item) =>
        new()
        {
            Latitude = item.Latitude,
            Longitude = item.Longitude,
            Label = item.Name
        };

    private static bool SamePoint(ItineraryPointRequest first, ItineraryPointRequest second) =>
        Math.Abs(first.Latitude - second.Latitude) < 0.000001 &&
        Math.Abs(first.Longitude - second.Longitude) < 0.000001;

    private static int AdjustDuration(int estimatedDurationMinutes, ItineraryPace pace)
    {
        var multiplier = pace switch
        {
            ItineraryPace.Relaxed => 1.15,
            ItineraryPace.Packed => 0.85,
            _ => 1.0
        };

        return Math.Max(30, (int)Math.Round(estimatedDurationMinutes * multiplier));
    }

    private static double CalculateItineraryScore(
        IReadOnlyList<ScheduledExperience> stops,
        IReadOnlyList<ItineraryLegResponse> legs,
        int candidateCount)
    {
        if (stops.Count == 0)
        {
            return 0;
        }

        var recommendationScore = stops.Average(x => x.Item.FinalScore);
        var travelPenalty = Math.Min(25, legs.Sum(x => x.DurationMinutes) * 0.08);
        var selectionBonus = Math.Min(10, stops.Count / (double)Math.Max(1, candidateCount) * 30);
        return Round(Math.Clamp(recommendationScore - travelPenalty + selectionBonus, 0, 100));
    }

    private static ExperienceCategory ParseCategory(string value)
    {
        if (!Enum.TryParse<ExperienceCategory>(value, true, out var category) || !Enum.IsDefined(category))
        {
            throw new ArgumentException($"Unsupported experience category '{value}'.");
        }

        return category;
    }

    private static double NormalizeWeight(double weight)
    {
        if (weight <= 0)
        {
            return 0;
        }

        return weight > 1 ? weight / 100.0 : weight;
    }

    private static string? NormalizeLanguage(string? language)
    {
        if (string.IsNullOrWhiteSpace(language))
        {
            return null;
        }

        return language.Trim().StartsWith("ar", StringComparison.OrdinalIgnoreCase) ? "ar" : "en";
    }

    private static string DetectLanguage(string text) =>
        text.Any(c => c >= '\u0600' && c <= '\u06FF') ? "ar" : "en";

    private static void AddCategoryIfMentioned(
        ItineraryPlanPreferences preferences,
        string text,
        ExperienceCategory category,
        params string[] terms)
    {
        if (!ContainsAny(text, terms) ||
            preferences.Categories.Any(x => x.Category.Equals(category.ToString(), StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        preferences.Categories.Add(new ItineraryCategoryPreference
        {
            Category = category.ToString(),
            Weight = 1
        });
    }

    private static bool ContainsAny(string text, params string[] terms) =>
        terms.Any(term => text.Contains(NormalizeForMatching(term), StringComparison.OrdinalIgnoreCase));

    private static string? FirstMentionedRegion(string text)
    {
        var aliases = new (string Match, string Region)[]
        {
            ("downtown cairo", "Downtown Cairo"), ("وسط البلد", "Downtown Cairo"),
            ("zamalek", "Zamalek"), ("الزمالك", "Zamalek"),
            ("alexandria", "Alexandria"), ("الاسكندرية", "Alexandria"),
            ("giza", "Giza"), ("الجيزه", "Giza"),
            ("luxor", "Luxor"), ("الاقصر", "Luxor"),
            ("aswan", "Aswan"), ("اسوان", "Aswan"),
            ("cairo", "Cairo"), ("القاهره", "Cairo")
        };
        return aliases.FirstOrDefault(x =>
            text.Contains(NormalizeForMatching(x.Match), StringComparison.OrdinalIgnoreCase)).Region;
    }

    private static List<ExperienceRecommendationItemResponse> ApplyBudgetLevel(
        IReadOnlyList<ExperienceRecommendationItemResponse> items,
        int? requestedLevel)
    {
        if (requestedLevel is null)
            return items.ToList();
        var priced = items
            .Where(item => item.StartingPricePerPerson is > 0)
            .OrderBy(item => item.StartingPricePerPerson)
            .Select((item, index) => new
            {
                item.ExperienceId,
                Level = Math.Clamp(
                    (int)Math.Ceiling((index + 1) * 5.0 / Math.Max(1, items.Count(value => value.StartingPricePerPerson is > 0))),
                    1,
                    5)
            })
            .ToDictionary(value => value.ExperienceId, value => value.Level);
        var filtered = items
            .Where(item => !priced.TryGetValue(item.ExperienceId, out var level) ||
                           Math.Abs(level - requestedLevel.Value) <= 1)
            .ToList();
        return filtered.Count > 0 ? filtered : items.ToList();
    }

    private static string NormalizeForMatching(string value) =>
        value.Trim()
            .ToLowerInvariant()
            .Replace('أ', 'ا')
            .Replace('إ', 'ا')
            .Replace('آ', 'ا')
            .Replace('ى', 'ي')
            .Replace('ة', 'ه');

    private static string FormatLocalTime(DateTime value) => value.ToString("HH:mm", CultureInfo.InvariantCulture);

    private static double Round(double value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);

    private const string LocalFallbackNotes =
        "Local fallback classification was used because Groq was unavailable or returned invalid JSON.";
    private const string PendingFirstStopOrigin = "Pending first itinerary stop fallback";

    private sealed record ScheduledExperience(
        ExperienceRecommendationItemResponse Item,
        ItineraryRouteResult Leg,
        DateTime ArrivalLocal,
        DateTime DepartureLocal,
        int DurationMinutes,
        double SelectionScore);

    private sealed record ScheduleBuildResult(
        List<ScheduledExperience> ExperienceStops,
        List<ItineraryLegResponse> Legs,
        List<string> Warnings,
        DateTime EndTimeLocal);
}
