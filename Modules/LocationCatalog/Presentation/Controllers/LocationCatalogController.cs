using Glinter.Modules.LocationCatalog.Application.Locations;
using Microsoft.AspNetCore.Mvc;
using Glinter.Modules.LocationCatalog.Application.Safety.Dtos;
using Glinter.Modules.LocationCatalog.Application.Safety.Services;

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
    private readonly SearchDistrictsQueryHandler _searchDistrictsQueryHandler;
    private readonly DistrictSafetySignalService _districtSafetySignalService;
    private readonly DistrictSafetyScoreService _districtSafetyScoreService;
    private readonly GetDistrictIndexByDistrictQueryHandler _getDistrictIndexByDistrictQueryHandler;
    private readonly GetGovernorateDistrictIndicesQueryHandler _getGovernorateDistrictIndicesQueryHandler;

    public LocationCatalogController(
        GetCountriesQueryHandler getCountriesQueryHandler,
        GetGovernoratesByCountryQueryHandler getGovernoratesByCountryQueryHandler,
        GetDistrictsByGovernorateQueryHandler getDistrictsByGovernorateQueryHandler,
        GetAreasByDistrictQueryHandler getAreasByDistrictQueryHandler,
        SearchAreasQueryHandler searchAreasQueryHandler,
        GetAreaByIdQueryHandler getAreaByIdQueryHandler,
        SearchDistrictsQueryHandler searchDistrictsQueryHandler,
        DistrictSafetySignalService districtSafetySignalService,
        DistrictSafetyScoreService districtSafetyScoreService,
        GetDistrictIndexByDistrictQueryHandler getDistrictIndexByDistrictQueryHandler,
        GetGovernorateDistrictIndicesQueryHandler getGovernorateDistrictIndicesQueryHandler
        )
    {
        _getCountriesQueryHandler = getCountriesQueryHandler;
        _getGovernoratesByCountryQueryHandler = getGovernoratesByCountryQueryHandler;
        _getDistrictsByGovernorateQueryHandler = getDistrictsByGovernorateQueryHandler;
        _getAreasByDistrictQueryHandler = getAreasByDistrictQueryHandler;
        _searchAreasQueryHandler = searchAreasQueryHandler;
        _getAreaByIdQueryHandler = getAreaByIdQueryHandler;
        _searchDistrictsQueryHandler = searchDistrictsQueryHandler;
        _districtSafetySignalService = districtSafetySignalService;
        _districtSafetyScoreService = districtSafetyScoreService;
        _getDistrictIndexByDistrictQueryHandler = getDistrictIndexByDistrictQueryHandler;
        _getGovernorateDistrictIndicesQueryHandler = getGovernorateDistrictIndicesQueryHandler;
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
    [HttpGet("districts/search")]
    public async Task<IActionResult> SearchDistricts(
        [FromQuery] string? query,
        CancellationToken cancellationToken)
    {
        var result = await _searchDistrictsQueryHandler.HandleAsync(query, cancellationToken);

        return Ok(result);
    }
    
    [HttpPost("districts/{districtId:guid}/safety-signals/analyze")]
    public async Task<IActionResult> AnalyzeDistrictSafetySignal(
        Guid districtId,
        [FromBody] AnalyzeDistrictSafetySignalRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _districtSafetySignalService.AnalyzeAndSaveAsync(
                districtId,
                request,
                cancellationToken);

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
    [HttpGet("districts/{districtId:guid}/safety-score")]
    public async Task<IActionResult> GetDistrictSafetyScore(
        Guid districtId,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _districtSafetyScoreService.CalculateAsync(
                districtId,
                cancellationToken);

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
    [HttpPost("districts/{districtId:guid}/safety-score/recalculate")]
    public async Task<IActionResult> RecalculateDistrictSafetyScore(
        Guid districtId,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _districtSafetyScoreService.CalculateAndSaveAsync(
                districtId,
                cancellationToken);

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
    [HttpGet("districts/{districtId:guid}/indices")]
    public async Task<IActionResult> GetDistrictIndices(
        Guid districtId,
        CancellationToken cancellationToken)
    {
        var result = await _getDistrictIndexByDistrictQueryHandler.HandleAsync(
            districtId,
            cancellationToken);

        if (result is null)
        {
            return NotFound(new { message = "District indices were not found. Recalculate the district score first." });
        }

        return Ok(result);
    }

    [HttpGet("governorates/{governorateId:guid}/district-indices")]
    public async Task<IActionResult> GetGovernorateDistrictIndices(
        Guid governorateId,
        CancellationToken cancellationToken)
    {
        var result = await _getGovernorateDistrictIndicesQueryHandler.HandleAsync(
            governorateId,
            cancellationToken);

        return Ok(result);
    }
}