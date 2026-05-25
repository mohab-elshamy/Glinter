namespace Glinter.Modules.LocationCatalog.Application.Safety.Dtos;

public sealed record ImportDistrictNewsRequest(
    string Query,
    int MaxArticles = 5
);