using Glinter.Modules.IdentityAccess.Domain.Constants;
using Glinter.Shared.Application.Dashboards;
using Glinter.Shared.Infrastructure.Dashboards;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Glinter.Shared.Presentation.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize]
public sealed class DashboardsController : ControllerBase
{
    private readonly DashboardService _dashboardService;

    public DashboardsController(DashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet("admin")]
    [Authorize(Roles = RoleNames.Admin)]
    public async Task<ActionResult<AdminDashboardDto>> GetAdmin(
        CancellationToken cancellationToken) =>
        Ok(await _dashboardService.GetAdminAsync(cancellationToken));

    [HttpGet("hotel-owner")]
    [Authorize(Roles = RoleNames.HotelOwner)]
    public async Task<ActionResult<HotelOwnerDashboardDto>> GetHotelOwner(
        CancellationToken cancellationToken) =>
        Ok(await _dashboardService.GetHotelOwnerAsync(cancellationToken));

    [HttpGet("experience-provider")]
    [Authorize(Roles = RoleNames.ExperienceProvider)]
    public async Task<ActionResult<ExperienceProviderDashboardDto>> GetExperienceProvider(
        CancellationToken cancellationToken) =>
        Ok(await _dashboardService.GetExperienceProviderAsync(cancellationToken));
}
