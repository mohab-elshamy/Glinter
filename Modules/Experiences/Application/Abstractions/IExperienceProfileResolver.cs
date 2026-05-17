namespace Glinter.Modules.Experiences.Application.Abstractions;

public interface IExperienceProfileResolver
{
    Task<Guid> GetCurrentExperienceProviderProfileIdAsync(
        CancellationToken cancellationToken = default);

    Task<Guid> GetCurrentTravelerProfileIdAsync(
        CancellationToken cancellationToken = default);
}