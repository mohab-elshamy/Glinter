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
}
