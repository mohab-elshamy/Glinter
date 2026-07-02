using System.Globalization;
using System.Text;
using Glinter.Modules.Experiences.Application.Abstractions;
using Glinter.Modules.Experiences.Application.Dtos;
using Glinter.Modules.Experiences.Application.Options;
using Glinter.Modules.Experiences.Domain.Entities;
using Glinter.Modules.Experiences.Domain.Enums;
using Glinter.Modules.Experiences.Infrastructure.Persistence;
using Glinter.Modules.Regions.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Glinter.Modules.Experiences.Application.Services;

public class ExperienceRecommendationService
{
    private readonly ExperiencesDbContext _experiencesDbContext;
    private readonly RegionsDbContext _regionsDbContext;
    private readonly IExperienceRecommendationGroqClient _groqClient;
    private readonly ExperienceRecommendationOptions _options;
    private readonly ILogger<ExperienceRecommendationService> _logger;

    public ExperienceRecommendationService(
        ExperiencesDbContext experiencesDbContext,
        RegionsDbContext regionsDbContext,
        IExperienceRecommendationGroqClient groqClient,
        IOptions<ExperienceRecommendationOptions> options,
        ILogger<ExperienceRecommendationService> logger)
    {
        _experiencesDbContext = experiencesDbContext;
        _regionsDbContext = regionsDbContext;
        _groqClient = groqClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<ExperienceRecommendationResponse> RecommendAsync(
        ExperienceRecommendationRequest request,
        CancellationToken cancellationToken)
    {
        var preferences = NormalizeStructuredRequest(request);
        var result = await RecommendCoreAsync(preferences, cancellationToken);

        return new ExperienceRecommendationResponse
        {
            Preferences = preferences,
            TotalCandidates = result.TotalCandidates,
            ReturnedCount = result.Items.Count,
            Items = result.Items
        };
    }

    public async Task<NaturalLanguageExperienceRecommendationResponse> RecommendFromTextAsync(
        NaturalLanguageExperienceRecommendationRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Text))
        {
            throw new ArgumentException("Natural-language recommendation text is required.");
        }

        var language = NormalizeLanguage(request.PreferredLanguage) ?? DetectLanguage(request.Text);
        var classified = await _groqClient.ClassifyAsync(request.Text, language, cancellationToken)
                         ?? ClassifyLocally(request.Text, language);

        classified.Latitude ??= request.Latitude;
        classified.Longitude ??= request.Longitude;
        classified.Adm0Gid ??= request.Adm0Gid;
        classified.Adm1Gid ??= request.Adm1Gid;
        classified.Adm2Gid ??= request.Adm2Gid;
        classified.Adm3Gid ??= request.Adm3Gid;
        classified.VisitAtLocal ??= request.VisitAtLocal;
        classified.GuestsCount ??= request.GuestsCount;
        classified.ForItinerary = request.ForItinerary ?? classified.ForItinerary;
        classified.Limit = NormalizeLimit(request.Limit ?? classified.Limit, classified.ForItinerary);
        classified.PreferredLanguage = language;

        var preferences = NormalizePreferences(RepairClassifiedPreferences(classified));
        var result = await RecommendCoreAsync(preferences, cancellationToken);

