namespace Glinter.Modules.Profiles.Application.Abstractions;

public interface IProfilesReadService
{
    Task<Guid?> GetTravelerProfileIdByUserIdAsync(Guid userId, CancellationToken cancellationToken);
    Task<Guid?> GetHotelOwnerProfileIdByUserIdAsync(Guid userId, CancellationToken cancellationToken);
    Task<Guid?> GetLocalBuddyProfileIdByUserIdAsync(Guid userId, CancellationToken cancellationToken);
    Task<Guid?> GetExperienceProviderProfileIdByUserIdAsync(Guid userId, CancellationToken cancellationToken);
}