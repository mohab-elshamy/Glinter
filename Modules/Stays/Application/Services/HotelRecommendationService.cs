using System.Globalization;
using System.Text;
using Glinter.Modules.Experiences.Application.Abstractions;
using Glinter.Modules.Regions.Application.Abstractions;
using Glinter.Modules.Stays.Application.Abstractions;
using Glinter.Modules.Stays.Application.Dtos;
using Glinter.Modules.Stays.Application.Options;
using Glinter.Modules.Stays.Domain.Entities;
using Glinter.Modules.Stays.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Glinter.Modules.Stays.Application.Services;

public class HotelRecommendationService
{
    private static readonly string[] SupportedCategories =
    [
        "Historical",
        "Nature",
        "Shopping",
        "Nightlife",
        "Dining"
    ];

    private static readonly Dictionary<int, string> BudgetLabels = new()
    {
        [1] = "Budget",
        [2] = "Economy",
        [3] = "MidRange",
        [4] = "Upscale",
        [5] = "Luxury"
    };

    private readonly StaysDbContext _staysDbContext;
    private readonly IExperienceRecommendationReadService _experienceReadService;
    private readonly IRegionRecommendationReadService _regionReadService;
    private readonly IHotelRecommendationGroqClient _groqClient;
    private readonly HotelRecommendationOptions _options;
    private readonly ILogger<HotelRecommendationService> _logger;

    public HotelRecommendationService(
        StaysDbContext staysDbContext,
        IExperienceRecommendationReadService experienceReadService,
        IRegionRecommendationReadService regionReadService,
        IHotelRecommendationGroqClient groqClient,
        IOptions<HotelRecommendationOptions> options,
        ILogger<HotelRecommendationService> logger)
    {
        _staysDbContext = staysDbContext;
        _experienceReadService = experienceReadService;
        _regionReadService = regionReadService;
        _groqClient = groqClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<HotelRecommendationResponse> RecommendAsync(
        HotelRecommendationRequest request,
        CancellationToken cancellationToken)
    {
        var preferences = NormalizeStructuredRequest(request);
        var result = await RecommendCoreAsync(preferences, cancellationToken);

        return new HotelRecommendationResponse
        {
            Preferences = preferences,
            TotalCandidates = result.TotalCandidates,
            ReturnedCount = result.Items.Count,
            Items = result.Items
        };
    }

    public async Task<NaturalLanguageHotelRecommendationResponse> RecommendFromTextAsync(
        NaturalLanguageHotelRecommendationRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Text))
        {
            throw new ArgumentException("Natural-language recommendation text is required.");
        }

        if (request.Text.Length > _options.NaturalLanguageMaxCharacters)
        {
            throw new ArgumentException(
                $"Natural-language recommendation text cannot exceed {_options.NaturalLanguageMaxCharacters} characters.");
        }

        var language = NormalizeLanguage(request.PreferredLanguage) ?? DetectLanguage(request.Text);
        var classified = await _groqClient.ClassifyAsync(request.Text, language, cancellationToken)
                         ?? ClassifyLocally(request.Text, language);

        classified.Adm0Gid ??= request.Adm0Gid;
        classified.Adm1Gid ??= request.Adm1Gid;
        classified.Adm2Gid ??= request.Adm2Gid;
        classified.Adm3Gid ??= request.Adm3Gid;
        classified.Limit = NormalizeLimit(request.Limit ?? classified.Limit);
        classified.PreferredLanguage = language;

        var preferences = NormalizePreferences(RepairClassifiedPreferences(classified));
        var result = await RecommendCoreAsync(preferences, cancellationToken);