        return new NaturalLanguageExperienceRecommendationResponse
        {
            InputText = request.Text,
            ClassificationNotes = classified.Notes,
            Preferences = preferences,
            TotalCandidates = result.TotalCandidates,
            ReturnedCount = result.Items.Count,
            Items = result.Items
        };
    }

    private async Task<RecommendationComputation> RecommendCoreAsync(
        ExperienceRecommendationPreferences preferences,
        CancellationToken cancellationToken)
    {
        if (preferences.BookableOnly)
        {
            return new RecommendationComputation(0, []);
        }

        var candidates = await GetCandidatesAsync(preferences, cancellationToken);
        if (candidates.Count == 0)
        {
            return new RecommendationComputation(0, []);
        }

        var qualityRange = CalculateQualityRange(candidates);
        var regions = await GetRegionContextsAsync(candidates, preferences.PreferredLanguage, cancellationToken);
        var scored = candidates
            .Select(candidate => ScoreCandidate(candidate, preferences, qualityRange, regions))
            .OrderByDescending(x => x.FinalScore)
            .ThenByDescending(x => x.Rating ?? 0)
            .ThenByDescending(x => x.Reviews ?? 0)
            .ThenBy(x => x.DistanceKm ?? double.MaxValue)
            .ThenBy(x => x.Name)
            .ToList();

        if (preferences.ForItinerary)
        {
            scored = ApplyDiversityReranking(scored);
        }

        var limited = scored.Take(preferences.Limit).ToList();
        var explanationMap = await TryGenerateAiExplanationsAsync(preferences, limited, cancellationToken);

        for (var index = 0; index < limited.Count; index++)
        {
            var item = limited[index];
            item.Ranking = index + 1;

            if (explanationMap?.TryGetValue(item.ExperienceId, out var explanation) == true)
            {
                explanation.IsAiGenerated = true;
                item.Explanation = explanation;
            }
            else
            {
                item.Explanation = BuildFallbackExplanation(preferences, item);
            }
        }

        return new RecommendationComputation(candidates.Count, limited);
    }

    private async Task<List<ExperienceCandidate>> GetCandidatesAsync(
        ExperienceRecommendationPreferences preferences,
        CancellationToken cancellationToken)
    {
        var query = _experiencesDbContext.Experiences
            .AsNoTracking()
            .Where(x => x.IsActive && x.ModerationStatus == ExperienceModerationStatus.Approved)
            .Where(x => x.Latitude != null && x.Longitude != null);

        if (preferences.Categories.Count > 0)
        {
            var categories = preferences.Categories.Select(x => x.Category).ToArray();
            query = query.Where(x => categories.Contains(x.Category));
        }

        if (preferences.Adm0Gid is not null)
        {
            query = query.Where(x => x.Adm0Gid == preferences.Adm0Gid);
        }

        if (preferences.Adm1Gid is not null)
        {
            query = query.Where(x => x.Adm1Gid == preferences.Adm1Gid);
        }

        if (preferences.Adm2Gid is not null)
        {
            query = query.Where(x => x.Adm2Gid == preferences.Adm2Gid);
        }

        if (preferences.Adm3Gid is not null)
        {
            query = query.Where(x => x.Adm3Gid == preferences.Adm3Gid);
        }

        return await query
            .Include(x => x.FeaturedImages)
            .Include(x => x.Hours)
            .Include(x => x.PopularTimes)
            .Include(x => x.Amenities)
            .OrderByDescending(x => x.Rating)
            .ThenByDescending(x => x.Reviews)
            .ThenBy(x => x.Name)
            .Take(Math.Clamp(_options.MaxCandidateExperiences, 1, 5000))
            .AsSplitQuery()
            .Select(x => new ExperienceCandidate
            {
                Id = x.Id,
                Category = x.Category,
                SourceType = x.SourceType,
                Name = x.Name,
                Description = x.Description,
                Address = x.Address,
                Adm0Gid = x.Adm0Gid,
                Adm1Gid = x.Adm1Gid,
                Adm2Gid = x.Adm2Gid,
                Adm3Gid = x.Adm3Gid,
                Latitude = x.Latitude!.Value,
                Longitude = x.Longitude!.Value,
                GoogleMapsLink = x.GoogleMapsLink,
                PriceRange = x.PriceRange,
                Reviews = x.Reviews,
                Rating = x.Rating,
                Website = x.Website,
                PrimaryImage = x.FeaturedImages.OrderBy(i => i.Id).Select(i => i.Link).FirstOrDefault(),
                Hours = x.Hours
                    .Select(h => new HourCandidate
                    {
                        DayOfWeek = h.DayOfWeek,
                        OpensAt = h.OpensAt,
                        ClosesAt = h.ClosesAt
                    })
                    .ToList(),
                PopularTimes = x.PopularTimes
                    .Select(p => new PopularTimeCandidate
                    {
                        DayOfWeek = p.DayOfWeek,
                        HourOfDay = p.HourOfDay,
                        PopularityPercentage = p.PopularityPercentage
                    })
                    .ToList(),
                Amenities = x.Amenities
                    .Select(a => a.NameEn ?? a.NameAr)
                    .Where(a => a != null)
                    .Select(a => a!)
                    .ToList(),
                HasReviews = x.ExperienceReviews.Any()
            })
            .ToListAsync(cancellationToken);
    }

    private ExperienceRecommendationItemResponse ScoreCandidate(
        ExperienceCandidate candidate,
        ExperienceRecommendationPreferences preferences,
        QualityRange qualityRange,
        IReadOnlyDictionary<int, string?> regions)
    {
        var categoryScore = CalculateCategoryScore(candidate, preferences);
        var distanceScore = CalculateDistanceScore(candidate, preferences, out var distanceKm);
        var qualityScore = CalculateNormalizedQualityScore(candidate, qualityRange);
        var timing = CalculateTimingScore(candidate, preferences);
        var crowdScore = CalculateCrowdScore(candidate, preferences);
        var contentScore = CalculateContentCompletenessScore(candidate);

        var weightedScores = new List<(double Weight, double Score)>();
        AddScore(weightedScores, 0.20, categoryScore);
        AddScore(weightedScores, 0.20, distanceScore);
        AddScore(weightedScores, 0.20, qualityScore);
        AddScore(weightedScores, 0.15, timing.Score);
        AddScore(weightedScores, 0.10, crowdScore);
        AddScore(weightedScores, 0.05, contentScore);

        var totalWeight = weightedScores.Sum(x => x.Weight);
        var finalScore = totalWeight <= 0
            ? 0
            : weightedScores.Sum(x => x.Weight * x.Score) / totalWeight * 100;

        var estimatedDuration = GetDefaultDurationMinutes(candidate.Category);
        var clusterKey = candidate.Adm3Gid is not null
            ? $"adm3:{candidate.Adm3Gid}"
            : candidate.Adm2Gid is not null
                ? $"adm2:{candidate.Adm2Gid}"
                : candidate.Adm1Gid is not null
                    ? $"adm1:{candidate.Adm1Gid}"
                    : null;

        return new ExperienceRecommendationItemResponse
        {
            ExperienceId = candidate.Id,
            Name = candidate.Name,
            Category = candidate.Category,
            Address = candidate.Address,
            Description = Truncate(candidate.Description, 500),
            Adm0Gid = candidate.Adm0Gid,
            Adm1Gid = candidate.Adm1Gid,
            Adm2Gid = candidate.Adm2Gid,
            Adm3Gid = candidate.Adm3Gid,
            RegionDisplayName = regions.TryGetValue(candidate.Id, out var region) ? region : null,
            Latitude = candidate.Latitude,
            Longitude = candidate.Longitude,
            DistanceKm = distanceKm is null ? null : Round(distanceKm.Value),
            Rating = candidate.Rating,
            Reviews = candidate.Reviews,
            PriceRange = candidate.PriceRange,
            StartingPricePerPerson = null,
            PrimaryImage = candidate.PrimaryImage,
            EstimatedDurationMinutes = estimatedDuration,
            DurationSource = "DefaultEstimate",
            RecommendedVisitWindow = BuildRecommendedVisitWindow(preferences, candidate.Category, estimatedDuration),
            OpeningWindow = timing.OpeningWindow,
            OpenHoursDataAvailable = candidate.Hours.Count > 0,
            PopularTimesDataAvailable = preferences.VisitAtLocal is not null &&
                                        FindPopularTime(candidate, preferences.VisitAtLocal.Value) is not null,
            AvailabilityDataAvailable = false,
            NextAvailableSlot = null,
            RoutingHints = new ExperienceRoutingHintsResponse
            {
                MustVisitAtFixedTime = false,
                RequiresBooking = false,
                ClusterKey = clusterKey,
                TimeOfDayPreference = GetTimeOfDayPreference(candidate.Category)
            },
            FinalScore = Round(finalScore),
            Scores = new ExperienceRecommendationScoreBreakdown
            {
                CategoryMatchScore = categoryScore is null ? null : Round(categoryScore.Value),
                DistanceScore = distanceScore is null ? null : Round(distanceScore.Value),
                QualityScore = Round(qualityScore),
                TimingOpenScore = timing.Score is null ? null : Round(timing.Score.Value),
                CrowdPreferenceScore = crowdScore is null ? null : Round(crowdScore.Value),
                AvailabilityScore = null,
                ContentCompletenessScore = Round(contentScore)
            },
            Amenities = candidate.Amenities
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x)
                .ToList()
        };
    }

    private async Task<Dictionary<int, string?>> GetRegionContextsAsync(
        IReadOnlyList<ExperienceCandidate> candidates,
        string preferredLanguage,
        CancellationToken cancellationToken)
    {
        var adm1Ids = candidates.Select(x => x.Adm1Gid).Where(x => x is not null).Select(x => x!.Value).Distinct().ToArray();
        var adm2Ids = candidates.Select(x => x.Adm2Gid).Where(x => x is not null).Select(x => x!.Value).Distinct().ToArray();
        var adm3Ids = candidates.Select(x => x.Adm3Gid).Where(x => x is not null).Select(x => x!.Value).Distinct().ToArray();
        var useArabic = preferredLanguage.Equals("ar", StringComparison.OrdinalIgnoreCase);

        var adm1 = await _regionsDbContext.Adm1
            .AsNoTracking()
            .Where(x => adm1Ids.Contains(x.Gid))
            .Select(x => new RegionName(x.Gid, x.NameEn, x.NameAr))
            .ToDictionaryAsync(x => x.Gid, cancellationToken);

        var adm2 = await _regionsDbContext.Adm2
            .AsNoTracking()
            .Where(x => adm2Ids.Contains(x.Gid))
            .Select(x => new RegionName(x.Gid, x.NameEn, x.NameAr))
            .ToDictionaryAsync(x => x.Gid, cancellationToken);

        var adm3 = await _regionsDbContext.Adm3
            .AsNoTracking()
            .Where(x => adm3Ids.Contains(x.Gid))
            .Select(x => new RegionName(x.Gid, x.NameEn, x.NameAr))
            .ToDictionaryAsync(x => x.Gid, cancellationToken);

        return candidates.ToDictionary(
            x => x.Id,
            x =>
            {
                var neighbourhood = x.Adm3Gid is not null && adm3.TryGetValue(x.Adm3Gid.Value, out var n)
                    ? PickLocalized(n, useArabic)
                    : null;
                var district = x.Adm2Gid is not null && adm2.TryGetValue(x.Adm2Gid.Value, out var d)
                    ? PickLocalized(d, useArabic)
                    : null;
                var governorate = x.Adm1Gid is not null && adm1.TryGetValue(x.Adm1Gid.Value, out var g)
                    ? PickLocalized(g, useArabic)
                    : null;

                var names = new[] { neighbourhood, district, governorate }
                    .Where(value => !string.IsNullOrWhiteSpace(value))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                return names.Count == 0 ? null : string.Join(", ", names);
            });
    }

    private ExperienceRecommendationPreferences NormalizeStructuredRequest(ExperienceRecommendationRequest request)
    {
        return NormalizePreferences(new ExperienceRecommendationPreferences
        {
            Categories = request.Categories
                .Select(x => new WeightedExperienceRecommendationCategory
                {
                    Category = ParseCategory(x.Category),
                    Weight = x.Weight ?? 0
                })
                .ToList(),
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            Adm0Gid = request.Adm0Gid,
            Adm1Gid = request.Adm1Gid,
            Adm2Gid = request.Adm2Gid,
            Adm3Gid = request.Adm3Gid,
            VisitAtLocal = request.VisitAtLocal,
            GuestsCount = request.GuestsCount,
            CrowdPreference = request.CrowdPreference,
            BookableOnly = request.BookableOnly,
            ForItinerary = request.ForItinerary,
            Limit = NormalizeLimit(request.Limit, request.ForItinerary),
            PreferredLanguage = NormalizeLanguage(request.PreferredLanguage) ?? "en"
        });
    }

    private ExperienceRecommendationPreferences NormalizePreferences(ExperienceRecommendationPreferences preferences)
    {
        if (preferences.Latitude is < -90 or > 90 || preferences.Longitude is < -180 or > 180)
        {
            throw new ArgumentException("Current latitude or longitude is outside its valid range.");
        }

        if ((preferences.Latitude is null) != (preferences.Longitude is null))
        {
            throw new ArgumentException("Latitude and longitude must be provided together.");
        }

        if (preferences.GuestsCount is < 1 or > 100)
        {
            throw new ArgumentException("Guest count must be between 1 and 100.");
        }

        preferences.PreferredLanguage = NormalizeLanguage(preferences.PreferredLanguage) ?? "en";
        preferences.Limit = NormalizeLimit(preferences.Limit, preferences.ForItinerary);

        var grouped = preferences.Categories
            .Where(x => Enum.IsDefined(x.Category))
            .GroupBy(x => x.Category)
            .Select(x => new WeightedExperienceRecommendationCategory
            {
                Category = x.Key,
                Weight = x.Sum(y => NormalizeWeight(y.Weight))
            })
            .ToList();

        if (grouped.Count > 0)
        {
            var total = grouped.Sum(x => x.Weight);
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
                    item.Weight /= total;
                }
            }
        }

        preferences.Categories = grouped
            .OrderByDescending(x => x.Weight)
            .ThenBy(x => x.Category)
            .ToList();

        return preferences;
    }

    private static ExperienceRecommendationPreferences RepairClassifiedPreferences(
        ExperienceRecommendationPreferences preferences)
    {
        preferences.Categories = preferences.Categories
            .Where(x => Enum.IsDefined(x.Category))
            .ToList();

        return preferences;
    }

    private ExperienceRecommendationPreferences ClassifyLocally(string text, string language)
    {
        var normalized = NormalizeForMatching(text);
        var preferences = new ExperienceRecommendationPreferences
        {
            PreferredLanguage = language,
            ClassificationConfidence = 0.4,
            Notes = "Local fallback classification was used because Groq was unavailable or returned invalid JSON."
        };

        AddCategoryIfMentioned(preferences, normalized, ExperienceCategory.Historical, "historical", "history", "تاريخي", "اثري", "آثار", "اثار");
        AddCategoryIfMentioned(preferences, normalized, ExperienceCategory.Nature, "nature", "park", "beach", "طبيعة", "حديقة", "بحر");
        AddCategoryIfMentioned(preferences, normalized, ExperienceCategory.Shopping, "shopping", "mall", "market", "تسوق", "مول", "سوق");
        AddCategoryIfMentioned(preferences, normalized, ExperienceCategory.Nightlife, "nightlife", "night", "club", "سهر", "ليل", "نايت");
        AddCategoryIfMentioned(preferences, normalized, ExperienceCategory.Dining, "dining", "restaurant", "food", "مطاعم", "مطعم", "اكل");

        if (ContainsAny(normalized, "quiet", "calm", "هادي", "هادئ", "مش زحمة"))
        {
            preferences.CrowdPreference = ExperienceCrowdPreference.Quiet;
        }
        else if (ContainsAny(normalized, "lively", "busy", "زحمة", "حيوي"))
        {
            preferences.CrowdPreference = ExperienceCrowdPreference.Lively;
        }

        return preferences;
    }

    private async Task<IReadOnlyDictionary<int, ExperienceRecommendationExplanationResponse>?> TryGenerateAiExplanationsAsync(
        ExperienceRecommendationPreferences preferences,
        IReadOnlyList<ExperienceRecommendationItemResponse> rankedItems,
        CancellationToken cancellationToken)
    {
        if (rankedItems.Count == 0)
        {
            return null;
        }

        try
        {
            return await _groqClient.GenerateExplanationsAsync(preferences, rankedItems, cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or OperationCanceledException or System.Text.Json.JsonException)
        {
            if (ex is OperationCanceledException && cancellationToken.IsCancellationRequested)
            {
                throw;
            }

            _logger.LogWarning(ex, "Groq experience recommendation explanations failed; using local fallback explanations.");
            return null;
        }
    }

    private static ExperienceRecommendationExplanationResponse BuildFallbackExplanation(
        ExperienceRecommendationPreferences preferences,
        ExperienceRecommendationItemResponse item)
    {
        var isArabic = preferences.PreferredLanguage.Equals("ar", StringComparison.OrdinalIgnoreCase);
        var reasons = new List<string>();
        var bestFor = new List<string> { item.Category.ToString() };

        if (item.DistanceKm is not null)
        {
            reasons.Add(isArabic
                ? $"قريب نسبيًا من نقطة البداية بمسافة {item.DistanceKm:0.#} كم."
                : $"It is relatively close to the start point at {item.DistanceKm:0.#} km.");
        }

        if (item.Rating is not null || item.Reviews is not null)
        {
            reasons.Add(isArabic
                ? $"إشارة الجودة مبنية على تقييم {item.Rating?.ToString(CultureInfo.InvariantCulture) ?? "غير متاح"} وعدد مراجعات {item.Reviews ?? 0}."
                : $"Quality uses rating {item.Rating?.ToString(CultureInfo.InvariantCulture) ?? "unavailable"} and {item.Reviews ?? 0} reviews.");
        }

        if (item.OpenHoursDataAvailable)
        {
            reasons.Add(isArabic
                ? "توجد بيانات مواعيد يمكن للرحلة استخدامها عند بناء الجدول."
                : "Opening-hours data is available for itinerary planning.");
        }

        return new ExperienceRecommendationExplanationResponse
        {
            ShortExplanation = isArabic
                ? $"{item.Name} اختيار مناسب بدرجة {item.FinalScore:0.#} من 100."
                : $"{item.Name} is a suitable route-ready option with a {item.FinalScore:0.#}/100 score.",
            Reasons = reasons.Take(3).ToList(),
            BestFor = bestFor.Distinct(StringComparer.OrdinalIgnoreCase).Take(4).ToList(),
            IsAiGenerated = false
        };
    }

    private static void AddScore(List<(double Weight, double Score)> scores, double weight, double? score)
    {
        if (score is not null)
        {
            scores.Add((weight, score.Value));
        }
    }

    private static double? CalculateCategoryScore(
        ExperienceCandidate candidate,
        ExperienceRecommendationPreferences preferences)
    {
        if (preferences.Categories.Count == 0)
        {
            return null;
        }

        return preferences.Categories.FirstOrDefault(x => x.Category == candidate.Category)?.Weight ?? 0;
    }

    private double? CalculateDistanceScore(
        ExperienceCandidate candidate,
        ExperienceRecommendationPreferences preferences,
        out double? distanceKm)
    {
        distanceKm = null;
        if (preferences.Latitude is null || preferences.Longitude is null)
        {
            return null;
        }

        distanceKm = CalculateDistanceKm(
            preferences.Latitude.Value,
            preferences.Longitude.Value,
            candidate.Latitude,
            candidate.Longitude);

        var maxDistance = Math.Max(1, _options.MaxUsefulDistanceKm);
        return Math.Clamp(1 - distanceKm.Value / maxDistance, 0, 1);
    }

    private static QualityRange CalculateQualityRange(IReadOnlyList<ExperienceCandidate> candidates)
    {
        var values = candidates.Select(CalculateRawQualityScore).ToList();
        return new QualityRange(values.Min(), values.Max());
    }

    private static double CalculateNormalizedQualityScore(ExperienceCandidate candidate, QualityRange range)
    {
        var raw = CalculateRawQualityScore(candidate);
        if (Math.Abs(range.Max - range.Min) < 0.000001)
        {
            return 0.5;
        }

        return Math.Clamp((raw - range.Min) / (range.Max - range.Min), 0, 1);
    }

    private static double CalculateRawQualityScore(ExperienceCandidate candidate)
    {
        var rating = Math.Max(0, (double)(candidate.Rating ?? 0));
        var reviews = Math.Max(0, candidate.Reviews ?? 0);
        return rating * Math.Log(reviews + 1);
    }

    private static TimingScoreResult CalculateTimingScore(
        ExperienceCandidate candidate,
        ExperienceRecommendationPreferences preferences)
    {
        if (preferences.VisitAtLocal is null || candidate.Hours.Count == 0)
        {
            return new TimingScoreResult(null, null);
        }

        var visitAt = preferences.VisitAtLocal.Value;
        var time = TimeOnly.FromDateTime(visitAt);
        var day = visitAt.DayOfWeek;
        var hours = candidate.Hours
            .Where(x => x.DayOfWeek == day)
            .OrderBy(x => x.OpensAt)
            .ToList();

        var matching = hours.FirstOrDefault(x => x.OpensAt <= time && x.ClosesAt > time);
        var nearest = matching ?? hours.FirstOrDefault();

        return new TimingScoreResult(
            matching is null ? 0 : 1,
            nearest is null
                ? null
                : new ExperienceOpeningWindowResponse
                {
                    OpensAt = nearest.OpensAt.ToString("HH:mm", CultureInfo.InvariantCulture),
                    ClosesAt = nearest.ClosesAt.ToString("HH:mm", CultureInfo.InvariantCulture)
                });
    }

    private static double? CalculateCrowdScore(
        ExperienceCandidate candidate,
        ExperienceRecommendationPreferences preferences)
    {
        if (preferences.VisitAtLocal is null || preferences.CrowdPreference is null)
        {
            return null;
        }

        var popularTime = FindPopularTime(candidate, preferences.VisitAtLocal.Value);
        if (popularTime is null)
        {
            return null;
        }

        var popularity = Math.Clamp(popularTime.PopularityPercentage, 0, 100) / 100.0;
        return preferences.CrowdPreference switch
        {
            ExperienceCrowdPreference.Quiet => 1 - popularity,
            ExperienceCrowdPreference.Lively => popularity,
            _ => Math.Clamp(1 - Math.Abs(popularity - 0.5) / 0.5, 0, 1)
        };
    }

    private static PopularTimeCandidate? FindPopularTime(
        ExperienceCandidate candidate,
        DateTime visitAt)
    {
        return candidate.PopularTimes.FirstOrDefault(x =>
            x.DayOfWeek == visitAt.DayOfWeek &&
            x.HourOfDay == visitAt.Hour);
    }

    private static double CalculateContentCompletenessScore(ExperienceCandidate candidate)
    {
        var points = 0;
        points += string.IsNullOrWhiteSpace(candidate.PrimaryImage) ? 0 : 1;
        points += string.IsNullOrWhiteSpace(candidate.Address) ? 0 : 1;
        points += string.IsNullOrWhiteSpace(candidate.Description) ? 0 : 1;
        points += candidate.Amenities.Count > 0 ? 1 : 0;
        points += candidate.HasReviews ? 1 : 0;
        return points / 5.0;
    }

    private List<ExperienceRecommendationItemResponse> ApplyDiversityReranking(
        IReadOnlyList<ExperienceRecommendationItemResponse> items)
    {
        var remaining = items.ToList();
        var selected = new List<ExperienceRecommendationItemResponse>();
        var categoryCounts = new Dictionary<ExperienceCategory, int>();

        while (remaining.Count > 0 && selected.Count < items.Count)
        {
            var next = remaining
                .OrderByDescending(item =>
                {
                    var count = categoryCounts.GetValueOrDefault(item.Category);
                    return item.FinalScore - count * 3;
                })
                .ThenByDescending(item => item.FinalScore)
                .First();

            selected.Add(next);
            remaining.Remove(next);
            categoryCounts[next.Category] = categoryCounts.GetValueOrDefault(next.Category) + 1;
        }

        return selected;
    }

    private ExperienceRecommendedVisitWindowResponse? BuildRecommendedVisitWindow(
        ExperienceRecommendationPreferences preferences,
        ExperienceCategory category,
        int durationMinutes)
    {
        if (preferences.VisitAtLocal is null)
        {
            return null;
        }

        var start = preferences.VisitAtLocal.Value;
        return new ExperienceRecommendedVisitWindowResponse
        {
            StartLocal = start.ToString("HH:mm", CultureInfo.InvariantCulture),
            EndLocal = start.AddMinutes(durationMinutes).ToString("HH:mm", CultureInfo.InvariantCulture)
        };
    }

    private int GetDefaultDurationMinutes(ExperienceCategory category)
    {
        return category switch
        {
            ExperienceCategory.Nature => Math.Max(15, _options.DefaultNatureDurationMinutes),
            ExperienceCategory.Shopping => Math.Max(15, _options.DefaultShoppingDurationMinutes),
            ExperienceCategory.Nightlife => Math.Max(15, _options.DefaultNightlifeDurationMinutes),
            ExperienceCategory.Dining => Math.Max(15, _options.DefaultDiningDurationMinutes),
            _ => Math.Max(15, _options.DefaultHistoricalDurationMinutes)
        };
    }

    private static string GetTimeOfDayPreference(ExperienceCategory category)
    {
        return category switch
        {
            ExperienceCategory.Nature => "MorningOrGoldenHour",
            ExperienceCategory.Shopping => "AfternoonOrEvening",
            ExperienceCategory.Nightlife => "EveningOrNight",
            ExperienceCategory.Dining => "LunchOrDinner",
            _ => "MorningOrAfternoon"
        };
    }

    private int NormalizeLimit(int? limit, bool forItinerary)
    {
        var max = Math.Clamp(_options.MaxLimit, 1, 100);
        var defaultLimit = forItinerary
            ? Math.Clamp(_options.DefaultItineraryLimit, 1, max)
            : Math.Clamp(_options.DefaultLimit, 1, max);

        return Math.Clamp(limit ?? defaultLimit, 1, max);
    }

    private static ExperienceCategory ParseCategory(string category)
    {
        if (!Enum.TryParse<ExperienceCategory>(category, true, out var parsed))
        {
            throw new ArgumentException(
                "Unsupported experience category. Supported categories: Historical, Nature, Shopping, Nightlife, Dining.");
        }

        return parsed;
    }

    private static double NormalizeWeight(double? weight)
    {
        if (weight is null or <= 0)
        {
            return 0;
        }

        return weight.Value > 1 ? weight.Value / 100 : weight.Value;
    }

    private static string? NormalizeLanguage(string? language)
    {
        if (string.IsNullOrWhiteSpace(language))
        {
            return null;
        }

        var normalized = language.Trim().ToLowerInvariant();
        return normalized.StartsWith("ar", StringComparison.Ordinal) ? "ar" : "en";
    }

    private static string DetectLanguage(string text)
    {
        return text.Any(c => c is >= '\u0600' and <= '\u06FF') ? "ar" : "en";
    }

    private static void AddCategoryIfMentioned(
        ExperienceRecommendationPreferences preferences,
        string normalizedText,
        ExperienceCategory category,
        params string[] terms)
    {
        if (!ContainsAny(normalizedText, terms))
        {
            return;
        }

        preferences.Categories.Add(new WeightedExperienceRecommendationCategory
        {
            Category = category,
            Weight = 1
        });
    }

    private static string NormalizeForMatching(string value)
    {
        var builder = new StringBuilder(value.Length);
        foreach (var character in value.Trim().ToLowerInvariant())
        {
            var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(character);
            if (unicodeCategory == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            builder.Append(char.IsPunctuation(character) || char.IsSymbol(character) ? ' ' : character);
        }

        return string.Join(' ', builder
            .ToString()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    private static bool ContainsAny(string value, params string[] terms)
    {
        return terms.Any(term => value.Contains(term, StringComparison.OrdinalIgnoreCase));
    }

    private static string PickLocalized(RegionName region, bool useArabic)
    {
        return useArabic
            ? FirstConfigured(region.NameAr, region.NameEn)
            : FirstConfigured(region.NameEn, region.NameAr);
    }

    private static string FirstConfigured(params string?[] values)
    {
        return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim() ?? string.Empty;
    }

    private static double CalculateDistanceKm(
        double latitude1,
        double longitude1,
        double latitude2,
        double longitude2)
    {
        const double earthRadiusKm = 6371.0088;

        var lat1 = DegreesToRadians(latitude1);
        var lat2 = DegreesToRadians(latitude2);
        var deltaLat = DegreesToRadians(latitude2 - latitude1);
        var deltaLon = DegreesToRadians(longitude2 - longitude1);

        var a = Math.Sin(deltaLat / 2) * Math.Sin(deltaLat / 2) +
                Math.Cos(lat1) * Math.Cos(lat2) *
                Math.Sin(deltaLon / 2) * Math.Sin(deltaLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return earthRadiusKm * c;
    }

    private static double DegreesToRadians(double degrees)
    {
        return degrees * Math.PI / 180;
    }

    private static double Round(double value)
    {
        return Math.Round(value, 4, MidpointRounding.AwayFromZero);
    }

    private static string? Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = string.Join(' ', value.Split(' ', StringSplitOptions.RemoveEmptyEntries));
        return normalized.Length <= maxLength ? normalized : $"{normalized[..maxLength].Trim()}...";
    }

    private sealed record RecommendationComputation(
        int TotalCandidates,
        List<ExperienceRecommendationItemResponse> Items);

    private sealed record QualityRange(double Min, double Max);

    private sealed record TimingScoreResult(
        double? Score,
        ExperienceOpeningWindowResponse? OpeningWindow);

    private sealed record RegionName(int Gid, string? NameEn, string? NameAr);

    private sealed class ExperienceCandidate
    {
        public int Id { get; set; }
        public ExperienceCategory Category { get; set; }
        public ExperienceSourceType SourceType { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Address { get; set; }
        public int? Adm0Gid { get; set; }
        public int? Adm1Gid { get; set; }
        public int? Adm2Gid { get; set; }
        public int? Adm3Gid { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string? GoogleMapsLink { get; set; }
        public string? PriceRange { get; set; }
        public int? Reviews { get; set; }
        public decimal? Rating { get; set; }
        public string? Website { get; set; }
        public string? PrimaryImage { get; set; }
        public List<HourCandidate> Hours { get; set; } = [];
        public List<PopularTimeCandidate> PopularTimes { get; set; } = [];
        public List<string> Amenities { get; set; } = [];
        public bool HasReviews { get; set; }
    }

    private sealed class HourCandidate
    {
        public DayOfWeek DayOfWeek { get; set; }
        public TimeOnly OpensAt { get; set; }
        public TimeOnly ClosesAt { get; set; }
    }

    private sealed class PopularTimeCandidate
    {
        public DayOfWeek DayOfWeek { get; set; }
        public int HourOfDay { get; set; }
        public int PopularityPercentage { get; set; }
    }
}
