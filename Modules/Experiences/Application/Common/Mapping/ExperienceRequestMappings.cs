using Glinter.Modules.Experiences.Application.Experiences.Commands.CreateExperience;
using Glinter.Modules.Experiences.Application.Experiences.Commands.UpdateExperience;
using Glinter.Modules.Experiences.Application.Experiences.Commands.CreateExperienceAvailability;
using Glinter.Modules.Experiences.Application.Experiences.Commands.CreateExperienceBooking;
using Glinter.Modules.Experiences.Application.Experiences.Commands.CreateExperienceReview;
using Glinter.Modules.Experiences.Application.Experiences.Commands.UpdateExperienceReview;
using Glinter.Modules.Experiences.Application.Experiences.Dtos;
using Glinter.Modules.Experiences.Application.Experiences.Queries.GetAllExperiences;

namespace Glinter.Modules.Experiences.Application.Common.Mapping;

public static class ExperienceRequestMappings
{
    public static CreateExperienceCommand ToCommand(this CreateExperienceRequestDto request)
    {
        return new CreateExperienceCommand
        {
            CategoryId = request.CategoryId,
            AreaId = request.AreaId,
            Title = request.Title,
            Description = request.Description,
            LocationName = request.LocationName,
            PricePerPerson = request.PricePerPerson,
            Currency = request.Currency,
            DurationMinutes = request.DurationMinutes,
            MaxGuests = request.MaxGuests,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            VibeIds = request.VibeIds,
            Tags = request.Tags
        };
    }

    public static UpdateExperienceCommand ToCommand(this UpdateExperienceRequestDto request, Guid experienceId)
    {
        return new UpdateExperienceCommand
        {
            Id = experienceId,
            CategoryId = request.CategoryId,
            AreaId = request.AreaId,
            Title = request.Title,
            Description = request.Description,
            LocationName = request.LocationName,
            PricePerPerson = request.PricePerPerson,
            Currency = request.Currency,
            DurationMinutes = request.DurationMinutes,
            MaxGuests = request.MaxGuests,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            VibeIds = request.VibeIds,
            Tags = request.Tags
        };
    }

    public static CreateExperienceAvailabilityCommand ToCommand(
        this CreateExperienceAvailabilityRequestDto request,
        Guid experienceId)
    {
        return new CreateExperienceAvailabilityCommand
        {
            ExperienceId = experienceId,
            StartTimeUtc = request.StartTimeUtc,
            EndTimeUtc = request.EndTimeUtc,
            Capacity = request.Capacity
        };
    }

    public static CreateExperienceBookingCommand ToCommand(
        this CreateExperienceBookingRequestDto request,
        Guid experienceId)
    {
        return new CreateExperienceBookingCommand
        {
            ExperienceId = experienceId,
            AvailabilityId = request.AvailabilityId,
            GuestsCount = request.GuestsCount
        };
    }

    public static CreateExperienceReviewCommand ToCommand(
        this CreateExperienceReviewRequestDto request,
        Guid experienceId)
    {
        return new CreateExperienceReviewCommand
        {
            ExperienceId = experienceId,
            Rating = request.Rating,
            Comment = request.Comment
        };
    }

    public static UpdateExperienceReviewCommand ToCommand(
        this UpdateExperienceReviewRequestDto request,
        Guid reviewId)
    {
        return new UpdateExperienceReviewCommand
        {
            ReviewId = reviewId,
            Rating = request.Rating,
            Comment = request.Comment
        };
    }
    
    public static GetAllExperiencesQuery ToQuery(this GetExperiencesRequestDto request)
    {
        return new GetAllExperiencesQuery
        {
            AreaId = request.AreaId,
            CategoryId = request.CategoryId,
            MinPrice = request.MinPrice,
            MaxPrice = request.MaxPrice,
            Guests = request.Guests,
            VibeId = request.VibeId,
            Tag = request.Tag
        };
    }
}