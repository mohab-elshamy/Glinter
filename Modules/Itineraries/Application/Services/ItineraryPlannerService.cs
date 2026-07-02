using System.Globalization;
using Glinter.Modules.Experiences.Application.Abstractions;
using Glinter.Modules.Experiences.Application.Dtos;
using Glinter.Modules.Experiences.Domain.Enums;
using Glinter.Modules.Itineraries.Application.Abstractions;
using Glinter.Modules.Itineraries.Application.Dtos;
using Glinter.Modules.Itineraries.Application.Options;
using Glinter.Modules.Itineraries.Domain.Enums;
using Microsoft.Extensions.Options;

namespace Glinter.Modules.Itineraries.Application.Services;

public class ItineraryPlannerService : IItineraryPlannerService
{
    private readonly IExperienceRecommendationService _experienceRecommendationService;
    private readonly IItineraryRoutePlanner _routePlanner;
    private readonly IItineraryGroqClient _groqClient;
    private readonly ItineraryPlanningOptions _options;
    private readonly ILogger<ItineraryPlannerService> _logger;

    public ItineraryPlannerService(
        IExperienceRecommendationService experienceRecommendationService,
        IItineraryRoutePlanner routePlanner,
        IItineraryGroqClient groqClient,
        IOptions<ItineraryPlanningOptions> options,
        ILogger<ItineraryPlannerService> logger)
    {
        _experienceRecommendationService = experienceRecommendationService;
        _routePlanner = routePlanner;
        _groqClient = groqClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<ItineraryPlanResponse> PlanAsync(
        ItineraryPlanRequest request,
        CancellationToken cancellationToken)
    {
        var normalized = NormalizeRequest(request);
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
            return BuildEmptyResponse(normalized, warnings);
        }

        var schedule = await BuildScheduleAsync(
            normalized,
            recommendations.Items,
            dayStart,
            dayEnd,
            cancellationToken);

        warnings.AddRange(schedule.Warnings);
        if (schedule.ExperienceStops.Count == 0)
        {
            warnings.Add("Recommended experiences were found, but none fit inside the requested time window.");
        }

        var stops = BuildStops(normalized, schedule, dayStart);
        var score = CalculateItineraryScore(schedule.ExperienceStops, schedule.Legs, recommendations.Items.Count);

        return new ItineraryPlanResponse
        {
            Date = normalized.Date.Value,
            TravelMode = normalized.TravelMode,
            FallbackTravelMode = normalized.FallbackTravelMode,
            Pace = normalized.Pace,
            TotalCandidateExperiences = recommendations.TotalCandidates,
            SelectedStopsCount = schedule.ExperienceStops.Count,
            TotalDurationMinutes = Math.Max(0, (int)Math.Round((schedule.EndTimeLocal - dayStart).TotalMinutes)),
            TotalTravelMinutes = schedule.Legs.Sum(x => x.DurationMinutes),
            TotalDistanceKm = Round(schedule.Legs.Sum(x => x.DistanceKm)),
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

        while (selected.Count < maxStops && remaining.Count > 0)
        {
            var best = await FindBestNextStopAsync(
                request,
                remaining.Take(Math.Max(6, maxStops * 2)).ToList(),
                currentPoint,
                currentTime,
                dayEnd,
                selected,
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
        CancellationToken cancellationToken)
    {
        ScheduledExperience? best = null;
        var usedCategories = selected
            .Select(x => x.Item.Category)
            .GroupBy(x => x)
            .ToDictionary(x => x.Key, x => x.Count());

        foreach (var candidate in candidates)
        {
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
            End = request.End,
            Date = request.Date,
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
            PreferredLanguage = preferredLanguage
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
            Latitude = request.Start.Latitude,
            Longitude = request.Start.Longitude,
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

    private ItineraryPlanResponse BuildEmptyResponse(ItineraryPlanRequest request, List<string> warnings) =>
        new()
        {
            Date = request.Date!.Value,
            TravelMode = request.TravelMode,
            FallbackTravelMode = request.FallbackTravelMode,
            Pace = request.Pace,
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
