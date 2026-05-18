namespace Glinter.Modules.LocationCatalog.Application.Safety.Dtos;

public sealed record DistrictSafetyScoreResponse(
    Guid DistrictId,
    string DistrictNameEn,
    string? DistrictNameAr,
    double SafetyScore,
    string SafetyLevel,
    int TotalSignals,
    int RelevantSignals,
    string Explanation
);