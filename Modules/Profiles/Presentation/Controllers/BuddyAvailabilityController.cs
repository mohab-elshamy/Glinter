using Glinter.Modules.IdentityAccess.Domain.Constants;
using Glinter.Modules.Profiles.Application.Profiles.Dtos;
using Glinter.Modules.Profiles.Application.Profiles.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Glinter.Modules.Profiles.Presentation.Controllers;

[ApiController]
[Route("api/buddy-availability")]
[Authorize(
    AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,
    Roles = RoleNames.LocalBuddy)]
public sealed class BuddyAvailabilityController(BuddyEngagementService service) : ControllerBase
{
    [HttpPut("{availabilityId:guid}")]
    public async Task<IActionResult> Update(
        Guid availabilityId,
        [FromBody] BuddyAvailabilityRequest request,
        CancellationToken cancellationToken) =>
        Ok(await service.UpdateAvailabilityAsync(
            availabilityId,
            request,
            cancellationToken));

    [HttpPatch("{availabilityId:guid}/activate")]
    public async Task<IActionResult> Activate(
        Guid availabilityId,
        CancellationToken cancellationToken) =>
        Ok(await service.SetAvailabilityActiveAsync(
            availabilityId,
            true,
            cancellationToken));

    [HttpPatch("{availabilityId:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(
        Guid availabilityId,
        CancellationToken cancellationToken) =>
        Ok(await service.SetAvailabilityActiveAsync(
            availabilityId,
            false,
            cancellationToken));
}
