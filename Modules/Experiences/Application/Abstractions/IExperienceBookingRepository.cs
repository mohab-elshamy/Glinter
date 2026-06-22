using Glinter.Modules.Experiences.Domain.Entities;

namespace Glinter.Modules.Experiences.Application.Abstractions;

public interface IExperienceBookingRepository
{
    Task AddAsync(ExperienceBooking booking, CancellationToken cancellationToken = default);

    Task<List<ExperienceBooking>> GetByExperienceIdAsync(
        Guid experienceId,
        CancellationToken cancellationToken = default);

    Task<ExperienceBooking?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<ExperienceBooking?> GetForUpdateAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<bool> HasActiveBookingAsync(
        Guid availabilityId,
        Guid travelerProfileId,
        CancellationToken cancellationToken = default);

    Task<bool> HasCompletedBookingAsync(
        Guid experienceId,
        Guid travelerProfileId,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(ExperienceBooking booking, CancellationToken cancellationToken = default);

    Task AddBookingAndUpdateAvailabilityAsync(
        ExperienceBooking booking,
        ExperienceAvailability availability,
        CancellationToken cancellationToken = default);

    Task UpdateBookingAndAvailabilityAsync(
        ExperienceBooking booking,
        ExperienceAvailability availability,
        CancellationToken cancellationToken = default);

    Task<List<ExperienceBooking>> GetByTravelerProfileIdAsync(
        Guid travelerProfileId,
        CancellationToken cancellationToken = default);
}