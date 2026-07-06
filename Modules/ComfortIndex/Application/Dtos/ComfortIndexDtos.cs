namespace Glinter.Modules.ComfortIndex.Application.Dtos;

public sealed record ComfortIndexRequest(
    int? Adm0Gid = null,
    int? Adm1Gid = null,
    int? Adm2Gid = null,
    int? Adm3Gid = null);

public sealed record ComfortIndexBatchRequest(
    IReadOnlyList<ComfortIndexRequest> Filters);

public sealed record ComfortIndexResponseDto(
    ComfortIndexFilterDto Filters,
    ComfortIndexSegmentDto Stays,
    ComfortIndexSegmentDto Experiences,
    ComfortIndexSegmentDto Combined);

public sealed record ComfortIndexFilterDto(
    int? Adm0Gid,
    int? Adm1Gid,
    int? Adm2Gid,
    int? Adm3Gid);

public sealed record ComfortIndexSegmentDto(
    string Segment,
    int SampleSize,
    decimal? IndexValue,
    decimal? MinimumScore,
    decimal? MaximumScore,
    decimal? AverageScore,
    decimal? MedianScore,
    decimal? Percentile25Score,
    decimal? Percentile75Score);
