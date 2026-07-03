# Hotel Recommendations

The hotel recommendation API ranks stays with deterministic backend scoring. Groq is used only to classify natural-language input and generate concise explanations after ranking. Groq does not decide scores, ranking, or hotel facts.

## Endpoints

### Structured request

`POST /api/stays/recommendations`

```json
{
  "budgetLevel": 3,
  "experienceCategories": [
    { "category": "Historical", "weight": 70 },
    { "category": "Dining", "weight": 30 }
  ],
  "requestedAmenities": ["WiFi", "Gym"],
  "adm1Gid": 11,
  "limit": 10,
  "preferredLanguage": "en"
}
```

### Natural-language request

`POST /api/stays/recommendations/natural-language`

```json
{
  "text": "عايز فندق متوسط قريب من الأماكن التاريخية والمطاعم، ويكون فيه جيم وواي فاي",
  "limit": 10
}
```

Groq converts the text into structured preferences. If Groq is unavailable or returns invalid JSON, the backend uses a small deterministic local classifier and still returns recommendations.

## Scoring

Budget levels are based on stay prices:

1. Budget
2. Economy
3. MidRange
4. Upscale
5. Luxury

Price quintiles are calculated inside the filtered hotel set when enough priced hotels exist. Otherwise the service falls back to the broader filtered/global price distribution. Null or zero prices are handled safely and receive a neutral budget match when a user budget is provided.

Supported experience categories:

- Historical
- Nature
- Shopping
- Nightlife
- Dining

For each selected category, the backend reads existing records from the Experiences module, calculates straight-line Haversine distance from each candidate hotel, averages the nearest configured experiences, and converts that distance into a 0-1 score. The default useful distance is 20 km.

Hotel quality uses:

```text
Rating * log(Reviews + 1)
```

The raw quality score is normalized across the candidate hotel set.

Supported canonical amenity tags include:

- WiFi
- Gym
- Pool
- Spa
- Restaurant
- Bar
- Parking

Amenity names from the existing `stays.stay_amenities` data are normalized from English and Arabic names where possible.

The default weighted score is:

```text
0.40 * interest proximity
+ 0.25 * budget match
+ 0.25 * hotel quality
+ 0.10 * amenity match
```

Missing user preferences are excluded and remaining weights are redistributed, so hotels are not penalized for preferences the user did not provide.

## Response Shape

```json
{
  "preferences": {
    "budgetLevel": 3,
    "budgetLabel": "MidRange",
    "experienceCategories": [
      { "category": "Historical", "weight": 0.7 },
      { "category": "Dining", "weight": 0.3 }
    ],
    "requestedAmenities": ["Gym", "WiFi"],
    "limit": 10,
    "preferredLanguage": "en"
  },
  "totalCandidates": 42,
  "returnedCount": 10,
  "items": [
    {
      "ranking": 1,
      "hotelId": 123,
      "name": "Example Hotel",
      "price": 1500,
      "budgetLevel": 3,
      "budgetLabel": "MidRange",
      "rating": 4.5,
      "reviews": 240,
      "finalScore": 87.25,
      "scores": {
        "interestProximityScore": 0.91,
        "budgetMatchScore": 1,
        "hotelQualityScore": 0.78,
        "amenityMatchScore": 1
      },
      "matchedAmenities": ["Gym", "WiFi"],
      "nearbyExperiences": [
        {
          "experienceId": 88,
          "name": "Example Museum",
          "category": "Historical",
          "distanceKm": 1.42
        }
      ],
      "explanation": {
        "shortExplanation": "Example Hotel matches your mid-range historical and dining preferences.",
        "reasons": ["Strong budget fit.", "Close to relevant experiences."],
        "bestFor": ["MidRange", "Historical", "Dining"],
        "isAiGenerated": true
      }
    }
  ]
}
```

## Configuration

Configure Groq through `HotelRecommendations` in appsettings or environment variables:

- `GROQ_API_KEY`
- `GROQ_MODEL`
- `GROQ_BASE_URL`

Never hardcode the API key. If Groq configuration is missing, the recommendation request still succeeds with local fallback classification/explanations.

The explanation payload is compact to control token usage. By default, Groq receives only the top `GroqMaxExplanationItems` ranked hotels, shortened description/location text, a small amenity list, and the nearest few experiences. If Groq returns `413 Payload Too Large`, the service retries once with `GroqRetryMaxExplanationItems` and an even smaller payload before using local fallback explanations.

Some Groq models reject OpenAI JSON mode (`response_format: json_object`) with `400 Bad Request`. When `GroqRetryWithoutResponseFormatOnBadRequest` is enabled, the service retries the same compact request without `response_format` and still parses JSON from the model text. Groq error bodies are logged in truncated form using `GroqErrorBodyLogCharacters`.
