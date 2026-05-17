using Glinter.Modules.LocationCatalog.Application.Locations;
using Microsoft.AspNetCore.Mvc;

namespace Glinter.Modules.LocationCatalog.Presentation.Controllers;

[ApiController]
[Route("api/location-catalog")]
public sealed class LocationCatalogController : ControllerBase
{
    private readonly GetCountriesQueryHandler _getCountriesQueryHandler;
    private readonly GetGovernoratesByCountryQueryHandler _getGovernoratesByCountryQueryHandler;
    private readonly GetDistrictsByGovernorateQueryHandler _getDistrictsByGovernorateQueryHandler;
    private readonly GetAreasByDistrictQueryHandler _getAreasByDistrictQueryHandler;
    private readonly SearchAreasQueryHandler _searchAreasQueryHandler;
    private readonly GetAreaByIdQueryHandler _getAreaByIdQueryHandler;

    public LocationCatalogController(
        GetCountriesQueryHandler getCountriesQueryHandler,
        GetGovernoratesByCountryQueryHandler getGovernoratesByCountryQueryHandler,
        GetDistrictsByGovernorateQueryHandler getDistrictsByGovernorateQueryHandler,
        GetAreasByDistrictQueryHandler getAreasByDistrictQueryHandler,
        SearchAreasQueryHandler searchAreasQueryHandler,
        GetAreaByIdQueryHandler getAreaByIdQueryHandler)
    {
        _getCountriesQueryHandler = getCountriesQueryHandler;
        _getGovernoratesByCountryQueryHandler = getGovernoratesByCountryQueryHandler;
        _getDistrictsByGovernorateQueryHandler = getDistrictsByGovernorateQueryHandler;
        _getAreasByDistrictQueryHandler = getAreasByDistrictQueryHandler;
        _searchAreasQueryHandler = searchAreasQueryHandler;
        _getAreaByIdQueryHandler = getAreaByIdQueryHandler;
    }

    [HttpGet("countries")]
    public async Task<IActionResult> GetCountries(CancellationToken cancellationToken)
    {
        var result = await _getCountriesQueryHandler.HandleAsync(cancellationToken);

        return Ok(result);
    }

    [HttpGet("countries/{countryId:guid}/governorates")]
    public async Task<IActionResult> GetGovernoratesByCountry(
        Guid countryId,
        CancellationToken cancellationToken)
    {
        var result = await _getGovernoratesByCountryQueryHandler.HandleAsync(countryId, cancellationToken);

        return Ok(result);
    }

    [HttpGet("governorates/{governorateId:guid}/districts")]
    public async Task<IActionResult> GetDistrictsByGovernorate(
        Guid governorateId,
        CancellationToken cancellationToken)
    {
        var result = await _getDistrictsByGovernorateQueryHandler.HandleAsync(governorateId, cancellationToken);

        return Ok(result);
    }

    [HttpGet("districts/{districtId:guid}/areas")]
    public async Task<IActionResult> GetAreasByDistrict(
        Guid districtId,
        CancellationToken cancellationToken)
    {
        var result = await _getAreasByDistrictQueryHandler.HandleAsync(districtId, cancellationToken);

        return Ok(result);
    }

    [HttpGet("areas/search")]
    public async Task<IActionResult> SearchAreas(
        [FromQuery] string? query,
        CancellationToken cancellationToken)
    {
        var result = await _searchAreasQueryHandler.HandleAsync(query, cancellationToken);

        return Ok(result);
    }

    [HttpGet("areas/{areaId:guid}")]
    public async Task<IActionResult> GetAreaById(
        Guid areaId,
        CancellationToken cancellationToken)
    {
        var result = await _getAreaByIdQueryHandler.HandleAsync(areaId, cancellationToken);

        if (result is null)
        {
            return NotFound("Area was not found.");
        }

        return Ok(result);
    }
}