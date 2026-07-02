# Experience Recommendations

This document describes the planned recommendation feature for the Experiences module. The goal is to return high-quality, route-ready experience candidates that can later feed the Routing module to build a client itinerary.

Groq may be used for natural-language preference extraction and human-friendly explanations only. Ranking, scoring, filtering, and itinerary readiness must stay deterministic in backend code.

## Current Data Reality

The local `glinter` database currently has useful data for:

- Coordinates for experiences.
- Rating and review volume.
- Administrative region IDs.
- Address.
- Reviews and review distribution.
- Hours for some experiences.
- Popular times for some experiences.
- Images for some experiences.
- Amenities for a small subset.

The current database does not yet have reliable recommendation data for:

- Availability slots.
- Capacity and remaining capacity.
- Bookings.
- Provider-only pricing.
- Category diversity across all five categories.

The implementation must therefore treat availability, booking capacity, price, opening hours, popular times, amenities, and descriptions as optional signals. Missing optional data should not crash scoring and should not unfairly penalize an otherwise good experience.

The system must support all planned categories even if the current database is mostly or entirely Historical:

- Historical
- Nature
- Shopping
- Nightlife
- Dining

## Implementation Plan

### Phase 1: Route-Ready Deterministic Recommendations

Add a recommendation service inside the Experiences module, following the current service/controller style:

- `ExperienceRecommendationService`
- DTOs in `Modules/Experiences/Application/Dtos`
- Optional Groq client abstraction for classification/explanation.
- Endpoints on `ExperiencesController`.

Suggested endpoints:

```http
POST /api/experiences/recommendations
POST /api/experiences/recommendations/natural-language
```

The structured endpoint should accept already-normalized preferences. The natural-language endpoint should use Groq to classify user text into the same internal preference model.

### Phase 2: Natural-Language Understanding

Use Groq to classify text such as:

```text
عايز أماكن تاريخية ومطاعم قريبة مني بكرة بعد المغرب ومش زحمة
```

Into structured preferences:

```json
{
  "categories": [
    { "category": "Historical", "weight": 0.6 },
    { "category": "Dining", "weight": 0.4 }
  ],
  "visitDate": "2026-07-02",
  "preferredStartLocal": "18:00",
  "preferredEndLocal": "22:00",
  "crowdPreference": "Quiet",
  "language": "ar"
}
```

Unsupported or unavailable categories should be repaired or ignored after Groq classification. The backend should validate categories against the supported enum list and normalize weights internally.

### Phase 3: AI Explanation

After backend scoring and ranking, send Groq a compact payload for explanation only:

- User preferences.
- Experience name.
- Category.
- Region display name.
- Rating/reviews.
- Distance.
- Opening/crowd/availability summary when available.
- Score breakdown.

Groq must not reorder experiences, invent facts, or decide score values.

If Groq fails, times out, returns invalid JSON, or returns payload-size errors, return deterministic local fallback explanations.

### Phase 4: Personalization and Itinerary Enhancement

After the base recommendation feature is stable:

- Use traveler profile interests.
- Prefer not-yet-booked/not-yet-visited experiences.
- Add diversity reranking so an itinerary candidate pool is not all one category.
- Add time-of-day preferences by category.
- Integrate with hotel recommendation output as a start point.
- Return larger candidate pools for Routing, such as 20-40 items, while keeping normal UI limits smaller.

## Request Model

