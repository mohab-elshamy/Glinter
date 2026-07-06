export interface PriceIndexFilters {
  adm0Gid?: number | null;
  adm1Gid?: number | null;
  adm2Gid?: number | null;
  adm3Gid?: number | null;
}

export interface PriceIndexSegment {
  segment: "stays" | "experiences" | "combined";
  sampleSize: number;
  indexValue?: number | null;
  minimumPrice?: number | null;
  maximumPrice?: number | null;
  averagePrice?: number | null;
  medianPrice?: number | null;
  percentile25Price?: number | null;
  percentile75Price?: number | null;
}

export interface PriceIndexResponse {
  currency: string;
  filters: PriceIndexFilters;
  stays: PriceIndexSegment;
  experiences: PriceIndexSegment;
  combined: PriceIndexSegment;
}
