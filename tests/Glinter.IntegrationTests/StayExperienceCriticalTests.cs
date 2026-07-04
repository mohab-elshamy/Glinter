using Glinter.IntegrationTests.Infrastructure;

namespace Glinter.IntegrationTests;

[Collection(IntegrationTestCollection.Name)]
public sealed class StayExperienceCriticalTests : ApiTestBase
{
    public StayExperienceCriticalTests(GlinterApiFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Another_hotel_owner_cannot_update_a_stay_they_do_not_own()
    {
        // Arrange
        var owner = await CreateUserAsync("HotelOwner", "stay-owner");
        var attacker = await CreateUserAsync("HotelOwner", "stay-attacker");
        await UpsertHotelOwnerAsync(owner);
        await UpsertHotelOwnerAsync(attacker);
        var stayId = await CreateStayAsync(owner, "Ownership Protected Stay");

        // Act
        var response = await SendAsync(
            HttpMethod.Put,
            $"/api/stays/{stayId}",
            attacker.Token,
            StayPayload("Attacker Rename"));

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        using var json = await ReadJsonAsync(response);
        Assert.Contains("manage", json.RootElement.GetProperty("message").GetString());
        Assert.Equal(
            "Ownership Protected Stay",
            await Factory.ScalarAsync<string>(
                $"""SELECT "Name" FROM stays.stays WHERE "Id" = {stayId}"""));
    }

    [Fact]
    public async Task Stay_booking_rejects_invalid_dates_and_an_inactive_stay()
    {
        // Arrange
        var owner = await CreateUserAsync("HotelOwner", "stay-booking-owner");
        var traveler = await CreateUserAsync("Traveler", "stay-booking-traveler");
        await UpsertHotelOwnerAsync(owner);
        await UpsertTravelerAsync(traveler);
        var stayId = await CreateStayAsync(owner, "Booking Guard Stay");
        var checkIn = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5));

        // Act
        var invalidDates = await SendAsync(
            HttpMethod.Post,
            $"/api/stays/{stayId}/bookings",
            traveler.Token,
            new
            {
                checkInDate = checkIn,
                checkOutDate = checkIn.AddDays(-1),
                guestCount = 1
            });
        var deactivate = await SendAsync(
            HttpMethod.Patch,
            $"/api/stays/{stayId}/deactivate",
            owner.Token);
        var inactive = await SendAsync(
            HttpMethod.Post,
            $"/api/stays/{stayId}/bookings",
            traveler.Token,
            new
            {
                checkInDate = checkIn,
                checkOutDate = checkIn.AddDays(2),
                guestCount = 1
            });

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, invalidDates.StatusCode);
        Assert.Equal(HttpStatusCode.OK, deactivate.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, inactive.StatusCode);
        Assert.Equal(
            0,
            await Factory.ScalarAsync<long>(
                $"""SELECT count(*) FROM stays.stay_bookings WHERE "StayId" = {stayId}"""));
    }

    [Fact]
    public async Task Another_provider_cannot_update_an_experience_they_do_not_own()
    {
        // Arrange
        var provider = await CreateUserAsync("ExperienceProvider", "experience-owner");
        var attacker = await CreateUserAsync("ExperienceProvider", "experience-attacker");
        await UpsertProviderAsync(provider);
        await UpsertProviderAsync(attacker);
        var experienceId = await CreateExperienceAsync(
            provider,
            "Ownership Protected Experience");

        // Act
        var response = await SendAsync(
            HttpMethod.Put,
            $"/api/experiences/{experienceId}",
            attacker.Token,
            ExperiencePayload("Attacker Rename"));

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        using var json = await ReadJsonAsync(response);
        Assert.Contains("manage", json.RootElement.GetProperty("message").GetString());
        Assert.Equal(
            "Ownership Protected Experience",
            await Factory.ScalarAsync<string>(
                $"""SELECT "Name" FROM experiences.experiences WHERE "Id" = {experienceId}"""));
    }

    [Fact]
    public async Task Experience_booking_rejects_over_capacity_without_reserving_places()
    {
        // Arrange
        var provider = await CreateUserAsync("ExperienceProvider", "capacity-provider");
        var traveler = await CreateUserAsync("Traveler", "capacity-traveler");
        await UpsertProviderAsync(provider);
        await UpsertTravelerAsync(traveler);
        var experienceId = await CreateExperienceAsync(provider, "Capacity Guard Experience");
        var adminToken = await GetAdminTokenAsync();
        (await SendAsync(
            HttpMethod.Patch,
            $"/api/admin/experiences/{experienceId}/moderation",
            adminToken,
            new { moderationStatus = "Approved" })).EnsureSuccessStatusCode();
        var availability = await SendAsync(
            HttpMethod.Post,
            $"/api/experiences/{experienceId}/availability",
            provider.Token,
            new
            {
                startTimeUtc = DateTime.UtcNow.AddDays(4),
                endTimeUtc = DateTime.UtcNow.AddDays(4).AddHours(2),
                capacity = 2,
                pricePerPerson = 25
            });
        Assert.Equal(HttpStatusCode.Created, availability.StatusCode);
        using var availabilityJson = await ReadJsonAsync(availability);
        var availabilityId = availabilityJson.RootElement.GetProperty("id").GetGuid();

        // Act
        var response = await SendAsync(
            HttpMethod.Post,
            $"/api/experiences/{experienceId}/bookings",
            traveler.Token,
            new { availabilityId, guestsCount = 3 });

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var json = await ReadJsonAsync(response);
        Assert.Contains("capacity", json.RootElement.GetProperty("message").GetString());
        Assert.Equal(
            0,
            await Factory.ScalarAsync<long>(
                $"""
                 SELECT count(*)
                 FROM experiences.experience_bookings
                 WHERE "AvailabilityId" = '{availabilityId}'
                 """));
    }

    private async Task<int> CreateStayAsync(TestUser owner, string name)
    {
        var response = await SendAsync(
            HttpMethod.Post,
            "/api/stays",
            owner.Token,
            StayPayload(name));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var json = await ReadJsonAsync(response);
        return json.RootElement.GetProperty("id").GetInt32();
    }

    private static object StayPayload(string name) => new
    {
        name,
        price = 300,
        description = "Focused integration test stay",
        latitude = 30.05,
        longitude = 31.25,
        imageLinks = Array.Empty<string>(),
        amenities = Array.Empty<string>(),
        bookingPlatforms = Array.Empty<object>()
    };

    private async Task<int> CreateExperienceAsync(TestUser provider, string name)
    {
        var response = await SendAsync(
            HttpMethod.Post,
            "/api/experiences",
            provider.Token,
            ExperiencePayload(name));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var json = await ReadJsonAsync(response);
        return json.RootElement.GetProperty("id").GetInt32();
    }

    private static object ExperiencePayload(string name) => new
    {
        category = "Historical",
        name,
        description = "Focused integration test experience",
        address = "Cairo",
        latitude = 30.05,
        longitude = 31.25,
        featuredImageLinks = Array.Empty<string>(),
        hours = Array.Empty<object>(),
        popularTimes = Array.Empty<object>(),
        priceRange = "$25",
        amenities = Array.Empty<string>()
    };
}