Suggested structured request:

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
  "visitDate": "2026-07-02",
  "preferredStartLocal": "10:00",
  "preferredEndLocal": "22:00",
  "guestsCount": 2,
  "crowdPreference": "Balanced",
  "bookableOnly": false,
  "forItinerary": true,
  "limit": 30,
  "preferredLanguage": "en"
}
```

### Request Fields

`categories`
: Optional list of supported categories with optional weights. If missing, all available categories can be considered.

`latitude` and `longitude`
: Optional user/hotel/start point. When present, distance scoring becomes active.

`adm0Gid`, `adm1Gid`, `adm2Gid`, `adm3Gid`
: Optional administrative filters.

`visitDate`, `preferredStartLocal`, `preferredEndLocal`
: Optional routing/time context. Used for opening-hours and popular-times scoring when data exists.

`guestsCount`
: Optional. Used only when availability/capacity data exists.

`crowdPreference`
: Optional values: `Quiet`, `Balanced`, `Lively`.

`bookableOnly`
: Should only be strict when availability data exists. For current third-party data, this should usually be `false`.

`forItinerary`
: When true, return route-ready metadata and a larger candidate pool.

## Scoring

The base score should be 0-100. Suggested default weights:

```text
CategoryMatchScore          0.20
DistanceScore               0.20
QualityScore                0.20
TimingOpenScore             0.15
CrowdPreferenceScore        0.10
AvailabilityScore           0.10
ContentCompletenessScore    0.05
```

If a factor is unavailable or not requested, exclude it and redistribute weights across available factors.

### Category Match

If the user selected categories, exact category matches score according to normalized category weights. If no category preference was provided, do not penalize experiences.

The implementation must support:

- Historical
- Nature
- Shopping
- Nightlife
- Dining

Current data may only contain Historical, but the algorithm should not be hardcoded to one category.

### Distance

Use straight-line Haversine distance in kilometers. Do not implement real routing here.

Suggested formula:

```text
DistanceScore = max(0, 1 - (DistanceKm / MaxUsefulDistanceKm))
```

Default `MaxUsefulDistanceKm` can be 20 km for city exploration and configurable later.

### Quality

Use rating and review volume:

```text
RawQualityScore = Rating * log(Reviews + 1)
```

Normalize across the candidate set:

```text
NormalizedQualityScore =
(RawQualityScore - MinRawQualityScore) /
(MaxRawQualityScore - MinRawQualityScore)
```

If max equals min, use neutral score `0.5`.

### Timing and Opening Hours

If `visitDate` and preferred time window are provided and the experience has `ExperienceHour` rows:

- Score high when open during the preferred window.
- Score medium when hours exist but only partly overlap.
- Score low when closed.

If hours are missing, use neutral score and set:

```json
"openHoursDataAvailable": false
```

### Crowd Preference

Use `ExperiencePopularTime` when available:

- `Quiet`: prefer low popularity.
- `Balanced`: prefer medium popularity.
- `Lively`: prefer high popularity.

If popular time data is missing, use neutral score and set:

```json
"popularTimesDataAvailable": false
```

### Availability

Availability and capacity should be optional because current third-party experiences may not have slots.

When availability tables/data exist:

- Score high when there is an active future slot in the preferred time window.
- Score high when remaining capacity is enough for `guestsCount`.
- Return `nextAvailableSlot`.

When no availability data exists:

```json
{
  "availabilityDataAvailable": false,
  "nextAvailableSlot": null
}
```

Do not penalize third-party experiences only because they are not provider-bookable, unless the request explicitly uses `bookableOnly: true`.

### Content Completeness

Small supporting signal based on:

- Has images.
- Has address.
- Has reviews.
- Has description.
- Has amenities.

This should never dominate ranking.

## Route-Ready Response

The response must be suitable for Routing module consumption without relying on AI text.

Suggested item shape:

```json
{
  "ranking": 1,
  "experienceId": 12,
  "name": "Example Museum",
  "category": "Historical",
  "latitude": 30.0444,
  "longitude": 31.2357,
  "adm0Gid": 1,
  "adm1Gid": 6,
  "adm2Gid": 42,
  "adm3Gid": 581,
  "regionDisplayName": "Downtown Cairo, Cairo",
  "distanceKm": 2.14,
  "estimatedDurationMinutes": 90,
  "durationSource": "DefaultEstimate",
  "recommendedVisitWindow": {
    "startLocal": "10:00",
    "endLocal": "12:00"
  },
  "openingWindow": {
    "opensAt": "09:00",
    "closesAt": "17:00"
  },
  "openHoursDataAvailable": true,
  "popularTimesDataAvailable": false,
  "availabilityDataAvailable": false,
  "nextAvailableSlot": null,
  "routingHints": {
    "mustVisitAtFixedTime": false,
    "requiresBooking": false,
    "clusterKey": "adm3:581",
    "timeOfDayPreference": "MorningOrAfternoon"
  },
  "finalScore": 88.4,
  "scores": {
    "categoryMatchScore": 1,
    "distanceScore": 0.89,
    "qualityScore": 0.76,
    "timingOpenScore": 1,
    "crowdPreferenceScore": null,
    "availabilityScore": null,
    "contentCompletenessScore": 0.6
  },
  "explanation": {
    "isAiGenerated": true,
    "shortExplanation": "...",
    "reasons": ["...", "..."],
    "bestFor": ["Historical", "Near your start point"]
  }
}
```

## Routing Module Contract

The Routing module should use deterministic fields only:

- `experienceId`
- `latitude`
- `longitude`
- `category`
- `finalScore`
- `distanceKm`
- `estimatedDurationMinutes`
- `recommendedVisitWindow`
- `openingWindow`
- `nextAvailableSlot`
- `routingHints`
- data-availability flags

The Routing module should not parse or depend on:

- `shortExplanation`
- `reasons`
- `bestFor`

AI explanations are display text, not routing logic.

## Diversity for Itineraries

When `forItinerary` is true, recommendations should return a diverse candidate pool rather than only the highest raw scores.

Suggested approach:

1. Score all candidates.
2. Keep a larger top pool, for example top 100.
3. Apply light diversity reranking:
   - Avoid too many same-category experiences in the first results.
   - Prefer geographic clusters that routing can combine efficiently.
   - Keep high-score anchors even if category repeats.
4. Return 20-40 candidates to Routing.

This allows the Routing module to build better day plans without losing high-quality options.

## Category Time-of-Day Hints

These are routing hints, not hard rules:

- Historical: morning or afternoon.
- Nature: morning, afternoon, or golden-hour windows.
- Shopping: afternoon or evening.
- Nightlife: evening or night.
- Dining: lunch or dinner windows.

When the database contains category-specific hours and popular times, those should override generic hints.

## Groq Token Control

For natural-language classification, send only user text and the allowed schema.

For explanations, send compact ranked data only:

- Top few recommendations.
- Short names/categories/regions.
- Score breakdown.
- Distance and timing summary.
- Availability summary only if present.

Do not send full reviews, long descriptions, large image lists, or full routing candidate pools.

If Groq fails, return local fallback explanations.

## Suggested Implementation Order

1. Verify/apply Experience migrations so current code and database schema agree.
2. Add recommendation DTOs and options.
3. Add deterministic `ExperienceRecommendationService`.
4. Add structured recommendation endpoint.
5. Add route-ready response metadata and data-availability flags.
6. Add natural-language classification with Groq.
7. Add compact Groq explanations.
8. Add focused integration tests using existing third-party Historical data patterns.
9. Add provider/availability tests once provider data exists.
10. Add diversity reranking for itinerary mode.

## MVP Rules

- Never assume availability/capacity exists.
- Never assume all five categories currently have data.
- Never penalize missing optional data too heavily.
- Always return data-availability flags.
- Keep ranking deterministic.
- Keep Groq out of routing and scoring.
- Return route-ready fields even when some values are defaults.
