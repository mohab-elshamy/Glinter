# Price Index

The Price Index module exposes read-only pricing analytics from existing Stays
and Experiences data. It does not store derived prices and does not require a
separate migration.

The module reads:

- Hotel/stay prices from `stays.stays.Price`.
- Experience prices from `experiences.experiences.PriceRangeMin` and
  `experiences.experiences.PriceRangeMax`.

## Endpoints

```http
GET /api/price-index
GET /api/price-index?adm0Gid=1&adm1Gid=2&adm2Gid=3&adm3Gid=4
GET /api/price-index/adm0/{adm0Gid}
GET /api/price-index/adm1/{adm1Gid}
GET /api/price-index/adm2/{adm2Gid}
GET /api/price-index/adm3/{adm3Gid}
POST /api/price-index/batch
```

All filters are optional on the root endpoint. Route-specific endpoints apply
one administrative filter.

The map heatmap should use the batch endpoint for child regions:

```json
{
  "filters": [
    { "adm1Gid": 11 },
    { "adm1Gid": 12 }
  ]
}
```

It returns an array of `PriceIndexResponse` objects in the same order.

## Response Shape

```json
{
  "currency": "USD",
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
    "minimumPrice": 25,
    "maximumPrice": 450,
    "averagePrice": 135.5,
    "medianPrice": 120,
    "percentile25Price": 75,
    "percentile75Price": 180
  },
  "experiences": {
    "segment": "experiences",
    "sampleSize": 64,
    "indexValue": 81.42,
    "minimumPrice": 0,
    "maximumPrice": 220,
    "averagePrice": 48.75,
    "medianPrice": 35,
    "percentile25Price": 15,
    "percentile75Price": 65
  },
  "combined": {
    "segment": "combined",
    "sampleSize": 106,
    "indexValue": 89.1,
    "minimumPrice": 0,
    "maximumPrice": 450,
    "averagePrice": 83.08,
    "medianPrice": 60,
    "percentile25Price": 25,
    "percentile75Price": 125
  }
}
```

The response always contains three segments:

- `stays`: priced active stays.
- `experiences`: priced active approved experiences.
- `combined`: the union of both price sets.

If a filtered segment has no priced records, its `sampleSize` is `0` and all
price/index fields are `null`.

## Index Meaning

`indexValue` compares the filtered average price against the global average for
the same segment:

```text
indexValue = (filteredAveragePrice / globalAveragePrice) * 100
```

Interpretation:

- `100`: the filtered area is at the platform average.
- Below `100`: the filtered area is cheaper than the platform average.
- Above `100`: the filtered area is more expensive than the platform average.

For example, `indexValue = 125` means the filtered area is about 25% more
expensive than the platform-wide average for that segment.

## Price Normalization

### Stays

Stay prices are used directly from `stays.stays.Price`.

Only records with:

- `IsActive = true`
- `Price IS NOT NULL`
- `Price >= 0`

are included.

### Experiences

Experience price ranges are normalized to one representative price:

```text
normalizedExperiencePrice = (min(PriceRangeMin, PriceRangeMax) + max(PriceRangeMin, PriceRangeMax)) / 2
```

If only one side of the range exists, that value is used for both min and max.
For example:

- `PriceRangeMin = 20`, `PriceRangeMax = 40` becomes `30`.
- `PriceRangeMin = 50`, `PriceRangeMax = null` becomes `50`.
- `PriceRangeMin = null`, `PriceRangeMax = 80` becomes `80`.

Only records with:

- `IsActive = true`
- `ModerationStatus = Approved`
- at least one of `PriceRangeMin` or `PriceRangeMax`
- normalized price `>= 0`

are included.

## Percentiles

The module returns:

- `minimumPrice`
- `maximumPrice`
- `averagePrice`
- `medianPrice`
- `percentile25Price`
- `percentile75Price`

Percentiles are calculated from sorted prices using linear interpolation between
neighboring values. All monetary values are rounded to two decimal places.

## Frontend Client

The frontend helper is available at:

```ts
import { priceIndexApi } from "@/shared/services/api-price-index";
```

Examples:

```ts
await priceIndexApi.get();
await priceIndexApi.get({ adm1Gid: 11 });
await priceIndexApi.getByDistrict(42);
```

The matching TypeScript response types are defined in:

```ts
import type { PriceIndexResponse } from "@/shared/types/price-index";
```

## Operational Notes

- The module is read-only and has no background worker.
- It uses the existing Stays and Experiences DbContexts.
- No external AI provider is used.
- No extra configuration is required.
