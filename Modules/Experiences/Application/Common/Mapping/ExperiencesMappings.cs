using Glinter.Modules.Experiences.Application.Experiences.Dtos;
using Glinter.Modules.Experiences.Domain.Entities;

namespace Glinter.Modules.Experiences.Application.Common.Mapping;

public static class ExperiencesMappings
{
    public static ExperienceCategoryResponseDto ToCategoryResponse(ExperienceCategory category)
    {
        return new ExperienceCategoryResponseDto
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description
        };
    }

    public static VibeResponseDto ToVibeResponse(Vibe vibe)
    {
        return new VibeResponseDto
        {
            Id = vibe.Id,
            Name = vibe.Name
        };
    }

    public static ExperienceTagDto ToTagDto(ExperienceTag tag)
    {
        return new ExperienceTagDto
        {
            Id = tag.Id,
            ExperienceId = tag.ExperienceId,
            Name = tag.Name
        };
    }

    public static ExperienceResponseDto ToExperienceResponse(Experience experience)
    {
        return new ExperienceResponseDto
        {
            Id = experience.Id,
            ProviderProfileId = experience.ProviderProfileId,
            CategoryId = experience.CategoryId,
            CategoryName = experience.Category?.Name,
            AreaId = experience.AreaId,
            Title = experience.Title,
            Description = experience.Description,
            LocationName = experience.LocationName,
            PricePerPerson = experience.PricePerPerson,
            Currency = experience.Currency,
            DurationMinutes = experience.DurationMinutes,
            MaxGuests = experience.MaxGuests,
            Latitude = experience.Latitude,
            Longitude = experience.Longitude,
            IsActive = experience.IsActive,
            CreatedAtUtc = experience.CreatedAtUtc,
            UpdatedAtUtc = experience.UpdatedAtUtc,
            Vibes = experience.ExperienceVibes
                .Where(x => x.Vibe != null)
                .Select(x => ToVibeResponse(x.Vibe!))
                .ToList(),
            Tags = experience.Tags
                .Select(ToTagDto)
                .ToList()
        };
    }

    public static ExperienceSummaryDto ToExperienceSummary(Experience experience)
    {
        return new ExperienceSummaryDto
        {
            Id = experience.Id,
            ProviderProfileId = experience.ProviderProfileId,
            CategoryId = experience.CategoryId,
            CategoryName = experience.Category?.Name,
            AreaId = experience.AreaId,
            Title = experience.Title,
            LocationName = experience.LocationName,
            PricePerPerson = experience.PricePerPerson,
            Currency = experience.Currency,
            DurationMinutes = experience.DurationMinutes,
            MaxGuests = experience.MaxGuests,
            IsActive = experience.IsActive,
            Vibes = experience.ExperienceVibes
                .Where(x => x.Vibe != null)
                .Select(x => x.Vibe!.Name)
                .ToList(),
            Tags = experience.Tags
                .Select(x => x.Name)
                .ToList()
        };
    }

    public static ExperienceAvailabilityResponseDto ToAvailabilityResponse(
        ExperienceAvailability availability)
    {
        return new ExperienceAvailabilityResponseDto
        {
            Id = availability.Id,
            ExperienceId = availability.ExperienceId,
            StartTimeUtc = availability.StartTimeUtc,
            EndTimeUtc = availability.EndTimeUtc,
            Capacity = availability.Capacity,
            BookedCount = availability.BookedCount,
            RemainingCapacity = availability.Capacity - availability.BookedCount,
            IsActive = availability.IsActive,
            CreatedAtUtc = availability.CreatedAtUtc
        };
    }

    public static ExperienceBookingResponseDto ToBookingResponse(ExperienceBooking booking)
    {
        return new ExperienceBookingResponseDto
        {
            Id = booking.Id,
            ExperienceId = booking.ExperienceId,
            AvailabilityId = booking.AvailabilityId,
            TravelerProfileId = booking.TravelerProfileId,
            GuestsCount = booking.GuestsCount,
            TotalPrice = booking.TotalPrice,
            Status = booking.Status.ToString(),
            CreatedAtUtc = booking.CreatedAtUtc,
            CancelledAtUtc = booking.CancelledAtUtc,
            CompletedAtUtc = booking.CompletedAtUtc
        };
    }

    public static ExperienceReviewResponseDto ToReviewResponse(ExperienceReview review)
    {
        return new ExperienceReviewResponseDto
        {
            Id = review.Id,
            ExperienceId = review.ExperienceId,
            TravelerProfileId = review.TravelerProfileId,
            Rating = review.Rating,
            Comment = review.Comment,
            CreatedAtUtc = review.CreatedAtUtc,
            UpdatedAtUtc = review.UpdatedAtUtc
        };
    }
}