using System.Text;
using System.Text.Json;
using Glinter.IntegrationTests.Infrastructure;

namespace Glinter.IntegrationTests;

[Collection(IntegrationTestCollection.Name)]
public sealed class RegionsAndProfilesTests : ApiTestBase
{
    public RegionsAndProfilesTests(GlinterApiFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Region_writes_require_admin_and_geojson_is_validated()
    {
        var traveler = await CreateUserAsync("Traveler", "region");
        var adminToken = await GetAdminTokenAsync();
        var suffix = Guid.NewGuid().ToString("N")[..10];
        var country = new
        {
            nameEn = $"Secured Country {suffix}",
            pcode = $"S{suffix}"
        };

        var unauthenticated = await SendAsync(
            HttpMethod.Post,
            "/api/regions/countries",
            body: country);
        await AssertProblemAsync(unauthenticated, 401, "authentication_required");

        var forbidden = await SendAsync(
            HttpMethod.Post,
            "/api/regions/countries",
            traveler.Token,
            country);
        await AssertProblemAsync(forbidden, 403, "forbidden");

        var created = await SendAsync(
            HttpMethod.Post,
            "/api/regions/countries",
            adminToken,
            country);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        using var createdJson = await ReadJsonAsync(created);
        var countryGid = createdJson.RootElement.GetProperty("gid").GetInt32();

        var updated = await SendAsync(
            HttpMethod.Put,
            $"/api/regions/countries/{countryGid}",
            adminToken,
            new
            {
                nameEn = $"Updated Secured Country {suffix}",
                nameAr = "Updated",
                imageUrl = "https://example.test/country.jpg",
                flagUrl = "https://example.test/flag.jpg"
            });
        updated.EnsureSuccessStatusCode();

        using var form = new MultipartFormDataContent();
        form.Add(
            new ByteArrayContent(Encoding.UTF8.GetBytes("{ malformed json")),
            "file",
            "regions.geojson");
        using var upload = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/admin/regions/import/adm0");
        upload.Headers.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", adminToken);
        upload.Content = form;

        var malformed = await Client.SendAsync(upload);
        await AssertProblemAsync(malformed, 400, "validation_error");

        var failedImportAudit = await SendAsync(
            HttpMethod.Get,
            "/api/admin/audit-events?action=AdminRegionsImport.ImportAdm0&pageSize=20",
            adminToken);
        failedImportAudit.EnsureSuccessStatusCode();
        using var failedImportAuditJson = await ReadJsonAsync(failedImportAudit);
        Assert.Contains(
            failedImportAuditJson.RootElement.GetProperty("items").EnumerateArray(),
            item =>
                item.GetProperty("path").GetString() ==
                "/api/admin/regions/import/adm0" &&
                item.GetProperty("statusCode").GetInt32() == 400 &&
                !item.GetProperty("succeeded").GetBoolean());

        var audit = await SendAsync(
            HttpMethod.Get,
            "/api/admin/audit-events?action=Regions.CreateCountry&pageSize=20",
            adminToken);
        audit.EnsureSuccessStatusCode();
        using var auditJson = await ReadJsonAsync(audit);
        Assert.Contains(
            auditJson.RootElement.GetProperty("items").EnumerateArray(),
            item =>
                item.GetProperty("path").GetString() == "/api/regions/countries" &&
                item.GetProperty("statusCode").GetInt32() == 201 &&
                item.GetProperty("succeeded").GetBoolean() &&
                item.GetProperty("changes")
                    .GetProperty("after")
                    .GetProperty("pcode")
                    .GetString() == country.pcode);

        var updateAudit = await SendAsync(
            HttpMethod.Get,
            "/api/admin/audit-events?action=Regions.UpdateCountry&pageSize=20",
            adminToken);
        updateAudit.EnsureSuccessStatusCode();
        using var updateAuditJson = await ReadJsonAsync(updateAudit);
        Assert.Contains(
            updateAuditJson.RootElement.GetProperty("items").EnumerateArray(),
            item =>
                item.GetProperty("target").GetString() == $"gid={countryGid}" &&
                item.GetProperty("changes")
                    .GetProperty("before")
                    .GetProperty("nameEn")
                    .GetString() == country.nameEn &&
                item.GetProperty("changes")
                    .GetProperty("after")
                    .GetProperty("nameEn")
                    .GetString() == $"Updated Secured Country {suffix}");
    }

    [Fact]
    public async Task Region_write_is_blocked_when_audit_intent_cannot_be_persisted()
    {
        var adminToken = await GetAdminTokenAsync();
        var suffix = Guid.NewGuid().ToString("N")[..10];
        var pcode = $"A{suffix}";

        await Factory.ExecuteAsync(
            """
            CREATE OR REPLACE FUNCTION reject_admin_audit_test()
            RETURNS trigger
            LANGUAGE plpgsql
            AS $$
            BEGIN
                RAISE EXCEPTION 'forced audit failure';
            END;
            $$;

            CREATE TRIGGER reject_admin_audit_test_trigger
            BEFORE INSERT ON admin_audit_events
            FOR EACH ROW EXECUTE FUNCTION reject_admin_audit_test();
            """);

        try
        {
            var response = await SendAsync(
                HttpMethod.Post,
                "/api/regions/countries",
                adminToken,
                new
                {
                    nameEn = $"Audit Protected Country {suffix}",
                    pcode
                });
            await AssertProblemAsync(response, 500, "database_update_failed");

            var persisted = await Factory.ScalarAsync<long>(
                $"""SELECT count(*) FROM adm0 WHERE pcode = '{pcode}'""");
            Assert.Equal(0, persisted);
        }
        finally
        {
            await Factory.ExecuteAsync(
                """
                DROP TRIGGER IF EXISTS reject_admin_audit_test_trigger
                ON admin_audit_events;
                DROP FUNCTION IF EXISTS reject_admin_audit_test();
                """);
        }
    }

    [Fact]
    public async Task Local_buddy_visibility_follows_verification_status()
    {
        var buddy = await CreateUserAsync("LocalBuddy", "visibility");
        var profile = await SendAsync(
            HttpMethod.Put,
            "/api/profiles/local-buddy",
            buddy.Token,
            new
            {
                displayName = "Pending Integration Buddy",
                city = "Cairo",
                interestIds = Array.Empty<Guid>()
            });
        profile.EnsureSuccessStatusCode();

        var publicPending = await Client.GetAsync($"/api/local-buddies/{buddy.UserId}");
        await AssertProblemAsync(publicPending, 404, "not_found");

        var ownerPending = await SendAsync(
            HttpMethod.Get,
            $"/api/local-buddies/{buddy.UserId}",
            buddy.Token);
        Assert.Equal(HttpStatusCode.OK, ownerPending.StatusCode);

        var adminToken = await GetAdminTokenAsync();
        var adminPending = await SendAsync(
            HttpMethod.Get,
            $"/api/local-buddies/{buddy.UserId}",
            adminToken);
        Assert.Equal(HttpStatusCode.OK, adminPending.StatusCode);

        var approve = await SendAsync(
            HttpMethod.Patch,
            $"/api/admin/local-buddies/{buddy.UserId}/verification",
            adminToken,
            new { verificationStatus = "Approved" });
        approve.EnsureSuccessStatusCode();

        var history = await SendAsync(
            HttpMethod.Get,
            $"/api/admin/local-buddies/{buddy.UserId}/verification-history",
            adminToken);
        history.EnsureSuccessStatusCode();
        using var historyJson = await ReadJsonAsync(history);
        var verificationEvent = Assert.Single(historyJson.RootElement.EnumerateArray());
        Assert.Equal("Pending", verificationEvent.GetProperty("previousStatus").GetString());
        Assert.Equal("Approved", verificationEvent.GetProperty("newStatus").GetString());

        var publicApproved = await Client.GetAsync($"/api/local-buddies/{buddy.UserId}");
        Assert.Equal(HttpStatusCode.OK, publicApproved.StatusCode);

        var list = await Client.GetAsync("/api/local-buddies?page=1&pageSize=50");
        list.EnsureSuccessStatusCode();
        using var listJson = await ReadJsonAsync(list);
        Assert.Contains(
            listJson.RootElement.GetProperty("items").EnumerateArray(),
            item => item.GetProperty("userId").GetGuid() == buddy.UserId);
    }

    [Fact]
    public async Task Follow_is_race_idempotent_and_profile_image_requires_http()
    {
        var follower = await CreateUserAsync("Traveler", "race-follower");
        var target = await CreateUserAsync("Traveler", "race-target");
        await UpsertTravelerAsync(target);

        var requests = Enumerable.Range(0, 20)
            .Select(_ => SendAsync(
                HttpMethod.Post,
                $"/api/profiles/users/{target.UserId}/follow",
                follower.Token))
            .ToArray();

        var responses = await Task.WhenAll(requests);
        Assert.All(responses, response => Assert.Equal(HttpStatusCode.OK, response.StatusCode));

        var rowCount = await Factory.ScalarAsync<long>(
            $"""
             SELECT count(*)
             FROM user_follows
             WHERE "FollowerUserId" = '{follower.UserId}'
               AND "FollowedUserId" = '{target.UserId}'
             """);
        Assert.Equal(1, rowCount);

        var invalidImage = await SendAsync(
            HttpMethod.Patch,
            "/api/profiles/image",
            target.Token,
            new { profileImageUrl = "ftp://example.test/profile.jpg" });
        await AssertProblemAsync(invalidImage, 400, "validation_error");

        var validImage = await SendAsync(
            HttpMethod.Patch,
            "/api/profiles/image",
            target.Token,
            new { profileImageUrl = "https://example.test/profile.jpg" });
        Assert.Equal(HttpStatusCode.OK, validImage.StatusCode);
    }

    [Fact]
    public async Task Profile_image_upload_and_experience_favorites_are_persisted()
    {
        var traveler = await CreateUserAsync("Traveler", "profile-media-favorites");
        await UpsertTravelerAsync(traveler);

        var pngBytes = new byte[]
        {
            0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A,
            0x00, 0x00, 0x00, 0x00
        };
        using var content = new MultipartFormDataContent();
        var imageContent = new ByteArrayContent(pngBytes);
        imageContent.Headers.ContentType =
            new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");
        content.Add(imageContent, "file", "profile.png");
        using var uploadRequest = new HttpRequestMessage(HttpMethod.Post, "/api/profiles/images");
        uploadRequest.Headers.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", traveler.Token);
        uploadRequest.Content = content;

        var upload = await Client.SendAsync(uploadRequest);
        upload.EnsureSuccessStatusCode();
        using var uploadJson = await ReadJsonAsync(upload);
        var imageLink = uploadJson.RootElement.GetProperty("link").GetString();
        Assert.False(string.IsNullOrWhiteSpace(imageLink));
        var image = await Client.GetAsync(imageLink);
        image.EnsureSuccessStatusCode();
        Assert.Equal(pngBytes, await image.Content.ReadAsByteArrayAsync());

        var saveImage = await SendAsync(
            HttpMethod.Patch,
            "/api/profiles/image",
            traveler.Token,
            new { profileImageUrl = imageLink });
        saveImage.EnsureSuccessStatusCode();

        var addFavorite = await SendAsync(
            HttpMethod.Put,
            "/api/profiles/experience-favorites/42",
            traveler.Token);
        addFavorite.EnsureSuccessStatusCode();

        var favorites = await SendAsync(
            HttpMethod.Get,
            "/api/profiles/experience-favorites",
            traveler.Token);
        favorites.EnsureSuccessStatusCode();
        using var favoritesJson = await ReadJsonAsync(favorites);
        Assert.Contains(
            favoritesJson.RootElement.EnumerateArray(),
            item => item.GetInt32() == 42);

        var removeFavorite = await SendAsync(
            HttpMethod.Delete,
            "/api/profiles/experience-favorites/42",
            traveler.Token);
        removeFavorite.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Buddy_availability_request_completion_and_review_flow_is_enforced()
    {
        var buddy = await CreateUserAsync("LocalBuddy", "buddy-flow");
        var traveler = await CreateUserAsync("Traveler", "buddy-traveler");
        await UpsertTravelerAsync(traveler);

        var buddyProfile = await SendAsync(
            HttpMethod.Put,
            "/api/profiles/local-buddy",
            buddy.Token,
            new
            {
                displayName = "Integration Guide",
                city = "Cairo",
                languages = "Arabic, English",
                interestIds = Array.Empty<Guid>()
            });
        buddyProfile.EnsureSuccessStatusCode();

        var adminToken = await GetAdminTokenAsync();
        var approved = await SendAsync(
            HttpMethod.Patch,
            $"/api/admin/local-buddies/{buddy.UserId}/verification",
            adminToken,
            new { verificationStatus = "Approved" });
        approved.EnsureSuccessStatusCode();

        var start = DateTime.UtcNow.AddDays(7);
        var end = start.AddHours(4);
        var availability = await SendAsync(
            HttpMethod.Post,
            $"/api/local-buddies/{buddy.UserId}/availability",
            buddy.Token,
            new { startTimeUtc = start, endTimeUtc = end, price = 75 });
        Assert.Equal(HttpStatusCode.Created, availability.StatusCode);
        using var availabilityJson = await ReadJsonAsync(availability);
        var availabilityId = availabilityJson.RootElement.GetProperty("id").GetGuid();

        var booking = await SendAsync(
            HttpMethod.Post,
            $"/api/local-buddies/{buddy.UserId}/bookings",
            traveler.Token,
            new { availabilityId, notes = "Museum and downtown walk" });
        Assert.Equal(HttpStatusCode.Created, booking.StatusCode);
        using var bookingJson = await ReadJsonAsync(booking);
        var bookingId = bookingJson.RootElement.GetProperty("id").GetGuid();
        Assert.Equal("Pending", bookingJson.RootElement.GetProperty("status").GetString());
        Assert.Equal(75, bookingJson.RootElement.GetProperty("totalPrice").GetDecimal());
        var buddyNotifications = await SendAsync(
            HttpMethod.Get,
            "/api/notifications?page=1&pageSize=20",
            buddy.Token);
        buddyNotifications.EnsureSuccessStatusCode();
        using var buddyNotificationsJson = await ReadJsonAsync(buddyNotifications);
        Assert.Contains(
            buddyNotificationsJson.RootElement.GetProperty("items").EnumerateArray(),
            item => item.GetProperty("type").GetString() == "Booking" &&
                    item.GetProperty("sourceEntityId").GetGuid() == bookingId);

        var duplicate = await SendAsync(
            HttpMethod.Post,
            $"/api/local-buddies/{buddy.UserId}/bookings",
            traveler.Token,
            new { availabilityId });
        await AssertProblemAsync(duplicate, 400, "validation_error");

        var accepted = await SendAsync(
            HttpMethod.Patch,
            $"/api/buddy-bookings/{bookingId}/status",
            buddy.Token,
            new { status = "Accepted" });
        accepted.EnsureSuccessStatusCode();

        var completed = await SendAsync(
            HttpMethod.Patch,
            $"/api/buddy-bookings/{bookingId}/status",
            buddy.Token,
            new { status = "Completed" });
        completed.EnsureSuccessStatusCode();

        var review = await SendAsync(
            HttpMethod.Post,
            $"/api/local-buddies/{buddy.UserId}/reviews",
            traveler.Token,
            new { bookingId, rating = 5, reviewText = "Knowledgeable and friendly guide." });
        Assert.Equal(HttpStatusCode.Created, review.StatusCode);

        var duplicateReview = await SendAsync(
            HttpMethod.Post,
            $"/api/local-buddies/{buddy.UserId}/reviews",
            traveler.Token,
            new { bookingId, rating = 4, reviewText = "Duplicate review." });
        await AssertProblemAsync(duplicateReview, 400, "validation_error");

        var publicProfile = await Client.GetAsync($"/api/local-buddies/{buddy.UserId}");
        publicProfile.EnsureSuccessStatusCode();
        using var profileJson = await ReadJsonAsync(publicProfile);
        Assert.Equal(1, profileJson.RootElement.GetProperty("reviewsCount").GetInt32());
        Assert.Equal(5, profileJson.RootElement.GetProperty("rating").GetDecimal());

        var follow = await SendAsync(
            HttpMethod.Post,
            $"/api/profiles/users/{buddy.UserId}/follow",
            traveler.Token);
        follow.EnsureSuccessStatusCode();
        using var followJson = await ReadJsonAsync(follow);
        Assert.True(followJson.RootElement.GetProperty("isFollowing").GetBoolean());
        Assert.Equal(1, followJson.RootElement.GetProperty("followersCount").GetInt32());

        var list = await SendAsync(
            HttpMethod.Get,
            "/api/local-buddies?page=1&pageSize=50",
            traveler.Token);
        list.EnsureSuccessStatusCode();
        using var listJson = await ReadJsonAsync(list);
        var listedBuddy = listJson.RootElement
            .GetProperty("items")
            .EnumerateArray()
            .Single(item => item.GetProperty("userId").GetGuid() == buddy.UserId);
        Assert.True(listedBuddy.GetProperty("isFollowing").GetBoolean());
        Assert.Equal(1, listedBuddy.GetProperty("followersCount").GetInt32());
    }
}
