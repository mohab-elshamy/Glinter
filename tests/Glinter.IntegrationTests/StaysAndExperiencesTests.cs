using Glinter.IntegrationTests.Infrastructure;

namespace Glinter.IntegrationTests;

[Collection(IntegrationTestCollection.Name)]
public sealed class StaysAndExperiencesTests : ApiTestBase
{
    public StaysAndExperiencesTests(GlinterApiFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Stay_ownership_privacy_cancellation_rebooking_and_reviews_are_enforced()
    {
        var adminToken = await GetAdminTokenAsync();
        var neighbourhoodId = await CreateRegionHierarchyAsync(adminToken);
        var ownerA = await CreateUserAsync("HotelOwner", "stay-owner-a");
        var ownerB = await CreateUserAsync("HotelOwner", "stay-owner-b");
        var travelerA = await CreateUserAsync("Traveler", "stay-traveler-a");
        var travelerB = await CreateUserAsync("Traveler", "stay-traveler-b");
        await UpsertHotelOwnerAsync(ownerA);
        await UpsertHotelOwnerAsync(ownerB);
        await UpsertTravelerAsync(travelerA);
        await UpsertTravelerAsync(travelerB);

        var stayBody = new
        {
            adm3Gid = neighbourhoodId,
            name = $"Integration Stay {Guid.NewGuid():N}",
            description = "Integration stay",
            address = "1 Integration Street",
            pricePerNight = 1000,
            currency = "EGP",
            maxGuests = 4,
            latitude = 30.05,
            longitude = 31.25,
            tags = new[] { "integration" }
        };
        var createStay = await SendAsync(
            HttpMethod.Post,
            "/api/stays",
            ownerA.Token,
            stayBody);
        Assert.Equal(HttpStatusCode.Created, createStay.StatusCode);
        using var stayJson = await ReadJsonAsync(createStay);
        var stayId = stayJson.RootElement.GetProperty("id").GetGuid();
        var ownerProfileId = stayJson.RootElement.GetProperty("ownerProfileId").GetGuid();

        var updateBody = new
        {
            name = "Unauthorized Stay Update",
            description = "Other owner",
            address = "2 Integration Street",
            pricePerNight = 1100,
            currency = "EGP",
            maxGuests = 4,
            latitude = 30.06,
            longitude = 31.26,
            tags = new[] { "other" }
        };
        var ownerViolation = await SendAsync(
            HttpMethod.Put,
            $"/api/stays/{stayId}",
            ownerB.Token,
            updateBody);
        await AssertProblemAsync(ownerViolation, 403, "forbidden");

        var checkIn = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(10));
        var checkOut = checkIn.AddDays(2);
        var bookingA = await SendAsync(
            HttpMethod.Post,
            $"/api/stays/{stayId}/bookings",
            travelerA.Token,
            new { checkInDate = checkIn, checkOutDate = checkOut, guestCount = 2 });
        bookingA.EnsureSuccessStatusCode();
        using var bookingAJson = await ReadJsonAsync(bookingA);
        var bookingAId = bookingAJson.RootElement.GetProperty("id").GetGuid();

        var wrongCancellation = await SendAsync(
            HttpMethod.Patch,
            $"/api/stay-bookings/{bookingAId}/cancel",
            travelerB.Token);
        await AssertProblemAsync(wrongCancellation, 403, "forbidden");

        var cancellation = await SendAsync(
            HttpMethod.Patch,
            $"/api/stay-bookings/{bookingAId}/cancel",
            travelerA.Token);
        cancellation.EnsureSuccessStatusCode();

        var rebooking = await SendAsync(
            HttpMethod.Post,
            $"/api/stays/{stayId}/bookings",
            travelerB.Token,
            new { checkInDate = checkIn, checkOutDate = checkOut, guestCount = 2 });
        rebooking.EnsureSuccessStatusCode();
        using var bookingBJson = await ReadJsonAsync(rebooking);
        var bookingBId = bookingBJson.RootElement.GetProperty("id").GetGuid();

        var search = await Client.GetAsync(
            $"/api/stays?search=Integration&currency=EGP&guests=2&sortBy=price_desc");
        search.EnsureSuccessStatusCode();
        using var searchJson = await ReadJsonAsync(search);
        Assert.Contains(
            searchJson.RootElement.EnumerateArray(),
            item => item.GetProperty("id").GetGuid() == stayId);

        var literalWildcardSearch = await Client.GetAsync("/api/stays?search=%25");
        literalWildcardSearch.EnsureSuccessStatusCode();
        using var literalWildcardJson = await ReadJsonAsync(literalWildcardSearch);
        Assert.DoesNotContain(
            literalWildcardJson.RootElement.EnumerateArray(),
            item => item.GetProperty("id").GetGuid() == stayId);

        var unavailableSearch = await Client.GetAsync(
            $"/api/stays?checkInDate={checkIn:yyyy-MM-dd}&checkOutDate={checkOut:yyyy-MM-dd}");
        unavailableSearch.EnsureSuccessStatusCode();
        using var unavailableJson = await ReadJsonAsync(unavailableSearch);
        Assert.DoesNotContain(
            unavailableJson.RootElement.EnumerateArray(),
            item => item.GetProperty("id").GetGuid() == stayId);

        var privateBookings = await SendAsync(
            HttpMethod.Get,
            $"/api/stays/{stayId}/bookings",
            ownerB.Token);
        await AssertProblemAsync(privateBookings, 403, "forbidden");

        await Factory.ExecuteAsync(
            $"""
             UPDATE stay_bookings
             SET "Status" = 'Completed',
                 "CheckInDate" = CURRENT_DATE - 3,
                 "CheckOutDate" = CURRENT_DATE - 1
             WHERE "Id" = '{bookingBId}'
             """);

        var review = await SendAsync(
            HttpMethod.Post,
            $"/api/stays/{stayId}/reviews",
            travelerB.Token,
            new { rating = 5, comment = "Integration review" });
        review.EnsureSuccessStatusCode();
        using var reviewJson = await ReadJsonAsync(review);
        var reviewId = reviewJson.RootElement.GetProperty("id").GetGuid();

        var reviewViolation = await SendAsync(
            HttpMethod.Put,
            $"/api/stay-reviews/{reviewId}",
            travelerA.Token,
            new { rating = 1, comment = "Not my review" });
        await AssertProblemAsync(reviewViolation, 403, "forbidden");

        var persistedOwner = await Factory.ScalarAsync<Guid>(
            $"""SELECT "OwnerProfileId" FROM stays WHERE "Id" = '{stayId}'""");
        Assert.Equal(ownerProfileId, persistedOwner);
    }