        return new NaturalLanguageHotelRecommendationResponse
        {
            InputText = request.Text,
            ClassificationNotes = classified.Notes,
            Preferences = preferences,
            TotalCandidates = result.TotalCandidates,
            ReturnedCount = result.Items.Count,
            Items = result.Items
        };
    }

    private static HotelRecommendationPreferences RepairClassifiedPreferences(
        HotelRecommendationPreferences preferences)
    {
        preferences.ExperienceCategories = (preferences.ExperienceCategories ?? [])
            .Where(x => !string.IsNullOrWhiteSpace(x.Category))
            .Where(x => SupportedCategories.Contains(
                x.Category,
                StringComparer.OrdinalIgnoreCase))
            .ToList();

        preferences.RequestedAmenities = (preferences.RequestedAmenities ?? [])
            .Select(NormalizeAmenityTag)
            .Where(x => x is not null)
            .Select(x => x!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (preferences.BudgetLevel is < 1 or > 5)
        {
            preferences.BudgetLevel = null;
        }

        return preferences;
    }

    public static string? NormalizeAmenityTag(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = NormalizeForMatching(value);

        if (ContainsAny(normalized, "wifi", "wi fi", "wireless", "واي فاي"))
        {
            return "WiFi";
        }

        if (ContainsAny(normalized, "gym", "fitness", "نادي رياضي", "جيم", "لياقة"))
        {
            return "Gym";
        }

        if (ContainsAny(normalized, "pool", "swimming", "مسبح", "حمام سباحة"))
        {
            return "Pool";
        }

        if (ContainsAny(normalized, "spa", "سبا", "منتجع صحي"))
        {
            return "Spa";
        }

        if (ContainsAny(normalized, "restaurant", "dining", "مطعم", "مطاعم"))
        {
            return "Restaurant";
        }

        if (ContainsAny(normalized, "bar", "lounge", "بار"))
        {
            return "Bar";
        }

        if (ContainsAny(normalized, "parking", "garage", "موقف", "ركن", "جراج"))
        {
            return "Parking";
        }

        var words = normalized
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(word => CultureInfo.InvariantCulture.TextInfo.ToTitleCase(word));

        return string.Join(' ', words);
    }

    private async Task<RecommendationComputation> RecommendCoreAsync(
        HotelRecommendationPreferences preferences,
        CancellationToken cancellationToken)
    {
        var candidates = await GetCandidateHotelsAsync(preferences, cancellationToken);
        if (candidates.Count == 0)
        {
            return new RecommendationComputation(0, []);
        }

        var budgetLevels = await CalculateBudgetLevelsAsync(candidates, preferences, cancellationToken);
        var experiences = await GetRelevantExperiencesAsync(preferences, cancellationToken);
        var regionContexts = await GetRegionContextsAsync(candidates, cancellationToken);
        var qualityRange = CalculateQualityRange(candidates);

        var scored = candidates
            .Select(candidate => ScoreCandidate(
                candidate,
                preferences,
                budgetLevels,
                experiences,
                qualityRange,
                regionContexts))
            .OrderByDescending(x => x.FinalScore)
            .ThenByDescending(x => x.Rating ?? 0)
            .ThenByDescending(x => x.Reviews ?? 0)
            .ThenBy(x => x.Price ?? decimal.MaxValue)
            .ThenBy(x => x.Name)
            .Take(preferences.Limit)
            .ToList();

        var explanationMap = await TryGenerateAiExplanationsAsync(preferences, scored, cancellationToken);

        for (var index = 0; index < scored.Count; index++)
        {
            var item = scored[index];
            item.Ranking = index + 1;

            if (explanationMap?.TryGetValue(item.HotelId, out var explanation) == true)
            {
                explanation.IsAiGenerated = true;
                item.Explanation = explanation;
            }
            else
            {
                item.Explanation = BuildFallbackExplanation(preferences, item);
            }
        }

        return new RecommendationComputation(candidates.Count, scored);
    }

    private async Task<List<HotelCandidate>> GetCandidateHotelsAsync(
        HotelRecommendationPreferences preferences,
        CancellationToken cancellationToken)
    {
        var query = ApplyAdminFilters(_staysDbContext.Stays.AsNoTracking(), preferences)
            .Where(x =>
                x.IsActive &&
                x.Latitude != null &&
                x.Longitude != null);

        var hotels = await query
            .Include(x => x.Amenities)
            .OrderByDescending(x => x.Rating)
            .ThenByDescending(x => x.Reviews)
            .ThenBy(x => x.Price)
            .ThenBy(x => x.Name)
            .Take(Math.Clamp(_options.MaxCandidateHotels, 1, 2000))
            .AsSplitQuery()
            .Select(x => new HotelCandidate
            {
                Id = x.Id,
                Name = x.Name,
                Price = x.Price,
                Description = x.Description,
                LocationSummaryDescription = x.LocationSummaryDescription,
                Rating = x.Rating,
                Reviews = x.Reviews,
                Latitude = x.Latitude!.Value,
                Longitude = x.Longitude!.Value,
                Adm0Gid = x.Adm0Gid,
                Adm1Gid = x.Adm1Gid,
                Adm2Gid = x.Adm2Gid,
                Adm3Gid = x.Adm3Gid,
                GoogleMapsLink = x.GoogleMapsLink,
                Website = x.Website,
                PhoneInternational = x.PhoneInternational,
                Amenities = x.Amenities
                    .Select(a => new AmenityCandidate
                    {
                        NameAr = a.NameAr,
                        NameEn = a.NameEn
                    })
                    .ToList()
            })
            .ToListAsync(cancellationToken);

        foreach (var hotel in hotels)
        {
            hotel.AmenityNames = hotel.Amenities
                .SelectMany(x => new[] { x.NameEn, x.NameAr })
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x!)
                .ToList();
        }

        return hotels;
    }

    private async Task<Dictionary<int, int?>> CalculateBudgetLevelsAsync(
        IReadOnlyList<HotelCandidate> candidates,
        HotelRecommendationPreferences preferences,
        CancellationToken cancellationToken)
    {
        var localPrices = await ApplyAdminFilters(
                _staysDbContext.Stays.AsNoTracking(),
                preferences)
            .Where(x => x.IsActive && x.Price != null && x.Price > 0)
            .Select(x => x.Price!.Value)
            .OrderBy(x => x)
            .ToListAsync(cancellationToken);

        var distribution = localPrices.Count >= Math.Max(1, _options.MinLocalPriceSampleSize)
            ? localPrices
            : await GetGlobalPriceDistributionAsync(cancellationToken);

        var result = new Dictionary<int, int?>();

        foreach (var candidate in candidates)
        {
            if (candidate.Price is null or <= 0 || distribution.Count == 0)
            {
                result[candidate.Id] = null;
                continue;
            }

            result[candidate.Id] = CalculateBudgetLevel(candidate.Price.Value, distribution);
        }

        return result;
    }

    private async Task<List<decimal>> GetGlobalPriceDistributionAsync(
        CancellationToken cancellationToken)
    {
        return await _staysDbContext.Stays
            .AsNoTracking()
            .Where(x => x.IsActive && x.Price != null && x.Price > 0)
            .OrderBy(x => x.Price)
            .Select(x => x.Price!.Value)
            .ToListAsync(cancellationToken);
    }

    private async Task<Dictionary<string, List<ExperienceRecommendationCandidate>>> GetRelevantExperiencesAsync(
        HotelRecommendationPreferences preferences,
        CancellationToken cancellationToken)
    {
        if (preferences.ExperienceCategories.Count == 0)
        {
            return [];
        }

        var categories = preferences.ExperienceCategories
            .Select(x => x.Category)
            .ToArray();

        var experiences = await _experienceReadService.GetCandidatesAsync(
            new ExperienceRecommendationQuery(
                categories,
                preferences.Adm0Gid,
                preferences.Adm1Gid,
                preferences.Adm2Gid,
                preferences.Adm3Gid,
                Math.Clamp(_options.MaxExperiencesPerCategory, 1, 5000)),
            cancellationToken);

        return experiences
            .GroupBy(x => x.Category)
            .ToDictionary(
                x => x.Key,
                x => x.ToList(),
                StringComparer.OrdinalIgnoreCase);
    }

    private async Task<Dictionary<int, HotelRecommendationRegionResponse>> GetRegionContextsAsync(
        IReadOnlyList<HotelCandidate> candidates,
        CancellationToken cancellationToken)
    {
        var resolved = await _regionReadService.ResolveAsync(
            candidates
                .Select(x => new RegionRecommendationReference(
                    x.Id,
                    x.Adm0Gid,
                    x.Adm1Gid,
                    x.Adm2Gid,
                    x.Adm3Gid))
                .ToArray(),
            cancellationToken);

        return resolved.ToDictionary(
            x => x.Key,
            x => new HotelRecommendationRegionResponse
            {
                CountryNameEn = x.Value.CountryNameEn,
                CountryNameAr = x.Value.CountryNameAr,
                GovernorateNameEn = x.Value.GovernorateNameEn,
                GovernorateNameAr = x.Value.GovernorateNameAr,
                DistrictNameEn = x.Value.DistrictNameEn,
                DistrictNameAr = x.Value.DistrictNameAr,
                NeighbourhoodNameEn = x.Value.NeighbourhoodNameEn,
                NeighbourhoodNameAr = x.Value.NeighbourhoodNameAr,
                DisplayName = x.Value.DisplayName
            });
    }

    private HotelRecommendationItemResponse ScoreCandidate(
        HotelCandidate candidate,
        HotelRecommendationPreferences preferences,
        IReadOnlyDictionary<int, int?> budgetLevels,
        IReadOnlyDictionary<string, List<ExperienceRecommendationCandidate>> experiences,
        QualityRange qualityRange,
        IReadOnlyDictionary<int, HotelRecommendationRegionResponse> regionContexts)
    {
        var budgetLevel = budgetLevels.TryGetValue(candidate.Id, out var level) ? level : null;
        var budgetScore = CalculateBudgetMatchScore(preferences.BudgetLevel, budgetLevel);
        var qualityScore = CalculateNormalizedQualityScore(candidate, qualityRange);
        var amenityTags = candidate.AmenityNames
            .Select(NormalizeAmenityTag)
            .Where(x => x is not null)
            .Select(x => x!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x)
            .ToList();

        var matchedAmenities = preferences.RequestedAmenities
            .Where(x => amenityTags.Contains(x, StringComparer.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x)
            .ToList();

        var amenityScore = preferences.RequestedAmenities.Count == 0
            ? (double?)null
            : (double)matchedAmenities.Count / preferences.RequestedAmenities.Count;

        var proximity = CalculateInterestProximity(candidate, preferences, experiences);

        var weightedScores = new List<(double Weight, double Score)>();
        if (proximity.Score is not null)
        {
            weightedScores.Add((0.40, proximity.Score.Value));
        }

        if (budgetScore is not null)
        {
            weightedScores.Add((0.25, budgetScore.Value));
        }

        weightedScores.Add((0.25, qualityScore));

        if (amenityScore is not null)
        {
            weightedScores.Add((0.10, amenityScore.Value));
        }

        var totalWeight = weightedScores.Sum(x => x.Weight);
        var finalScore = totalWeight <= 0
            ? 0
            : weightedScores.Sum(x => x.Weight * x.Score) / totalWeight * 100;

        return new HotelRecommendationItemResponse
        {
            HotelId = candidate.Id,
            Name = candidate.Name,
            Price = candidate.Price,
            Description = Truncate(candidate.Description, 700),
            LocationSummaryDescription = Truncate(candidate.LocationSummaryDescription, 500),
            BudgetLevel = budgetLevel,
            BudgetLabel = budgetLevel is null ? null : BudgetLabels[budgetLevel.Value],
            Rating = candidate.Rating,
            Reviews = candidate.Reviews,
            Latitude = candidate.Latitude,
            Longitude = candidate.Longitude,
            Adm0Gid = candidate.Adm0Gid,
            Adm1Gid = candidate.Adm1Gid,
            Adm2Gid = candidate.Adm2Gid,
            Adm3Gid = candidate.Adm3Gid,
            Region = regionContexts.TryGetValue(candidate.Id, out var region) ? region : null,
            GoogleMapsLink = candidate.GoogleMapsLink,
            Website = candidate.Website,
            PhoneInternational = candidate.PhoneInternational,
            FinalScore = Round(finalScore),
            Scores = new HotelRecommendationScoreBreakdown
            {
                InterestProximityScore = proximity.Score is null ? null : Round(proximity.Score.Value),
                BudgetMatchScore = budgetScore is null ? null : Round(budgetScore.Value),
                HotelQualityScore = Round(qualityScore),
                AmenityMatchScore = amenityScore is null ? null : Round(amenityScore.Value)
            },
            Amenities = amenityTags,
            MatchedAmenities = matchedAmenities,
            NearbyExperiences = proximity.NearbyExperiences
        };
    }

    private InterestProximityResult CalculateInterestProximity(
        HotelCandidate candidate,
        HotelRecommendationPreferences preferences,
        IReadOnlyDictionary<string, List<ExperienceRecommendationCandidate>> experiences)
    {
        if (preferences.ExperienceCategories.Count == 0)
        {
            return new InterestProximityResult(null, []);
        }

        var nearestPerCategory = Math.Clamp(_options.NearestExperiencesPerCategory, 1, 20);
        var maxDistance = Math.Max(1, _options.MaxUsefulDistanceKm);
        var score = 0.0;
        var nearby = new List<NearbyExperienceSummaryResponse>();
        var availableCategories = preferences.ExperienceCategories
            .Where(x =>
                experiences.TryGetValue(x.Category, out var candidates) &&
                candidates.Count > 0)
            .ToArray();

        if (availableCategories.Length == 0)
        {
            return new InterestProximityResult(null, []);
        }

        var availableWeight = availableCategories.Sum(x => x.Weight);
        if (availableWeight <= 0)
        {
            availableWeight = availableCategories.Length;
        }

        foreach (var categoryPreference in availableCategories)
        {
            if (!experiences.TryGetValue(
                    categoryPreference.Category,
                    out var categoryExperiences) ||
                categoryExperiences.Count == 0)
            {
                continue;
            }

            var nearest = categoryExperiences
                .Select(x => new
                {
                    Experience = x,
                    DistanceKm = CalculateDistanceKm(
                        candidate.Latitude,
                        candidate.Longitude,
                        x.Latitude,
                        x.Longitude)
                })
                .OrderBy(x => x.DistanceKm)
                .Take(nearestPerCategory)
                .ToList();

            if (nearest.Count == 0)
            {
                continue;
            }

            var averageDistance = nearest.Average(x => x.DistanceKm);
            var categoryScore = Math.Clamp(1 - averageDistance / maxDistance, 0, 1);
            var redistributedWeight = availableCategories.Sum(x => x.Weight) > 0
                ? categoryPreference.Weight / availableWeight
                : 1.0 / availableCategories.Length;
            score += categoryScore * redistributedWeight;

            nearby.AddRange(nearest.Select(x => new NearbyExperienceSummaryResponse
            {
                ExperienceId = x.Experience.Id,
                Name = x.Experience.Name,
                Category = x.Experience.Category,
                DistanceKm = Round(x.DistanceKm),
                Rating = x.Experience.Rating,
                Reviews = x.Experience.Reviews
            }));
        }

        return new InterestProximityResult(score, nearby
            .OrderBy(x => x.DistanceKm)
            .ThenByDescending(x => x.Rating ?? 0)
            .Take(availableCategories.Length * nearestPerCategory)
            .ToList());
    }

    private async Task<IReadOnlyDictionary<int, HotelRecommendationExplanationResponse>?> TryGenerateAiExplanationsAsync(
        HotelRecommendationPreferences preferences,
        IReadOnlyList<HotelRecommendationItemResponse> scored,
        CancellationToken cancellationToken)
    {
        if (scored.Count == 0)
        {
            return null;
        }

        try
        {
            var request = new HotelRecommendationRequest
            {
                BudgetLevel = preferences.BudgetLevel,
                ExperienceCategories = preferences.ExperienceCategories
                    .Select(x => new HotelRecommendationCategoryPreference
                    {
                        Category = x.Category,
                        Weight = x.Weight
                    })
                    .ToList(),
                RequestedAmenities = preferences.RequestedAmenities,
                Adm0Gid = preferences.Adm0Gid,
                Adm1Gid = preferences.Adm1Gid,
                Adm2Gid = preferences.Adm2Gid,
                Adm3Gid = preferences.Adm3Gid,
                Limit = preferences.Limit,
                PreferredLanguage = preferences.PreferredLanguage
            };

            return await _groqClient.GenerateExplanationsAsync(request, scored, cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or OperationCanceledException or System.Text.Json.JsonException)
        {
            if (ex is OperationCanceledException && cancellationToken.IsCancellationRequested)
            {
                throw;
            }

            _logger.LogWarning(ex, "Groq hotel recommendation explanations failed; using local fallback explanations.");
            return null;
        }
    }

    private HotelRecommendationExplanationResponse BuildFallbackExplanation(
        HotelRecommendationPreferences preferences,
        HotelRecommendationItemResponse item)
    {
        var isArabic = preferences.PreferredLanguage.Equals("ar", StringComparison.OrdinalIgnoreCase);
        var reasons = new List<string>();
        var bestFor = new List<string>();

        if (preferences.BudgetLevel is not null && item.BudgetLevel is not null)
        {
            reasons.Add(isArabic
                ? $"يناسب مستوى الميزانية {item.BudgetLabel} بدرجة {item.Scores.BudgetMatchScore:0.##}."
                : $"It fits the {item.BudgetLabel} budget level with a {item.Scores.BudgetMatchScore:0.##} budget score.");
            bestFor.Add(item.BudgetLabel!);
        }

        if (item.Scores.InterestProximityScore is not null && preferences.ExperienceCategories.Count > 0)
        {
            var categoryText = string.Join(", ", preferences.ExperienceCategories.Select(x => x.Category));
            reasons.Add(isArabic
                ? $"قريب نسبيا من اهتماماتك: {categoryText}."
                : $"It is relatively close to your selected interests: {categoryText}.");
            bestFor.AddRange(preferences.ExperienceCategories.Select(x => x.Category));
        }

        if (item.Rating is not null || item.Reviews is not null)
        {
            reasons.Add(isArabic
                ? $"إشارة الجودة مبنية على تقييم {item.Rating?.ToString(CultureInfo.InvariantCulture) ?? "غير متاح"} وعدد مراجعات {item.Reviews ?? 0}."
                : $"Quality signal uses rating {item.Rating?.ToString(CultureInfo.InvariantCulture) ?? "unavailable"} and {item.Reviews ?? 0} reviews.");
        }

        if (item.MatchedAmenities.Count > 0)
        {
            reasons.Add(isArabic
                ? $"يوفر مرافق طلبتها: {string.Join(", ", item.MatchedAmenities)}."
                : $"It matches requested amenities: {string.Join(", ", item.MatchedAmenities)}.");
            bestFor.AddRange(item.MatchedAmenities);
        }

        if (reasons.Count == 0)
        {
            reasons.Add(isArabic
                ? "تم ترتيبه بناء على جودة الفندق والبيانات المتاحة."
                : "It ranked well based on hotel quality and available data.");
        }

        return new HotelRecommendationExplanationResponse
        {
            ShortExplanation = isArabic
                ? $"{item.Name} خيار مناسب بدرجة {item.FinalScore:0.#} من 100."
                : $"{item.Name} is a suitable match with a {item.FinalScore:0.#}/100 score.",
            Reasons = reasons.Take(3).ToList(),
            BestFor = bestFor.Distinct(StringComparer.OrdinalIgnoreCase).Take(4).ToList(),
            IsAiGenerated = false
        };
    }

    private HotelRecommendationPreferences NormalizeStructuredRequest(HotelRecommendationRequest request)
    {
        return NormalizePreferences(new HotelRecommendationPreferences
        {
            BudgetLevel = request.BudgetLevel,
            ExperienceCategories = (request.ExperienceCategories ?? [])
                .Select(x => new WeightedExperienceCategoryPreference
                {
                    Category = x.Category,
                    Weight = x.Weight ?? 0
                })
                .ToList(),
            RequestedAmenities = request.RequestedAmenities ?? [],
            Adm0Gid = request.Adm0Gid,
            Adm1Gid = request.Adm1Gid,
            Adm2Gid = request.Adm2Gid,
            Adm3Gid = request.Adm3Gid,
            Limit = NormalizeLimit(request.Limit),
            PreferredLanguage = NormalizeLanguage(request.PreferredLanguage) ?? "en"
        });
    }

    private HotelRecommendationPreferences NormalizePreferences(HotelRecommendationPreferences preferences)
    {
        if (preferences.BudgetLevel is < 1 or > 5)
        {
            throw new ArgumentException("Budget level must be between 1 and 5.");
        }

        preferences.Limit = NormalizeLimit(preferences.Limit);
        preferences.PreferredLanguage = NormalizeLanguage(preferences.PreferredLanguage) ?? "en";
        preferences.BudgetLabel = preferences.BudgetLevel is null ? null : BudgetLabels[preferences.BudgetLevel.Value];

        var categories = (preferences.ExperienceCategories ?? [])
            .Where(x => !string.IsNullOrWhiteSpace(x.Category))
            .GroupBy(x => x.Category.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(x => new WeightedExperienceCategoryPreference
            {
                Category = NormalizeCategory(x.Key),
                Weight = x.Sum(y => NormalizeWeight(y.Weight))
            })
            .ToList();

        if (categories.Any(x => x.Category.Length == 0))
        {
            var supported = string.Join(", ", SupportedCategories);
            throw new ArgumentException($"Unsupported experience category. Supported categories: {supported}.");
        }

        if (categories.Count > SupportedCategories.Length)
        {
            throw new ArgumentException(
                $"No more than {SupportedCategories.Length} distinct experience categories may be requested.");
        }

        var positiveTotal = categories.Sum(x => x.Weight);
        if (categories.Count > 0)
        {
            if (positiveTotal <= 0)
            {
                var equal = 1.0 / categories.Count;
                foreach (var category in categories)
                {
                    category.Weight = equal;
                }
            }
            else
            {
                foreach (var category in categories)
                {
                    category.Weight = category.Weight / positiveTotal;
                }
            }
        }

        preferences.ExperienceCategories = categories
            .OrderByDescending(x => x.Weight)
            .ThenBy(x => x.Category)
            .ToList();

        preferences.RequestedAmenities = (preferences.RequestedAmenities ?? [])
            .Select(NormalizeAmenityTag)
            .Where(x => x is not null)
            .Select(x => x!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x)
            .ToList();

        if (preferences.RequestedAmenities.Count > _options.MaxRequestedAmenities)
        {
            throw new ArgumentException(
                $"No more than {_options.MaxRequestedAmenities} distinct amenities may be requested.");
        }

        return preferences;
    }

    private HotelRecommendationPreferences ClassifyLocally(string text, string language)
    {
        var normalized = NormalizeForMatching(text);
        var preferences = new HotelRecommendationPreferences
        {
            PreferredLanguage = language,
            ClassificationConfidence = 0.45,
            Notes = "Local fallback classification was used because Groq was unavailable or returned invalid JSON."
        };

        if (ContainsAny(normalized, "midrange", "mid range", "متوسط", "متوسطة"))
        {
            preferences.BudgetLevel = 3;
        }
        else if (ContainsAny(normalized, "luxury", "فاخر", "فخم", "راقي"))
        {
            preferences.BudgetLevel = 5;
        }
        else if (ContainsAny(normalized, "cheap", "budget", "رخيص"))
        {
            preferences.BudgetLevel = 1;
        }
        else if (ContainsAny(normalized, "economy", "اقتصادي"))
        {
            preferences.BudgetLevel = 2;
        }
        else if (ContainsAny(normalized, "upscale", "premium"))
        {
            preferences.BudgetLevel = 4;
        }

        AddCategoryIfMentioned(preferences, normalized, "Historical", "historical", "history", "اثري", "تاريخي", "الأماكن التاريخية", "اماكن تاريخية");
        AddCategoryIfMentioned(preferences, normalized, "Nature", "nature", "park", "beach", "طبيعة", "حديقة", "بحر");
        AddCategoryIfMentioned(preferences, normalized, "Shopping", "shopping", "mall", "market", "تسوق", "مول", "سوق");
        AddCategoryIfMentioned(preferences, normalized, "Nightlife", "nightlife", "club", "night", "سهر", "ليل", "نايت");
        AddCategoryIfMentioned(preferences, normalized, "Dining", "dining", "restaurant", "food", "مطاعم", "مطعم", "اكل");

        foreach (var amenity in new[] { "wifi", "واي فاي", "gym", "جيم", "pool", "مسبح", "spa", "سبا", "restaurant", "مطعم", "bar", "بار", "parking", "موقف" })
        {
            if (normalized.Contains(amenity, StringComparison.OrdinalIgnoreCase))
            {
                var tag = NormalizeAmenityTag(amenity);
                if (tag is not null)
                {
                    preferences.RequestedAmenities.Add(tag);
                }
            }
        }

        return preferences;
    }

    private static void AddCategoryIfMentioned(
        HotelRecommendationPreferences preferences,
        string normalizedText,
        string category,
        params string[] terms)
    {
        if (!ContainsAny(normalizedText, terms))
        {
            return;
        }

        preferences.ExperienceCategories.Add(new WeightedExperienceCategoryPreference
        {
            Category = category.ToString(),
            Weight = 1
        });
    }

    private static IQueryable<Stay> ApplyAdminFilters(
        IQueryable<Stay> query,
        HotelRecommendationPreferences preferences)
    {
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

        return query;
    }

    private static QualityRange CalculateQualityRange(IReadOnlyList<HotelCandidate> candidates)
    {
        var values = candidates.Select(CalculateRawQualityScore).ToList();
        return new QualityRange(values.Min(), values.Max());
    }

    private static double CalculateNormalizedQualityScore(HotelCandidate candidate, QualityRange range)
    {
        var raw = CalculateRawQualityScore(candidate);
        if (Math.Abs(range.Max - range.Min) < 0.000001)
        {
            return 0.5;
        }

        return Math.Clamp((raw - range.Min) / (range.Max - range.Min), 0, 1);
    }

    private static double CalculateRawQualityScore(HotelCandidate candidate)
    {
        var rating = Math.Max(0, (double)(candidate.Rating ?? 0));
        var reviews = Math.Max(0, candidate.Reviews ?? 0);
        return rating * Math.Log(reviews + 1);
    }

    private static double? CalculateBudgetMatchScore(int? userBudgetLevel, int? hotelBudgetLevel)
    {
        if (userBudgetLevel is null)
        {
            return null;
        }

        if (hotelBudgetLevel is null)
        {
            return 0.5;
        }

        return Math.Clamp(1 - Math.Abs(userBudgetLevel.Value - hotelBudgetLevel.Value) / 4.0, 0, 1);
    }

    private static int CalculateBudgetLevel(
        decimal price,
        IReadOnlyList<decimal> sortedPrices)
    {
        var p20 = PercentileThreshold(sortedPrices, 0.20);
        var p40 = PercentileThreshold(sortedPrices, 0.40);
        var p60 = PercentileThreshold(sortedPrices, 0.60);
        var p80 = PercentileThreshold(sortedPrices, 0.80);

        if (price <= p20) return 1;
        if (price <= p40) return 2;
        if (price <= p60) return 3;
        if (price <= p80) return 4;
        return 5;
    }

    private static decimal PercentileThreshold(
        IReadOnlyList<decimal> sortedPrices,
        double percentile)
    {
        if (sortedPrices.Count == 0)
            throw new ArgumentException("A price distribution is required.");

        var index = (int)Math.Ceiling(sortedPrices.Count * percentile) - 1;
        return sortedPrices[Math.Clamp(index, 0, sortedPrices.Count - 1)];
    }

    private int NormalizeLimit(int? limit)
    {
        var max = Math.Clamp(_options.MaxLimit, 1, 100);
        var defaultLimit = Math.Clamp(_options.DefaultLimit, 1, max);
        return Math.Clamp(limit ?? defaultLimit, 1, max);
    }

    private static string NormalizeCategory(string category)
    {
        return SupportedCategories.FirstOrDefault(
                   x => x.Equals(category.Trim(), StringComparison.OrdinalIgnoreCase))
               ?? string.Empty;
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
        List<HotelRecommendationItemResponse> Items);

    private sealed record QualityRange(double Min, double Max);

    private sealed record InterestProximityResult(
        double? Score,
        List<NearbyExperienceSummaryResponse> NearbyExperiences);

    private sealed class HotelCandidate
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal? Price { get; set; }
        public string? Description { get; set; }
        public string? LocationSummaryDescription { get; set; }
        public decimal? Rating { get; set; }
        public int? Reviews { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public int? Adm0Gid { get; set; }
        public int? Adm1Gid { get; set; }
        public int? Adm2Gid { get; set; }
        public int? Adm3Gid { get; set; }
        public string? GoogleMapsLink { get; set; }
        public string? Website { get; set; }
        public string? PhoneInternational { get; set; }
        public List<AmenityCandidate> Amenities { get; set; } = [];
        public List<string> AmenityNames { get; set; } = [];
    }

    private sealed class AmenityCandidate
    {
        public string? NameAr { get; set; }
        public string? NameEn { get; set; }
    }

}
