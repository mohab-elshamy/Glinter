namespace Glinter.Modules.PriceIndex.Application.Dtos;

public sealed record PriceIndexRequest(
    int? Adm0Gid = null,
    int? Adm1Gid = null,
    int? Adm2Gid = null,
    int? Adm3Gid = null);

public sealed record PriceIndexBatchRequest(
    IReadOnlyList<PriceIndexRequest> Filters);

public sealed record PriceIndexResponseDto(
    string Currency,
    PriceIndexFilterDto Filters,
    PriceIndexSegmentDto Stays,
    PriceIndexSegmentDto Experiences,
    PriceIndexSegmentDto Combined);

public sealed record PriceIndexFilterDto(
    int? Adm0Gid,
    int? Adm1Gid,
    int? Adm2Gid,
    int? Adm3Gid);

public sealed record PriceIndexSegmentDto(
    string Segment,
    int SampleSize,
    decimal? IndexValue,
    decimal? MinimumPrice,
    decimal? MaximumPrice,
    decimal? AveragePrice,
    decimal? MedianPrice,
    decimal? Percentile25Price,
    decimal? Percentile75Price);
