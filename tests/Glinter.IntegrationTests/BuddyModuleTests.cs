using Glinter.IntegrationTests.Infrastructure;

namespace Glinter.IntegrationTests;

[Collection(IntegrationTestCollection.Name)]
public sealed class BuddyModuleTests : ApiTestBase
{
    public BuddyModuleTests(GlinterApiFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Anonymous_user_cannot_create_a_buddy_request()
    {
        var response = await Client.PostAsJsonAsync(
            "/api/buddy/requests",
            new
            {
                localBuddyUserId = Guid.NewGuid(),
                availabilityId = Guid.NewGuid()
            });
        await AssertProblemAsync(response, 401, "authentication_required");
    }

    [Fact]
    public async Task Traveler_profile_and_approved_buddy_are_required()
    {
        var scenario = await CreateBuddyScenarioAsync(approved: true);
        var travelerWithoutProfile = await CreateUserAsync(
            "Traveler", "buddy-no-profile");

        var missingProfile = await CreateRequestAsync(
            travelerWithoutProfile,
            scenario.Buddy.UserId,
            scenario.AvailabilityId);
        await AssertProblemAsync(missingProfile, 400, "validation_error");

        var unapproved = await CreateBuddyScenarioAsync(approved: false);
        var traveler = await CreateUserAsync("Traveler", "buddy-unapproved");
        await UpsertTravelerAsync(traveler);
        var hiddenBuddy = await CreateRequestAsync(
            traveler,
            unapproved.Buddy.UserId,
            unapproved.AvailabilityId);
        await AssertProblemAsync(hiddenBuddy, 404, "not_found");
    }

    [Fact]
    public async Task Traveler_cannot_request_themselves()
    {
        var traveler = await CreateUserAsync("Traveler", "buddy-self");
        await UpsertTravelerAsync(traveler);
        var response = await CreateRequestAsync(
            traveler,
            traveler.UserId,
            Guid.NewGuid());
        await AssertProblemAsync(response, 400, "validation_error");
    }

    [Fact]
    public async Task Availability_rejects_past_and_reversed_dates()
    {
        var buddy = await CreateUserAsync("LocalBuddy", "buddy-invalid-dates");
        var profile = await SendAsync(
            HttpMethod.Put,
            "/api/profiles/local-buddy",
            buddy.Token,
            new
            {
                displayName = "Date Validator",
                city = "Cairo",
                interestIds = Array.Empty<Guid>()
            });
        profile.EnsureSuccessStatusCode();

        var past = await SendAsync(
            HttpMethod.Post,
            $"/api/local-buddies/{buddy.UserId}/availability",
            buddy.Token,
            new
            {
                startTimeUtc = DateTime.UtcNow.AddHours(-2),
                endTimeUtc = DateTime.UtcNow.AddHours(-1),
                price = 0
            });
        await AssertProblemAsync(past, 400, "validation_error");

        var reversed = await SendAsync(
            HttpMethod.Post,
            $"/api/local-buddies/{buddy.UserId}/availability",
            buddy.Token,
            new
            {
                startTimeUtc = DateTime.UtcNow.AddDays(1),
                endTimeUtc = DateTime.UtcNow.AddHours(1),
                price = 0
            });
        await AssertProblemAsync(reversed, 400, "validation_error");
    }

    [Fact]
    public async Task Request_visibility_decisions_and_final_transitions_are_owned()
    {
        var scenario = await CreateBuddyScenarioAsync(approved: true);
        var traveler = await CreateUserAsync("Traveler", "buddy-owner");
        var otherTraveler = await CreateUserAsync("Traveler", "buddy-other-traveler");
        var otherBuddy = await CreateBuddyScenarioAsync(approved: true);
        await UpsertTravelerAsync(traveler);
        await UpsertTravelerAsync(otherTraveler);

        var created = await CreateRequestAsync(
            traveler, scenario.Buddy.UserId, scenario.AvailabilityId, "City walk");
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        using var createdJson = await ReadJsonAsync(created);
        var requestId = createdJson.RootElement.GetProperty("id").GetGuid();
        Assert.Equal("Pending", createdJson.RootElement.GetProperty("status").GetString());

        var duplicate = await CreateRequestAsync(
            traveler, scenario.Buddy.UserId, scenario.AvailabilityId);
        await AssertProblemAsync(duplicate, 400, "validation_error");

        var incoming = await SendAsync(
            HttpMethod.Get, "/api/buddy/requests/incoming", scenario.Buddy.Token);
        incoming.EnsureSuccessStatusCode();
        using var incomingJson = await ReadJsonAsync(incoming);
        Assert.Contains(
            incomingJson.RootElement.EnumerateArray(),
            item => item.GetProperty("id").GetGuid() == requestId);

        var otherIncoming = await SendAsync(
            HttpMethod.Get,
            "/api/buddy/requests/incoming",
            otherBuddy.Buddy.Token);
        otherIncoming.EnsureSuccessStatusCode();
        using var otherIncomingJson = await ReadJsonAsync(otherIncoming);
        Assert.DoesNotContain(
            otherIncomingJson.RootElement.EnumerateArray(),
            item => item.GetProperty("id").GetGuid() == requestId);

        var otherTravelerRead = await SendAsync(
            HttpMethod.Get,
            $"/api/buddy/requests/{requestId}",
            otherTraveler.Token);
        await AssertProblemAsync(otherTravelerRead, 403, "forbidden");

        var wrongBuddyDecision = await SendAsync(
            HttpMethod.Patch,
            $"/api/buddy/requests/{requestId}/accept",
            otherBuddy.Buddy.Token);
        await AssertProblemAsync(wrongBuddyDecision, 403, "forbidden");

        var accepted = await SendAsync(
            HttpMethod.Patch,
            $"/api/buddy/requests/{requestId}/accept",
            scenario.Buddy.Token);
        accepted.EnsureSuccessStatusCode();

        var repeated = await SendAsync(
            HttpMethod.Patch,
            $"/api/buddy/requests/{requestId}/reject",
            scenario.Buddy.Token);
        await AssertProblemAsync(repeated, 409, "conflict");

        var mine = await SendAsync(
            HttpMethod.Get, "/api/buddy/requests/mine", traveler.Token);
        mine.EnsureSuccessStatusCode();
        using var mineJson = await ReadJsonAsync(mine);
        Assert.Contains(
            mineJson.RootElement.EnumerateArray(),
            item => item.GetProperty("id").GetGuid() == requestId &&
                    item.GetProperty("status").GetString() == "Accepted");

        var notifications = await SendAsync(
            HttpMethod.Get,
            "/api/notifications?page=1&pageSize=20",
            scenario.Buddy.Token);
        notifications.EnsureSuccessStatusCode();
        using var notificationsJson = await ReadJsonAsync(notifications);
        Assert.Contains(
            notificationsJson.RootElement.GetProperty("items").EnumerateArray(),
            item => item.GetProperty("sourceModule").GetString() == "Buddy" &&
                    item.GetProperty("sourceEntityType").GetString() == "BuddyRequest" &&
                    item.GetProperty("sourceEntityId").GetGuid() == requestId);
    }

    [Fact]
    public async Task Accepted_request_can_only_be_reviewed_after_end_once()
    {
        var scenario = await CreateBuddyScenarioAsync(approved: true);
        var traveler = await CreateUserAsync("Traveler", "buddy-review-owner");
        var otherTraveler = await CreateUserAsync("Traveler", "buddy-review-other");
        await UpsertTravelerAsync(traveler);
        await UpsertTravelerAsync(otherTraveler);
        var created = await CreateRequestAsync(
            traveler, scenario.Buddy.UserId, scenario.AvailabilityId);
        created.EnsureSuccessStatusCode();
        using var createdJson = await ReadJsonAsync(created);
        var requestId = createdJson.RootElement.GetProperty("id").GetGuid();
        (await SendAsync(
            HttpMethod.Patch,
            $"/api/buddy/requests/{requestId}/accept",
            scenario.Buddy.Token)).EnsureSuccessStatusCode();

        var tooEarly = await ReviewAsync(traveler, requestId, 5, "Too early");
        await AssertProblemAsync(tooEarly, 409, "conflict");

        await Factory.ExecuteAsync(
            $"""
             UPDATE buddy_availability
             SET "StartTimeUtc" = NOW() - INTERVAL '2 hours',
                 "EndTimeUtc" = NOW() - INTERVAL '1 hour'
             WHERE "Id" = '{scenario.AvailabilityId}'
             """);

        var wrongTraveler = await ReviewAsync(
            otherTraveler, requestId, 5, "Not my request");
        await AssertProblemAsync(wrongTraveler, 403, "forbidden");

        foreach (var rating in new[] { 0, 6 })
        {
            var invalid = await ReviewAsync(
                traveler, requestId, rating, "Invalid rating");
            await AssertProblemAsync(invalid, 400, "validation_error");
        }

        var review = await ReviewAsync(
            traveler, requestId, 5, "Knowledgeable and friendly.");
        Assert.Equal(HttpStatusCode.Created, review.StatusCode);

        var duplicate = await ReviewAsync(
            traveler, requestId, 4, "Second review");
        await AssertProblemAsync(duplicate, 409, "conflict");

        var publicReviews = await Client.GetAsync(
            $"/api/local-buddies/{scenario.Buddy.UserId}/reviews");
        publicReviews.EnsureSuccessStatusCode();
        using var reviewsJson = await ReadJsonAsync(publicReviews);
        Assert.Contains(
            reviewsJson.RootElement.EnumerateArray(),
            item => item.GetProperty("requestId").GetGuid() == requestId);

        var summary = await Client.GetAsync(
            $"/api/local-buddies/{scenario.Buddy.UserId}/review-summary");
        summary.EnsureSuccessStatusCode();
        using var summaryJson = await ReadJsonAsync(summary);
        Assert.Equal(1, summaryJson.RootElement.GetProperty("reviewsCount").GetInt32());
        Assert.Equal(5, summaryJson.RootElement.GetProperty("averageRating").GetDecimal());
    }

    [Fact]
    public async Task Rejected_request_cannot_be_reviewed_and_pending_can_be_cancelled()
    {
        var rejectedScenario = await CreateBuddyScenarioAsync(approved: true);
        var traveler = await CreateUserAsync("Traveler", "buddy-reject-review");
        await UpsertTravelerAsync(traveler);
        var created = await CreateRequestAsync(
            traveler,
            rejectedScenario.Buddy.UserId,
            rejectedScenario.AvailabilityId);
        using var createdJson = await ReadJsonAsync(created);
        var rejectedId = createdJson.RootElement.GetProperty("id").GetGuid();
        var pendingReview = await ReviewAsync(
            traveler, rejectedId, 5, "Still pending");
        await AssertProblemAsync(pendingReview, 409, "conflict");
        (await SendAsync(
            HttpMethod.Patch,
            $"/api/buddy/requests/{rejectedId}/reject",
            rejectedScenario.Buddy.Token)).EnsureSuccessStatusCode();
        var rejectedReview = await ReviewAsync(
            traveler, rejectedId, 5, "Should fail");
        await AssertProblemAsync(rejectedReview, 409, "conflict");

        var cancelScenario = await CreateBuddyScenarioAsync(approved: true);
        var cancellable = await CreateRequestAsync(
            traveler,
            cancelScenario.Buddy.UserId,
            cancelScenario.AvailabilityId);
        using var cancellableJson = await ReadJsonAsync(cancellable);
        var cancellableId = cancellableJson.RootElement.GetProperty("id").GetGuid();
        var otherTraveler = await CreateUserAsync(
            "Traveler", "buddy-cancel-other");
        await UpsertTravelerAsync(otherTraveler);
        var wrongCancellation = await SendAsync(
            HttpMethod.Patch,
            $"/api/buddy/requests/{cancellableId}/cancel",
            otherTraveler.Token);
        await AssertProblemAsync(wrongCancellation, 403, "forbidden");
        var cancelled = await SendAsync(
            HttpMethod.Patch,
            $"/api/buddy/requests/{cancellableId}/cancel",
            traveler.Token);
        cancelled.EnsureSuccessStatusCode();
        var cancelAgain = await SendAsync(
            HttpMethod.Patch,
            $"/api/buddy/requests/{cancellableId}/cancel",
            traveler.Token);
        await AssertProblemAsync(cancelAgain, 409, "conflict");
    }

    [Fact]
    public async Task Compatibility_routes_continue_to_use_the_same_lifecycle()
    {
        var scenario = await CreateBuddyScenarioAsync(approved: true);
        var traveler = await CreateUserAsync("Traveler", "buddy-compat");
        await UpsertTravelerAsync(traveler);
        var created = await SendAsync(
            HttpMethod.Post,
            $"/api/local-buddies/{scenario.Buddy.UserId}/bookings",
            traveler.Token,
            new { availabilityId = scenario.AvailabilityId });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        using var createdJson = await ReadJsonAsync(created);
        var requestId = createdJson.RootElement.GetProperty("id").GetGuid();

        var mine = await SendAsync(
            HttpMethod.Get, "/api/buddy-bookings/my", traveler.Token);
        mine.EnsureSuccessStatusCode();
        var accepted = await SendAsync(
            HttpMethod.Patch,
            $"/api/buddy-bookings/{requestId}/status",
            scenario.Buddy.Token,
            new { status = "Accepted" });
        accepted.EnsureSuccessStatusCode();
    }

    private async Task<BuddyScenario> CreateBuddyScenarioAsync(bool approved)
    {
        var buddy = await CreateUserAsync("LocalBuddy", "buddy-module");
        var profile = await SendAsync(
            HttpMethod.Put,
            "/api/profiles/local-buddy",
            buddy.Token,
            new
            {
                displayName = $"Buddy {buddy.UserId}",
                city = "Cairo",
                languages = "Arabic, English",
                interestIds = Array.Empty<Guid>()
            });
        profile.EnsureSuccessStatusCode();
        if (approved)
        {
            var approvedResponse = await SendAsync(
                HttpMethod.Patch,
                $"/api/admin/local-buddies/{buddy.UserId}/verification",
                await GetAdminTokenAsync(),
                new { verificationStatus = "Approved" });
            approvedResponse.EnsureSuccessStatusCode();
        }

        var availability = await SendAsync(
            HttpMethod.Post,
            $"/api/local-buddies/{buddy.UserId}/availability",
            buddy.Token,
            new
            {
                startTimeUtc = DateTime.UtcNow.AddDays(10),
                endTimeUtc = DateTime.UtcNow.AddDays(10).AddHours(2),
                price = 50
            });
        availability.EnsureSuccessStatusCode();
        using var json = await ReadJsonAsync(availability);
        return new BuddyScenario(
            buddy,
            json.RootElement.GetProperty("id").GetGuid());
    }

    private Task<HttpResponseMessage> CreateRequestAsync(
        TestUser traveler,
        Guid buddyUserId,
        Guid availabilityId,
        string? notes = null) =>
        SendAsync(
            HttpMethod.Post,
            "/api/buddy/requests",
            traveler.Token,
            new
            {
                localBuddyUserId = buddyUserId,
                availabilityId,
                notes
            });

    private Task<HttpResponseMessage> ReviewAsync(
        TestUser traveler,
        Guid requestId,
        int rating,
        string reviewText) =>
        SendAsync(
            HttpMethod.Post,
            $"/api/buddy/requests/{requestId}/review",
            traveler.Token,
            new { rating, reviewText });

    private sealed record BuddyScenario(TestUser Buddy, Guid AvailabilityId);
}
