# Experience Recommendations

The Experiences module now exposes deterministic, route-ready recommendations that can feed the Routing module when building a client itinerary.

Groq is used only for:

- Natural-language preference classification.
- Human-friendly explanations after backend ranking is complete.

Groq must not decide ranking, reorder results, or invent unavailable facts.

## Endpoints

```http
POST /api/experiences/recommendations
POST /api/experiences/recommendations/natural-language
```

The first endpoint accepts structured preferences. The second accepts free text, uses Groq when configured, validates the classification, then runs the same backend scoring pipeline.

## Supported Categories

The recommendation system supports the current enum values:

- `Historical`
- `Nature`
- `Shopping`
- `Nightlife`
- `Dining`

The algorithm is not hardcoded to the current database distribution. New categories can be added later by extending the enum, default duration mapping, and Groq prompt/category validation.

## Current Data Reality

The local `glinter` database currently has useful data for:

- Experience coordinates.
- Rating and review volume.
- Administrative region IDs.
- Address.
- Review distribution and review rows.
- Featured images for some experiences.
- Opening hours for some experiences.
- Popular times for some experiences.
- Amenities for a small subset.

The current database does not yet have reliable recommendation data for:

- Availability slots.
- Capacity and remaining capacity.
- Bookings.
- Provider-only pricing.
- Category diversity across all five categories.

Availability, capacity, price, opening hours, popular times, amenities, and descriptions are therefore optional signals. Missing optional data does not crash scoring and does not unfairly penalize an otherwise good experience.

`bookableOnly=true` currently returns no results until reliable availability/capacity data exists in the active database.

## Structured Request

```json
{
  "categories": [
    { "category": "Historical", "weight": 70 },
    { "category": "Dining", "weight": 30 }
  ],
  "latitude": 30.0444,
  "longitude": 31.2357,
  "adm0Gid": null,
  "adm1Gid": null,
  "adm2Gid": null,
  "adm3Gid": null,
  "visitAtLocal": "2026-07-02T18:00:00",
  "guestsCount": 2,
  "crowdPreference": "Quiet",
  "bookableOnly": false,
  "forItinerary": true,
  "limit": 30,
  "preferredLanguage": "ar"
}
```

### Request Fields

`categories`
: Optional category preferences with optional weights. Weights can be `0-1`, `0-100`, or omitted. They are normalized internally.

`latitude` and `longitude`
: Optional start point, hotel point, or user point. When both are present, distance scoring is active. They must be provided together.

`adm0Gid`, `adm1Gid`, `adm2Gid`, `adm3Gid`
: Optional administrative filters.

`visitAtLocal`
: Optional local visit datetime. Used for opening-hours and popular-times scoring when data exists.

`guestsCount`
: Optional. Reserved for availability/capacity scoring once reliable data exists.

`crowdPreference`
: Optional values: `Quiet`, `Balanced`, `Lively`.

`bookableOnly`
: Strictly filters to bookable experiences. Keep it `false` for current third-party data.

`forItinerary`
: Returns a larger route-ready candidate pool and applies light diversity reranking.

`preferredLanguage`
: `ar` or `en` for explanations and region display names.

## Natural-Language Request

```json
{
  "text": "عايز أماكن تاريخية ومطاعم قريبة مني بعد المغرب ومش زحمة",
  "latitude": 30.0444,
  "longitude": 31.2357,
  "visitAtLocal": "2026-07-02T18:00:00",
  "forItinerary": true,
  "limit": 30,
  "preferredLanguage": "ar"
}
```

Groq returns strict JSON classification. The backend then repairs unsupported values, normalizes weights, applies caller-provided location/admin/time overrides, and scores deterministically.

If Groq is missing, times out, returns invalid JSON, or returns an HTTP error, the service falls back to a simple local classifier and deterministic local explanations.

## Scoring

Final score is `0-100`. Available factors are weighted and missing/unrequested factors are excluded, redistributing weight across the remaining factors.

Default factor weights:

```text
CategoryMatchScore          0.20
DistanceScore               0.20
QualityScore                0.20
TimingOpenScore             0.15
CrowdPreferenceScore        0.10
ContentCompletenessScore    0.05
```

Availability is represented in the response but currently not scored because reliable slot/capacity data is not available in the active database.

### Category Match

If categories are selected, exact matches score according to normalized category weights. If no category preference is provided, this factor is excluded.

### Distance

Uses straight-line Haversine distance in kilometers. No road routing is done in this module.

```text
DistanceScore = max(0, 1 - (DistanceKm / MaxUsefulDistanceKm))
```

