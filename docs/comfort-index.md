# Comfort Index

The Comfort Index module exposes read-only comfort analytics from existing
Stays and Experiences data. It uses rating and review volume as a confidence
weighted comfort signal.

The module reads:

- Stay comfort inputs from `stays.stays.Rating` and `stays.stays.Reviews`.
- Experience comfort inputs from `experiences.experiences.Rating` and
  `experiences.experiences.Reviews`.

## Formula

```text
comfortScore = rating * log(numberOfReviews)
```

Records are included only when:

- `Rating IS NOT NULL`
- `Rating > 0`
- `Reviews IS NOT NULL`
- `Reviews > 1`

The `Reviews > 1` rule avoids invalid or zero-value logarithms.

## Endpoints

```http
GET /api/comfort-index
GET /api/comfort-index?adm0Gid=1&adm1Gid=2&adm2Gid=3&adm3Gid=4
GET /api/comfort-index/adm0/{adm0Gid}
GET /api/comfort-index/adm1/{adm1Gid}
GET /api/comfort-index/adm2/{adm2Gid}
GET /api/comfort-index/adm3/{adm3Gid}
POST /api/comfort-index/batch
```

The batch endpoint accepts:

```json
{
  "filters": [
    { "adm1Gid": 11 },
    { "adm1Gid": 12 }
  ]
}
```

It returns an array of comfort index responses in the same order.

## Response Shape

```json
{
  "filters": {
    "adm0Gid": null,
    "adm1Gid": 11,
    "adm2Gid": null,
    "adm3Gid": null
  },
  "stays": {
    "segment": "stays",
    "sampleSize": 42,
    "indexValue": 96.25,
    "minimumScore": 3.22,
    "maximumScore": 28.91,
    "averageScore": 14.44,
    "medianScore": 13.8,
    "percentile25Score": 9.7,
    "percentile75Score": 18.1
  },
  "experiences": {
    "segment": "experiences",
    "sampleSize": 64,
    "indexValue": 108.42,
    "minimumScore": 2.5,
    "maximumScore": 31.12,
    "averageScore": 16.35,
    "medianScore": 15.2,
    "percentile25Score": 11.4,
    "percentile75Score": 20.9
  },
  "combined": {
    "segment": "combined",
    "sampleSize": 106,
    "indexValue": 101.1,
    "minimumScore": 2.5,
    "maximumScore": 31.12,
    "averageScore": 15.59,
    "medianScore": 14.8,
    "percentile25Score": 10.2,
    "percentile75Score": 19.3
  }
}
```

## Index Meaning

`indexValue` compares the filtered average comfort score against the global
average for the same segment:

```text
indexValue = (filteredAverageScore / globalAverageScore) * 100
```

Interpretation:

- `100`: the filtered area is at the platform average.
- Below `100`: the filtered area has a lower comfort signal.
- Above `100`: the filtered area has a higher comfort signal.

## Frontend Heatmap

The Where to Stay map uses `POST /api/comfort-index/batch` to load child-region
comfort values in one request, then draws region boundaries with the same color
scale used by price index heatmaps.

No external AI provider or background worker is used.
