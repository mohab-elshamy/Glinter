# Itinerary Planning

The Itineraries module builds route-ready day plans from existing experience recommendations. It does not replace the Experiences recommendation scorer; it consumes its ranked candidates and decides the order, schedule, and route legs.

AI is used only to fill the itinerary form from natural language. The backend still selects stops, orders the route, schedules time, and calls routing providers deterministically.

## Endpoints

```http
POST /api/itineraries/plan
POST /api/itineraries/plan/natural-language
```

## Structured Request

```json
{
  "start": {
    "latitude": 30.0444,
    "longitude": 31.2357,
    "label": "Hotel"
  },
  "end": null,
  "date": "2026-07-10",
  "dayStartLocal": "10:00:00",
  "dayEndLocal": "20:00:00",
  "travelMode": "PublicTransit",
  "fallbackTravelMode": "Walking",
  "pace": "Balanced",
  "categories": [
    { "category": "Historical", "weight": 70 },
    { "category": "Dining", "weight": 30 }
  ],
  "adm1Gid": null,
  "adm2Gid": null,
  "adm3Gid": null,
  "maxStops": 5,
  "candidateLimit": 30,
  "guestsCount": 2,
  "crowdPreference": "Quiet",
  "includeMealBreaks": true,
  "returnToStart": true,
  "avoidLongWalking": true,
  "preferredLanguage": "ar"
}
```

## Natural-Language Request

```json
{
  "text": "عايز يوم خفيف في القاهرة يبدأ من الفندق 10 الصبح، أماكن تاريخية ومطعم كويس، من غير مشي كتير، وارجع الفندق قبل 8",
  "start": {
    "latitude": 30.0444,
    "longitude": 31.2357,
    "label": "Hotel"
  },
  "date": "2026-07-10",
  "preferredLanguage": "ar"
}
```

Groq classifies the text into an internal `ItineraryPlanRequest`. Explicit fields sent by the client, such as `start`, `date`, time overrides, admin filters, and limits, are trusted over AI-inferred fields.

If Groq is unavailable, times out, or returns invalid JSON, the endpoint uses a local fallback classifier and still attempts to build an itinerary.

## Response

```json
{
  "date": "2026-07-10",
  "travelMode": "PublicTransit",
  "fallbackTravelMode": "Walking",
  "pace": "Balanced",
  "totalCandidateExperiences": 30,
  "selectedStopsCount": 4,
  "totalDurationMinutes": 510,
  "totalTravelMinutes": 85,
  "totalDistanceKm": 12.4,
  "score": 83.2,
  "warnings": [],
  "stops": [
    {
      "order": 0,
      "type": "Start",
      "name": "Hotel",
      "arrivalLocal": "10:00",
      "departureLocal": "10:00"
    }
  ],
  "legs": [
    {
      "fromOrder": 0,
      "toOrder": 1,
      "mode": "PublicTransit",
      "provider": "OpenTripPlanner",
      "distanceKm": 2.1,
      "durationMinutes": 18,
      "geometry": {
        "type": "LineString",
        "coordinates": [
          [31.2357, 30.0444],
          [31.2382, 30.0461]
        ]
      }
    }
  ],
  "explanation": {
    "summary": "Built an itinerary using recommendation quality, travel time, and visit duration.",
    "isAiGenerated": false
  }
}
```

For the natural-language endpoint, the response wraps the same itinerary with:

- `inputText`
- `interpretedRequest`
- `classification.isAiGenerated`
- `classification.confidence`
- `classification.notes`

## Routing Providers

`PublicTransit`
: Uses OpenTripPlanner GraphQL `planConnection` at `OpenTripPlannerBaseUrl` + `OpenTripPlannerGraphQlPath`, default `http://localhost:8040/otp/gtfs/v1`.

`Walking`, `Driving`, `Cycling`
: Uses OpenRouteService directions.

Fallback
: If a provider is unavailable, misconfigured, times out, or returns an error, the module returns a Haversine-based travel estimate with a straight-line GeoJSON geometry and a warning instead of failing the entire itinerary.

Each leg returns `geometry` as a GeoJSON `LineString` with `[longitude, latitude]` coordinates, ready for map rendering.

## Configuration

```json
{
  "ItineraryPlanning": {
    "OpenRouteServiceApiKey": "",
    "OpenRouteServiceBaseUrl": "https://api.openrouteservice.org",
    "OpenTripPlannerBaseUrl": "http://localhost:8040",
    "OpenTripPlannerGraphQlPath": "/otp/gtfs/v1",
    "RoutingRequestTimeoutSeconds": 20,
    "FallbackWalkingKmh": 4.5,
    "FallbackDrivingKmh": 25,
    "FallbackCyclingKmh": 12,
    "FallbackTransitKmh": 18,
    "DefaultMaxStops": 5,
    "MaxStops": 10,
    "DefaultCandidateLimit": 30,
    "MaxCandidateLimit": 50,
    "GroqApiKey": "",
    "GroqModel": "qwen/qwen3-32b",
    "GroqBaseUrl": "https://api.groq.com/openai/v1"
  }
}
```

Environment variables are also supported:

- `OPENROUTESERVICE_API_KEY`
- `OPENROUTESERVICE_BASE_URL`
- `OPENTRIPPLANNER_BASE_URL`
- `GROQ_API_KEY`
- `GROQ_MODEL`
- `GROQ_BASE_URL`

## Current Algorithm

1. Normalize and validate request.
2. Ask `ExperienceRecommendationService` for route-ready candidates with `forItinerary=true`.
3. Preselect a bounded candidate pool.
4. Greedily choose the next stop based on recommendation score, route duration penalty, and category diversity.
5. Schedule arrival/departure using estimated experience duration and pace.
6. Add a return/end leg.
7. Return stops, legs, warnings, and a deterministic explanation.

Future improvements can add persisted itineraries, route cache, ORS matrix calls, OTP GraphQL support, local-search route optimization, and AI-generated final itinerary summaries.