    [Fact]
    public async Task Experience_capacity_duplicates_approval_and_ownership_are_enforced()
    {
        var adminToken = await GetAdminTokenAsync();
        var neighbourhoodId = await CreateRegionHierarchyAsync(adminToken);
        var providerA = await CreateUserAsync("ExperienceProvider", "experience-provider-a");
        var providerB = await CreateUserAsync("ExperienceProvider", "experience-provider-b");
        var travelerA = await CreateUserAsync("Traveler", "experience-traveler-a");
        var travelerB = await CreateUserAsync("Traveler", "experience-traveler-b");
        await UpsertProviderAsync(providerA);
        await UpsertProviderAsync(providerB);
        await UpsertTravelerAsync(travelerA);
        await UpsertTravelerAsync(travelerB);

        var categories = await Client.GetAsync("/api/experience-categories?page=1&pageSize=10");
        categories.EnsureSuccessStatusCode();
        using var categoriesJson = await ReadJsonAsync(categories);
        var categoryId = categoriesJson.RootElement[0].GetProperty("id").GetGuid();

        var vibes = await Client.GetAsync("/api/vibes?page=1&pageSize=10");
        vibes.EnsureSuccessStatusCode();
        using var vibesJson = await ReadJsonAsync(vibes);
        var vibeId = vibesJson.RootElement[0].GetProperty("id").GetGuid();

        var title = $"Integration Experience {Guid.NewGuid():N}";
        var experienceBody = new
        {
            categoryId,
            adm3Gid = neighbourhoodId,
            title,
            description = "Integration experience",
            locationName = "Integration neighbourhood",
            pricePerPerson = 500,
            currency = "EGP",
            durationMinutes = 120,
            maxGuests = 6,
            latitude = 30.05,
            longitude = 31.25,
            vibeIds = new[] { vibeId },
            tags = new[] { "integration" }
        };
        var createExperience = await SendAsync(
            HttpMethod.Post,
            "/api/experiences",
            providerA.Token,
            experienceBody);
        Assert.Equal(HttpStatusCode.Created, createExperience.StatusCode);
        using var experienceJson = await ReadJsonAsync(createExperience);
        var experienceId = experienceJson.RootElement.GetProperty("id").GetGuid();

        var providerViolation = await SendAsync(
            HttpMethod.Put,
            $"/api/experiences/{experienceId}",
            providerB.Token,
            experienceBody);
        await AssertProblemAsync(providerViolation, 403, "forbidden");

        var rejectWithoutNotes = await SendAsync(
            HttpMethod.Patch,
            $"/api/admin/experiences/{experienceId}/approval-status",
            adminToken,
            new { approvalStatus = "Rejected" });
        await AssertProblemAsync(rejectWithoutNotes, 400, "validation_error");

        var approve = await SendAsync(
            HttpMethod.Patch,
            $"/api/admin/experiences/{experienceId}/approval-status",
            adminToken,
            new { approvalStatus = "Approved", moderationNotes = "Integration approval" });
        approve.EnsureSuccessStatusCode();

        var moderationHistory = await SendAsync(
            HttpMethod.Get,
            $"/api/admin/experiences/{experienceId}/moderation-history",
            adminToken);
        moderationHistory.EnsureSuccessStatusCode();
        using var moderationHistoryJson = await ReadJsonAsync(moderationHistory);
        Assert.Contains(
            moderationHistoryJson.RootElement.EnumerateArray(),
            item => item.GetProperty("action").GetString() == "Submitted");
        Assert.Contains(
            moderationHistoryJson.RootElement.EnumerateArray(),
            item => item.GetProperty("action").GetString() == "AdminDecision" &&
                    item.GetProperty("newStatus").GetString() == "Approved");

        var start = DateTime.UtcNow.AddDays(5);
        var availability = await SendAsync(
            HttpMethod.Post,
            $"/api/experiences/{experienceId}/availability",
            providerA.Token,
            new { startTimeUtc = start, endTimeUtc = start.AddHours(2), capacity = 3 });
        Assert.Equal(HttpStatusCode.Created, availability.StatusCode);
        using var availabilityJson = await ReadJsonAsync(availability);
        var availabilityId = availabilityJson.RootElement.GetProperty("id").GetGuid();

        var experienceSearch = await Client.GetAsync(
            $"/api/experiences?search=Integration&currency=EGP&minDurationMinutes=60" +
            $"&maxDurationMinutes=180&availableFromUtc={Uri.EscapeDataString(start.ToString("O"))}" +
            $"&availableToUtc={Uri.EscapeDataString(start.AddHours(2).ToString("O"))}" +
            "&guests=3&sortBy=duration_asc");
        experienceSearch.EnsureSuccessStatusCode();
        using var experienceSearchJson = await ReadJsonAsync(experienceSearch);
        Assert.Contains(
            experienceSearchJson.RootElement.EnumerateArray(),
            item => item.GetProperty("id").GetGuid() == experienceId);

        var literalExperienceWildcardSearch =
            await Client.GetAsync("/api/experiences?search=%25");
        literalExperienceWildcardSearch.EnsureSuccessStatusCode();
        using var literalExperienceWildcardJson =
            await ReadJsonAsync(literalExperienceWildcardSearch);
        Assert.DoesNotContain(
            literalExperienceWildcardJson.RootElement.EnumerateArray(),
            item => item.GetProperty("id").GetGuid() == experienceId);

        var unspecifiedAvailabilitySearch = await Client.GetAsync(
            "/api/experiences?availableFromUtc=2026-07-01T10:00:00" +
            "&availableToUtc=2026-07-01T12:00:00");
        await AssertProblemAsync(
            unspecifiedAvailabilitySearch,
            400,
            "validation_error");

        var bookingA = await SendAsync(
            HttpMethod.Post,
            $"/api/experiences/{experienceId}/bookings",
            travelerA.Token,
            new { availabilityId, guestsCount = 2 });
        bookingA.EnsureSuccessStatusCode();
        using var bookingAJson = await ReadJsonAsync(bookingA);
        var bookingAId = bookingAJson.RootElement.GetProperty("id").GetGuid();

        var duplicate = await SendAsync(
            HttpMethod.Post,
            $"/api/experiences/{experienceId}/bookings",
            travelerA.Token,
            new { availabilityId, guestsCount = 1 });
        await AssertProblemAsync(duplicate, 409, "conflict");

        var bookingB = await SendAsync(
            HttpMethod.Post,
            $"/api/experiences/{experienceId}/bookings",
            travelerB.Token,
            new { availabilityId, guestsCount = 1 });
        bookingB.EnsureSuccessStatusCode();
        using var bookingBJson = await ReadJsonAsync(bookingB);
        var bookingBId = bookingBJson.RootElement.GetProperty("id").GetGuid();

        var cancel = await SendAsync(
            HttpMethod.Patch,
            $"/api/experience-bookings/{bookingAId}/cancel",
            travelerA.Token);
        cancel.EnsureSuccessStatusCode();
        var cancelAgain = await SendAsync(
            HttpMethod.Patch,
            $"/api/experience-bookings/{bookingAId}/cancel",
            travelerA.Token);
        await AssertProblemAsync(cancelAgain, 409, "conflict");

        var bookedCount = await Factory.ScalarAsync<int>(
            $"""SELECT "BookedCount" FROM experience_availability WHERE "Id" = '{availabilityId}'""");
        Assert.Equal(1, bookedCount);

        await Factory.ExecuteAsync(
            $"""
             UPDATE experience_availability
             SET "StartTimeUtc" = NOW() - INTERVAL '3 hours',
                 "EndTimeUtc" = NOW() - INTERVAL '1 hour'
             WHERE "Id" = '{availabilityId}'
             """);
        var complete = await SendAsync(
            HttpMethod.Patch,
            $"/api/experience-bookings/{bookingBId}/complete",
            providerA.Token);
        complete.EnsureSuccessStatusCode();

        var review = await SendAsync(
            HttpMethod.Post,
            $"/api/experiences/{experienceId}/reviews",
            travelerB.Token,
            new { rating = 5, comment = "Integration review" });
        Assert.Equal(HttpStatusCode.Created, review.StatusCode);
        using var reviewJson = await ReadJsonAsync(review);
        var reviewId = reviewJson.RootElement.GetProperty("id").GetGuid();

        var reviewViolation = await SendAsync(
            HttpMethod.Put,
            $"/api/experience-reviews/{reviewId}",
            travelerA.Token,
            new { rating = 1, comment = "Not my review" });
        await AssertProblemAsync(reviewViolation, 403, "forbidden");

        var update = await SendAsync(
            HttpMethod.Put,
            $"/api/experiences/{experienceId}",
            providerA.Token,
            new
            {
                categoryId,
                adm3Gid = neighbourhoodId,
                title = $"{title} Updated",
                description = "Updated integration experience",
                locationName = "Integration neighbourhood",
                pricePerPerson = 550,
                currency = "EGP",
                durationMinutes = 120,
                maxGuests = 6,
                latitude = 30.05,
                longitude = 31.25,
                vibeIds = new[] { vibeId },
                tags = new[] { "updated" }
            });
        update.EnsureSuccessStatusCode();
        using var updateJson = await ReadJsonAsync(update);
        Assert.Equal(
            "Pending",
            updateJson.RootElement.GetProperty("approvalStatus").GetString());

        var updatedHistory = await SendAsync(
            HttpMethod.Get,
            $"/api/admin/experiences/{experienceId}/moderation-history",
            adminToken);
        updatedHistory.EnsureSuccessStatusCode();
        using var updatedHistoryJson = await ReadJsonAsync(updatedHistory);
        Assert.Contains(
            updatedHistoryJson.RootElement.EnumerateArray(),
            item => item.GetProperty("action").GetString() == "ProviderUpdated" &&
                    item.GetProperty("previousStatus").GetString() == "Approved" &&
                    item.GetProperty("newStatus").GetString() == "Pending");
    }
}
