export interface ComfortIndexFilters {
  adm0Gid?: number | null;
  adm1Gid?: number | null;
  adm2Gid?: number | null;
  adm3Gid?: number | null;
}

export interface ComfortIndexSegment {
  segment: "stays" | "experiences" | "combined";
  sampleSize: number;
  indexValue?: number | null;
  minimumScore?: number | null;
  maximumScore?: number | null;
  averageScore?: number | null;
  medianScore?: number | null;
  percentile25Score?: number | null;
  percentile75Score?: number | null;
}

export interface ComfortIndexResponse {
  filters: ComfortIndexFilters;
  stays: ComfortIndexSegment;
  experiences: ComfortIndexSegment;
  combined: ComfortIndexSegment;
}