`MaxUsefulDistanceKm` defaults to `20` and is configurable.

### Quality

```text
RawQualityScore = Rating * log(Reviews + 1)
```

Raw quality is normalized across the candidate set. If all candidates have the same raw quality, a neutral `0.5` is used.

### Timing

When `visitAtLocal` is supplied and opening-hour rows exist, the score prefers experiences open at that time. If hours are missing, the timing factor is excluded and `openHoursDataAvailable=false`.

### Crowd Preference

When `visitAtLocal`, `crowdPreference`, and matching popular-times rows exist:

- `Quiet` prefers lower popularity.
- `Balanced` prefers medium popularity.
- `Lively` prefers higher popularity.

If popular-time data is missing, the factor is excluded and `popularTimesDataAvailable=false`.

### Content Completeness

Adds a small signal for practical itinerary usefulness: image, description, address, website, reviews, hours, popular times, and amenities.

## Response Shape

Each item includes:

- Ranking and final score.
- Score breakdown.
- Category, coordinates, address, region display name.
- Rating and review count.
- Straight-line distance when a point was provided.
- Estimated duration and duration source.
- Recommended visit window.
- Opening window when available.
- Data availability flags.
- Routing hints such as `clusterKey` and `timeOfDayPreference`.
- AI or fallback explanation.

Example:

```json
{
  "preferences": {
    "categories": [{ "category": "Historical", "weight": 1 }],
    "latitude": 30.0444,
    "longitude": 31.2357,
    "forItinerary": true,
    "limit": 30,
    "preferredLanguage": "ar"
  },
  "totalCandidates": 120,
  "returnedCount": 30,
  "items": [
    {
      "ranking": 1,
      "experienceId": 12,
      "name": "Example Museum",
      "category": "Historical",
      "regionDisplayName": "Cairo",
      "distanceKm": 2.4,
      "rating": 4.6,
      "reviews": 1800,
      "estimatedDurationMinutes": 90,
      "routingHints": {
        "mustVisitAtFixedTime": false,
        "requiresBooking": false,
        "clusterKey": "adm2:123",
        "timeOfDayPreference": "MorningOrAfternoon"
      },
      "finalScore": 87.3,
      "scores": {
        "categoryMatchScore": 1,
        "distanceScore": 0.88,
        "qualityScore": 0.91,
        "timingOpenScore": null,
        "crowdPreferenceScore": null,
        "availabilityScore": null,
        "contentCompletenessScore": 0.75
      },
      "explanation": {
        "shortExplanation": "A strong historical match near your start point with solid review confidence.",
        "reasons": [
          "Matches the selected Historical category.",
          "Close to the provided start point.",
          "Rating and review volume make it a reliable itinerary candidate."
        ],
        "bestFor": ["Historical", "Itinerary"],
        "isAiGenerated": true
      }
    }
  ]
}
```

## Routing Integration Notes

For itinerary building, call recommendations with `forItinerary=true` and a larger `limit`, usually `20-40`.

Use these response fields in Routing:

- `experienceId`
- `latitude`
- `longitude`
- `estimatedDurationMinutes`
- `recommendedVisitWindow`
- `openingWindow`
- `openHoursDataAvailable`
- `nextAvailableSlot`
- `routingHints.mustVisitAtFixedTime`
- `routingHints.requiresBooking`
- `routingHints.clusterKey`
- `routingHints.timeOfDayPreference`
- `finalScore`

The Routing module should still calculate real route order and travel time. This recommendation feature only provides candidate quality, straight-line proximity, soft timing hints, and route-friendly metadata.

## Configuration

```json
{
  "ExperienceRecommendations": {
    "GroqApiKey": "",
    "GroqModel": "qwen/qwen3-32b",
    "GroqBaseUrl": "https://api.groq.com/openai/v1",
    "GroqRequestTimeoutSeconds": 45,
    "GroqClassificationMaxTokens": 500,
    "GroqExplanationMaxTokens": 750,
    "GroqMaxExplanationItems": 8,
    "GroqRetryMaxExplanationItems": 3,
    "DefaultLimit": 10,
    "DefaultItineraryLimit": 30,
    "MaxLimit": 40,
    "MaxCandidateExperiences": 600,
    "MaxUsefulDistanceKm": 20
  }
}
```

The service also reads `GROQ_API_KEY`, `GROQ_MODEL`, and `GROQ_BASE_URL` from environment variables. If the experience-specific Groq key is empty, it can reuse the existing hotel or safety-index Groq configuration.
