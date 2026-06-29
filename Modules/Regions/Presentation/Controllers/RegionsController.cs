using Glinter.Modules.IdentityAccess.Domain.Constants;
using Glinter.Modules.Regions.Application.Countries.Commands;
using Glinter.Modules.Regions.Application.Countries.Queries;
using Glinter.Modules.Regions.Application.Districts.Commands;
using Glinter.Modules.Regions.Application.Districts.Queries;
using Glinter.Modules.Regions.Application.DTOs;
using Glinter.Modules.Regions.Application.Governorates.Commands;
using Glinter.Modules.Regions.Application.Governorates.Queries;
using Glinter.Modules.Regions.Application.Neighbourhoods.Commands;
using Glinter.Modules.Regions.Application.Neighbourhoods.Queries;
using Glinter.Modules.Regions.Application.PointLookup.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Glinter.Modules.Regions.Presentation.Controllers;

[ApiController]
[Route("api/regions")]
public class RegionsController(
    // Country
    GetAllCountriesHandler getAllCountries,
    GetCountryByIdHandler getCountryById,
    CreateCountryHandler createCountry,
    UpdateCountryHandler updateCountry,
    DeleteCountryHandler deleteCountry,
    // Governorate
    GetAllGovernoratesHandler getAllGovernorates,
    GetGovernorateByIdHandler getGovernorateById,
    GetGovernoratesByCountryHandler getGovernoratesByCountry,
    CreateGovernorateHandler createGovernorate,
    UpdateGovernorateHandler updateGovernorate,
    DeleteGovernorateHandler deleteGovernorate,
    // District
    GetAllDistrictsHandler getAllDistricts,
    GetDistrictByIdHandler getDistrictById,
    GetDistrictsByGovernorateHandler getDistrictsByGovernorate,
    CreateDistrictHandler createDistrict,
    UpdateDistrictHandler updateDistrict,
    DeleteDistrictHandler deleteDistrict,
    // Neighbourhood
    GetAllNeighbourhoodsHandler getAllNeighbourhoods,
    GetNeighbourhoodByIdHandler getNeighbourhoodById,
    GetNeighbourhoodsByDistrictHandler getNeighbourhoodsByDistrict,
    CreateNeighbourhoodHandler createNeighbourhood,
    UpdateNeighbourhoodHandler updateNeighbourhood,
    DeleteNeighbourhoodHandler deleteNeighbourhood,
    // Point lookup
    GetRegionsByPointHandler getRegionsByPoint
) : ControllerBase
{
    [HttpGet("by-point")]
    public async Task<IActionResult> GetByPoint([FromQuery] RegionByPointRequest request, CancellationToken ct)
    {
        var result = await getRegionsByPoint.HandleAsync(new GetRegionsByPointQuery
        {
            Lat = request.Lat,
            Lon = request.Lon,
        }, ct);

        if (result is null)
            throw new NotFoundException("No region found for point.");

        return Ok(result);
    }

    // ──────────────────────────────────────────────
    // Countries (ADM0)
    // ──────────────────────────────────────────────

    [HttpGet("countries")]
    public async Task<IActionResult> GetCountries([FromQuery] GetAllCountriesQuery query, CancellationToken ct)
        => Ok(await getAllCountries.HandleAsync(query, ct));

    [HttpGet("countries/{gid:int}")]
    public async Task<IActionResult> GetCountry(
        int gid,
        [FromQuery] RegionGeometryAccuracyRequest geometry,
        CancellationToken ct)
    {
        var result = await getCountryById.HandleAsync(new GetCountryByIdQuery
        {
            Gid = gid,
            GeometryAccuracy = geometry.GeometryAccuracy,
        }, ct);
        if (result is null)
            throw new NotFoundException("Country not found.");

        return Ok(result);
    }

    [HttpPost("countries")]
    [Authorize(Policy = PolicyNames.AdminOnly)]
    public async Task<IActionResult> CreateCountry([FromBody] CreateAdm0Request request, CancellationToken ct)
    {
        var result = await createCountry.HandleAsync(new CreateCountryCommand
        {
            NameEn = request.NameEn,
            NameAr = request.NameAr,
            Pcode = request.Pcode,
            ImageUrl = request.ImageUrl,
            FlagUrl = request.FlagUrl,
        }, ct);
        return CreatedAtAction(nameof(GetCountry), new { gid = result.Gid }, result);
    }

    [HttpPut("countries/{gid:int}")]
    [Authorize(Policy = PolicyNames.AdminOnly)]
    public async Task<IActionResult> UpdateCountry(int gid, [FromBody] UpdateAdm0Request request, CancellationToken ct)
    {
        var result = await updateCountry.HandleAsync(new UpdateCountryCommand
        {
            Gid = gid,
            NameEn = request.NameEn,
            NameAr = request.NameAr,
            ImageUrl = request.ImageUrl,
            FlagUrl = request.FlagUrl,
        }, ct);
        if (result is null)
            throw new NotFoundException("Country not found.");

        return Ok(result);
    }

    [HttpDelete("countries/{gid:int}")]
    [Authorize(Policy = PolicyNames.AdminOnly)]
    public async Task<IActionResult> DeleteCountry(int gid, CancellationToken ct)
    {
        var deleted = await deleteCountry.HandleAsync(gid, ct);
        if (!deleted)
            throw new NotFoundException("Country not found.");

        return NoContent();
    }

    // ──────────────────────────────────────────────
    // Governorates (ADM1)
    // ──────────────────────────────────────────────

    [HttpGet("governorates")]
    public async Task<IActionResult> GetGovernorates([FromQuery] GetAllGovernoratesQuery query, CancellationToken ct)
        => Ok(await getAllGovernorates.HandleAsync(query, ct));

    [HttpGet("governorates/{gid:int}")]
    public async Task<IActionResult> GetGovernorate(
        int gid,
        [FromQuery] RegionGeometryAccuracyRequest geometry,
        CancellationToken ct)
    {
        var result = await getGovernorateById.HandleAsync(new GetGovernorateByIdQuery
        {
            Gid = gid,
            GeometryAccuracy = geometry.GeometryAccuracy,
        }, ct);
        if (result is null)
            throw new NotFoundException("Governorate not found.");

        return Ok(result);
    }

    [HttpGet("countries/{adm0Gid:int}/governorates")]
    public async Task<IActionResult> GetGovernoratesByCountry(
        int adm0Gid,
        [FromQuery] GetGovernoratesByCountryQuery query,
        CancellationToken ct)
    {
        query.Adm0Gid = adm0Gid;
        return Ok(await getGovernoratesByCountry.HandleAsync(query, ct));
    }

    [HttpPost("governorates")]
    [Authorize(Policy = PolicyNames.AdminOnly)]
    public async Task<IActionResult> CreateGovernorate([FromBody] CreateAdm1Request request, CancellationToken ct)
    {
        var result = await createGovernorate.HandleAsync(new CreateGovernorateCommand
        {
            Adm0Gid = request.Adm0Gid,
            NameEn = request.NameEn,
            NameAr = request.NameAr,
            Pcode = request.Pcode,
            ImageUrl = request.ImageUrl,
        }, ct);
        return CreatedAtAction(nameof(GetGovernorate), new { gid = result.Gid }, result);
    }

    [HttpPut("governorates/{gid:int}")]
    [Authorize(Policy = PolicyNames.AdminOnly)]
    public async Task<IActionResult> UpdateGovernorate(int gid, [FromBody] UpdateAdm1Request request, CancellationToken ct)
    {
        var result = await updateGovernorate.HandleAsync(new UpdateGovernorateCommand
        {
            Gid = gid,
            NameEn = request.NameEn,
            NameAr = request.NameAr,
            ImageUrl = request.ImageUrl,
        }, ct);
        if (result is null)
            throw new NotFoundException("Governorate not found.");

        return Ok(result);
    }

    [HttpDelete("governorates/{gid:int}")]
    [Authorize(Policy = PolicyNames.AdminOnly)]
    public async Task<IActionResult> DeleteGovernorate(int gid, CancellationToken ct)
    {
        var deleted = await deleteGovernorate.HandleAsync(gid, ct);
        if (!deleted)
            throw new NotFoundException("Governorate not found.");

        return NoContent();
    }

    // ──────────────────────────────────────────────
    // Districts (ADM2)
    // ──────────────────────────────────────────────

    [HttpGet("districts")]
    public async Task<IActionResult> GetDistricts([FromQuery] GetAllDistrictsQuery query, CancellationToken ct)
        => Ok(await getAllDistricts.HandleAsync(query, ct));

    [HttpGet("districts/{gid:int}")]
    public async Task<IActionResult> GetDistrict(
        int gid,
        [FromQuery] RegionGeometryAccuracyRequest geometry,
        CancellationToken ct)
    {
        var result = await getDistrictById.HandleAsync(new GetDistrictByIdQuery
        {
            Gid = gid,
            GeometryAccuracy = geometry.GeometryAccuracy,
        }, ct);
        if (result is null)
            throw new NotFoundException("District not found.");

        return Ok(result);
    }

    [HttpGet("governorates/{adm1Gid:int}/districts")]
    public async Task<IActionResult> GetDistrictsByGovernorate(
        int adm1Gid,
        [FromQuery] GetDistrictsByGovernorateQuery query,
        CancellationToken ct)
    {
        query.Adm1Gid = adm1Gid;
        return Ok(await getDistrictsByGovernorate.HandleAsync(query, ct));
    }

    [HttpPost("districts")]
    [Authorize(Policy = PolicyNames.AdminOnly)]
    public async Task<IActionResult> CreateDistrict([FromBody] CreateAdm2Request request, CancellationToken ct)
    {
        var result = await createDistrict.HandleAsync(new CreateDistrictCommand
        {
            Adm1Gid = request.Adm1Gid,
            NameEn = request.NameEn,
            NameAr = request.NameAr,
            Pcode = request.Pcode,
            ImageUrl = request.ImageUrl,
        }, ct);
        return CreatedAtAction(nameof(GetDistrict), new { gid = result.Gid }, result);
    }

    [HttpPut("districts/{gid:int}")]
    [Authorize(Policy = PolicyNames.AdminOnly)]
    public async Task<IActionResult> UpdateDistrict(int gid, [FromBody] UpdateAdm2Request request, CancellationToken ct)
    {
        var result = await updateDistrict.HandleAsync(new UpdateDistrictCommand
        {
            Gid = gid,
            NameEn = request.NameEn,
            NameAr = request.NameAr,
            ImageUrl = request.ImageUrl,
        }, ct);
        if (result is null)
            throw new NotFoundException("District not found.");

        return Ok(result);
    }

    [HttpDelete("districts/{gid:int}")]
    [Authorize(Policy = PolicyNames.AdminOnly)]
    public async Task<IActionResult> DeleteDistrict(int gid, CancellationToken ct)
    {
        var deleted = await deleteDistrict.HandleAsync(gid, ct);
        if (!deleted)
            throw new NotFoundException("District not found.");

        return NoContent();
    }

    // ──────────────────────────────────────────────
    // Neighbourhoods (ADM3)
    // ──────────────────────────────────────────────

    [HttpGet("neighbourhoods")]
    public async Task<IActionResult> GetNeighbourhoods([FromQuery] GetAllNeighbourhoodsQuery query, CancellationToken ct)
        => Ok(await getAllNeighbourhoods.HandleAsync(query, ct));

    [HttpGet("neighbourhoods/{gid:int}")]
    public async Task<IActionResult> GetNeighbourhood(
        int gid,
        [FromQuery] RegionGeometryAccuracyRequest geometry,
        CancellationToken ct)
    {
        var result = await getNeighbourhoodById.HandleAsync(new GetNeighbourhoodByIdQuery
        {
            Gid = gid,
            GeometryAccuracy = geometry.GeometryAccuracy,
        }, ct);
        if (result is null)
            throw new NotFoundException("Neighbourhood not found.");

        return Ok(result);
    }

    [HttpGet("districts/{adm2Gid:int}/neighbourhoods")]
    public async Task<IActionResult> GetNeighbourhoodsByDistrict(
        int adm2Gid,
        [FromQuery] GetNeighbourhoodsByDistrictQuery query,
        CancellationToken ct)
    {
        query.Adm2Gid = adm2Gid;
        return Ok(await getNeighbourhoodsByDistrict.HandleAsync(query, ct));
    }

    [HttpPost("neighbourhoods")]
    [Authorize(Policy = PolicyNames.AdminOnly)]
    public async Task<IActionResult> CreateNeighbourhood([FromBody] CreateAdm3Request request, CancellationToken ct)
    {
        var result = await createNeighbourhood.HandleAsync(new CreateNeighbourhoodCommand
        {
            Adm2Gid = request.Adm2Gid,
            NameEn = request.NameEn,
            NameAr = request.NameAr,
            Pcode = request.Pcode,
            ImageUrl = request.ImageUrl,
        }, ct);
        return CreatedAtAction(nameof(GetNeighbourhood), new { gid = result.Gid }, result);
    }

    [HttpPut("neighbourhoods/{gid:int}")]
    [Authorize(Policy = PolicyNames.AdminOnly)]
    public async Task<IActionResult> UpdateNeighbourhood(int gid, [FromBody] UpdateAdm3Request request, CancellationToken ct)
    {
        var result = await updateNeighbourhood.HandleAsync(new UpdateNeighbourhoodCommand
        {
            Gid = gid,
            NameEn = request.NameEn,
            NameAr = request.NameAr,
            ImageUrl = request.ImageUrl,
        }, ct);
        if (result is null)
            throw new NotFoundException("Neighbourhood not found.");

        return Ok(result);
    }

    [HttpDelete("neighbourhoods/{gid:int}")]
    [Authorize(Policy = PolicyNames.AdminOnly)]
    public async Task<IActionResult> DeleteNeighbourhood(int gid, CancellationToken ct)
    {
        var deleted = await deleteNeighbourhood.HandleAsync(gid, ct);
        if (!deleted)
            throw new NotFoundException("Neighbourhood not found.");

        return NoContent();
    }
}
