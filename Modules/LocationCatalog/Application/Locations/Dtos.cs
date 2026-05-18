namespace Glinter.Modules.LocationCatalog.Application.Locations;

public sealed record CountryResponse(
    Guid Id,
    string Pcode,
    string NameEn,
    string? NameAr
);

public sealed record GovernorateResponse(
    Guid Id,
    Guid CountryId,
    string Pcode,
    string NameEn,
    string? NameAr
);

public sealed record DistrictResponse(
    Guid Id,
    Guid GovernorateId,
    string Pcode,
    string NameEn,
    string? NameAr
);

public sealed record AreaResponse(
    Guid Id,
    Guid DistrictId,
    string Pcode,
    string NameEn,
    string? NameAr,
    double? Latitude,
    double? Longitude,
    bool IsActive
);

public sealed record AreaDetailsResponse(
    Guid AreaId,
    string AreaNameEn,
    string? AreaNameAr,
    double? Latitude,
    double? Longitude,
    Guid DistrictId,
    string DistrictNameEn,
    Guid GovernorateId,
    string GovernorateNameEn,
    Guid CountryId,
    string CountryNameEn
);
public sealed record AreaSearchResponse(
    Guid AreaId,
    string AreaNameEn,
    string? AreaNameAr,
    string DisplayName,
    Guid DistrictId,
    string DistrictNameEn,
    string? DistrictNameAr,
    Guid GovernorateId,
    string GovernorateNameEn,
    string? GovernorateNameAr,
    Guid CountryId,
    string CountryNameEn,
    string? CountryNameAr,
    double? Latitude,
    double? Longitude
);

public sealed record DistrictSearchResponse(
    Guid DistrictId,
    string DistrictNameEn,
    string? DistrictNameAr,
    string DisplayName,
    Guid GovernorateId,
    string GovernorateNameEn,
    string? GovernorateNameAr,
    Guid CountryId,
    string CountryNameEn,
    string? CountryNameAr
);
public sealed record DistrictIndexResponse(
    Guid DistrictId,
    string DistrictNameEn,
    string? DistrictNameAr,
    string DisplayName,
    Guid GovernorateId,
    string GovernorateNameEn,
    string? GovernorateNameAr,
    Guid CountryId,
    string CountryNameEn,
    string? CountryNameAr,
    double? Latitude,
    double? Longitude,
    double SafetyScore,
    string SafetyLevel,
    string? SafetyExplanation,
    double? PriceScore,
    string? PriceLevel,
    string? PriceExplanation,
    double? ServicesScore,
    string? ServicesLevel,
    string? ServicesExplanation,
    DateTime ComputedAtUtc
);