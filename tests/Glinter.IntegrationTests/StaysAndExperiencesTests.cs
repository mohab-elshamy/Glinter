using Glinter.IntegrationTests.Infrastructure;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace Glinter.IntegrationTests;

[Collection(IntegrationTestCollection.Name)]
public sealed class StaysAndExperiencesTests : ApiTestBase
{
    public StaysAndExperiencesTests(GlinterApiFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Hotel_owner_can_create_and_publicly_search_a_stay()
    {
        var owner = await CreateUserAsync("HotelOwner", "stay-owner");
        await UpsertHotelOwnerAsync(owner);

        var name = $"Integration Stay {Guid.NewGuid():N}";
        var create = await SendAsync(
            HttpMethod.Post,
            "/api/stays",
            owner.Token,
            new
            {
                name,
                price = 1000,
                description = "Integration stay",
                latitude = 30.05,
                longitude = 31.25,
                imageLinks = new[] { "https://example.test/stay.jpg" },
                amenities = new[] { "Wi-Fi" },
                bookingPlatforms = new[]
                {
                    new
                    {
                        name = "Glinter",
                        priceWithTax = 1150,
                        link = "https://example.test/book"
                    }
                }
            });

        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        using var createdJson = await ReadJsonAsync(create);
        var stayId = createdJson.RootElement.GetProperty("id").GetInt32();
        Assert.Equal(owner.UserId, createdJson.RootElement.GetProperty("createdByUserId").GetGuid());
        Assert.NotEqual(
            Guid.Empty,
            createdJson.RootElement.GetProperty("hotelOwnerProfileId").GetGuid());

        var details = await Client.GetAsync($"/api/stays/{stayId}");
        details.EnsureSuccessStatusCode();

        var search = await Client.GetAsync(
            $"/api/stays?search={Uri.EscapeDataString(name)}&page=1&pageSize=10");
        search.EnsureSuccessStatusCode();
        using var searchJson = await ReadJsonAsync(search);
        Assert.Contains(
            searchJson.RootElement.GetProperty("items").EnumerateArray(),
            item => item.GetProperty("id").GetInt32() == stayId);

        var filtered = await Client.GetAsync(
            $"/api/stays?search={Uri.EscapeDataString(name)}&minPrice=999&maxPrice=1001&sortBy=Price&sortDirection=Asc&page=1&pageSize=1");
        filtered.EnsureSuccessStatusCode();
        using var filteredJson = await ReadJsonAsync(filtered);
        Assert.Equal(1, filteredJson.RootElement.GetProperty("pageSize").GetInt32());
        Assert.Equal(stayId, filteredJson.RootElement.GetProperty("items")[0].GetProperty("id").GetInt32());

        var stats = await Client.GetAsync("/api/stays/region-stats?groupBy=Adm0");
        stats.EnsureSuccessStatusCode();
        using var statsJson = await ReadJsonAsync(stats);
        Assert.Equal(JsonValueKind.Array, statsJson.RootElement.ValueKind);
    }

    [Fact]
    public async Task Stay_image_upload_validates_content_and_serves_the_stored_image()
    {
        var owner = await CreateUserAsync("HotelOwner", "stay-image-owner");
        var traveler = await CreateUserAsync("Traveler", "stay-image-traveler");
        await UpsertHotelOwnerAsync(owner);

        var pngBytes = new byte[]
        {
            0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A,
            0x00, 0x00, 0x00, 0x00
        };
        using var content = new MultipartFormDataContent();
        var imageContent = new ByteArrayContent(pngBytes);
        imageContent.Headers.ContentType =
            new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");
        content.Add(imageContent, "file", "stay.png");
        using var uploadRequest = new HttpRequestMessage(HttpMethod.Post, "/api/stays/images");
        uploadRequest.Headers.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", owner.Token);
        uploadRequest.Content = content;

        var upload = await Client.SendAsync(uploadRequest);
        upload.EnsureSuccessStatusCode();
        using var uploadJson = await ReadJsonAsync(upload);
        var link = uploadJson.RootElement.GetProperty("link").GetString();
        Assert.False(string.IsNullOrWhiteSpace(link));

        var image = await Client.GetAsync(link);
        image.EnsureSuccessStatusCode();
        Assert.Equal("image/png", image.Content.Headers.ContentType?.MediaType);
        Assert.Equal(pngBytes, await image.Content.ReadAsByteArrayAsync());

        using var invalidContent = new MultipartFormDataContent();
        var fakeImage = new ByteArrayContent("not an image"u8.ToArray());
        fakeImage.Headers.ContentType =
            new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");
        invalidContent.Add(fakeImage, "file", "fake.png");
        using var invalidRequest = new HttpRequestMessage(HttpMethod.Post, "/api/stays/images");
        invalidRequest.Headers.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", owner.Token);
        invalidRequest.Content = invalidContent;
        await AssertProblemAsync(await Client.SendAsync(invalidRequest), 400, "validation_error");

        using var forbiddenContent = new MultipartFormDataContent();
        var travelerImage = new ByteArrayContent(pngBytes);
        travelerImage.Headers.ContentType =
            new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");
        forbiddenContent.Add(travelerImage, "file", "stay.png");
        using var forbiddenRequest = new HttpRequestMessage(HttpMethod.Post, "/api/stays/images");
        forbiddenRequest.Headers.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", traveler.Token);
        forbiddenRequest.Content = forbiddenContent;
        await AssertProblemAsync(await Client.SendAsync(forbiddenRequest), 403, "forbidden");
    }

    [Fact]
    public async Task Experience_provider_can_create_and_publicly_search_an_experience()
    {
        var provider = await CreateUserAsync(
            "ExperienceProvider",
            "experience-provider");
        await UpsertProviderAsync(provider);

        var name = $"Integration Experience {Guid.NewGuid():N}";
        var create = await SendAsync(
            HttpMethod.Post,
            "/api/experiences",
            provider.Token,
            new
            {
                category = "Historical",
                name,
                description = "Integration experience",
                address = "Integration neighbourhood",
                latitude = 30.05,
                longitude = 31.25,
                featuredImageLinks = new[] { "https://example.test/experience.jpg" },
                hours = new[]
                {
                    new
                    {
                        dayOfWeek = DateTime.Now.DayOfWeek.ToString(),
                        opensAt = "00:00:00",
                        closesAt = "23:59:59"
                    }
                },
                popularTimes = new[]
                {
                    new
                    {
                        dayOfWeek = DateTime.Now.DayOfWeek.ToString(),
                        hourOfDay = DateTime.Now.Hour,
                        popularityPercentage = 42
                    }
                },
                amenities = new[] { "Guided tour" }
            });

        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        using var createdJson = await ReadJsonAsync(create);
        var experienceId = createdJson.RootElement.GetProperty("id").GetInt32();
        Assert.Equal(
            provider.UserId,
            createdJson.RootElement.GetProperty("createdByUserId").GetGuid());
        Assert.NotEqual(
            Guid.Empty,
            createdJson.RootElement.GetProperty("providerProfileId").GetGuid());

        var adminToken = await GetAdminTokenAsync();
        var approve = await SendAsync(
            HttpMethod.Patch,
            $"/api/admin/experiences/{experienceId}/moderation",
            adminToken,
            new { moderationStatus = "Approved" });
        approve.EnsureSuccessStatusCode();

        var details = await Client.GetAsync($"/api/experiences/{experienceId}");
        details.EnsureSuccessStatusCode();

        var search = await Client.GetAsync(
            $"/api/experiences?search={Uri.EscapeDataString(name)}&category=Historical&page=1&pageSize=10");
        search.EnsureSuccessStatusCode();
        using var searchJson = await ReadJsonAsync(search);
        Assert.Contains(
            searchJson.RootElement.GetProperty("items").EnumerateArray(),
            item => item.GetProperty("id").GetInt32() == experienceId);

        var paged = await Client.GetAsync(
            $"/api/experiences?search={Uri.EscapeDataString(name)}&page=1&pageSize=1");
        paged.EnsureSuccessStatusCode();
        using var pagedJson = await ReadJsonAsync(paged);
        Assert.Equal(1, pagedJson.RootElement.GetProperty("pageSize").GetInt32());
        Assert.Equal(experienceId, pagedJson.RootElement.GetProperty("items")[0].GetProperty("id").GetInt32());

        var map = await Client.GetAsync(
            $"/api/experiences/map?search={Uri.EscapeDataString(name)}");
        map.EnsureSuccessStatusCode();
        using var mapJson = await ReadJsonAsync(map);
        Assert.Contains(
            mapJson.RootElement.EnumerateArray(),
            item => item.GetProperty("id").GetInt32() == experienceId &&
                    item.GetProperty("latitude").GetDouble() == 30.05);

        var insight = await Client.GetAsync($"/api/experiences/{experienceId}/visit-insights");
        insight.EnsureSuccessStatusCode();
        using var insightJson = await ReadJsonAsync(insight);
        Assert.Equal("open", insightJson.RootElement.GetProperty("openStatus").GetString());
        Assert.Equal(42, insightJson.RootElement.GetProperty("popularityPercentage").GetInt32());

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            (await Client.GetAsync($"/api/experiences/{experienceId}/reviews/llm-input")).StatusCode);
        (await SendAsync(
            HttpMethod.Get,
            $"/api/experiences/{experienceId}/reviews/llm-input",
            adminToken)).EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Experience_image_upload_validates_content_and_serves_the_stored_image()
    {
        var provider = await CreateUserAsync("ExperienceProvider", "experience-image-provider");
        var traveler = await CreateUserAsync("Traveler", "experience-image-traveler");
        await UpsertProviderAsync(provider);

        var pngBytes = new byte[]
        {
            0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A,
            0x00, 0x00, 0x00, 0x00
        };
        using var content = new MultipartFormDataContent();
        var imageContent = new ByteArrayContent(pngBytes);
        imageContent.Headers.ContentType =
            new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");
        content.Add(imageContent, "file", "experience.png");
        using var uploadRequest = new HttpRequestMessage(HttpMethod.Post, "/api/experiences/images");
        uploadRequest.Headers.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", provider.Token);
        uploadRequest.Content = content;

        var upload = await Client.SendAsync(uploadRequest);
        upload.EnsureSuccessStatusCode();
        using var uploadJson = await ReadJsonAsync(upload);
        var link = uploadJson.RootElement.GetProperty("link").GetString();
        Assert.False(string.IsNullOrWhiteSpace(link));

        var image = await Client.GetAsync(link);
        image.EnsureSuccessStatusCode();
        Assert.Equal("image/png", image.Content.Headers.ContentType?.MediaType);
        Assert.Equal(pngBytes, await image.Content.ReadAsByteArrayAsync());

        using var invalidContent = new MultipartFormDataContent();
        var fakeImage = new ByteArrayContent("not an image"u8.ToArray());
        fakeImage.Headers.ContentType =
            new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");
        invalidContent.Add(fakeImage, "file", "fake.png");
        using var invalidRequest = new HttpRequestMessage(HttpMethod.Post, "/api/experiences/images");
        invalidRequest.Headers.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", provider.Token);
        invalidRequest.Content = invalidContent;
        await AssertProblemAsync(await Client.SendAsync(invalidRequest), 400, "validation_error");

        using var forbiddenContent = new MultipartFormDataContent();
        var travelerImage = new ByteArrayContent(pngBytes);
        travelerImage.Headers.ContentType =
            new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");
        forbiddenContent.Add(travelerImage, "file", "experience.png");
        using var forbiddenRequest = new HttpRequestMessage(HttpMethod.Post, "/api/experiences/images");
        forbiddenRequest.Headers.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", traveler.Token);
        forbiddenRequest.Content = forbiddenContent;
        await AssertProblemAsync(await Client.SendAsync(forbiddenRequest), 403, "forbidden");
    }

    [Fact]
    public async Task Experience_import_skips_duplicate_cids_and_continues()
    {
        var adminToken = await GetAdminTokenAsync();
        var suffix = Guid.NewGuid().ToString("N");
        var duplicateCid = $"experience-import-duplicate-{suffix}";
        var newCid = $"experience-import-new-{suffix}";

        var firstImport = await ImportExperiencesJsonAsync(
            adminToken,
            "Historical",
            $$"""
            [
              {
                "name": "Imported Duplicate First {{suffix}}",
                "cid": "{{duplicateCid}}",
                "price_range": "$$",
                "coordinates": { "latitude": 30.05, "longitude": 31.25 }
              },
              {
                "name": "Imported Duplicate Second {{suffix}}",
                "cid": "{{duplicateCid}}",
                "coordinates": { "latitude": 30.06, "longitude": 31.26 }
              }
            ]
            """);

        firstImport.EnsureSuccessStatusCode();
        using (var firstJson = await ReadJsonAsync(firstImport))
        {
            Assert.Equal(1, firstJson.RootElement.GetProperty("created").GetInt32());
            Assert.Equal(0, firstJson.RootElement.GetProperty("updated").GetInt32());
            Assert.Equal(1, firstJson.RootElement.GetProperty("skipped").GetInt32());
        }

        var secondImport = await ImportExperiencesJsonAsync(
            adminToken,
            "Historical",
            $$"""
            [
              {
                "name": "Imported Duplicate Existing {{suffix}}",
                "cid": "{{duplicateCid}}",
                "price_range": "$20-$40",
                "coordinates": { "latitude": 30.07, "longitude": 31.27 }
              },
              {
                "name": "Imported New {{suffix}}",
                "cid": "{{newCid}}",
                "coordinates": { "latitude": 30.08, "longitude": 31.28 }
              }
            ]
            """);

        secondImport.EnsureSuccessStatusCode();
        using var secondJson = await ReadJsonAsync(secondImport);
        Assert.Equal(1, secondJson.RootElement.GetProperty("created").GetInt32());
        Assert.Equal(1, secondJson.RootElement.GetProperty("updated").GetInt32());
        Assert.Equal(0, secondJson.RootElement.GetProperty("skipped").GetInt32());
    }

    [Fact]
    public async Task Stay_owner_and_traveler_can_manage_the_full_stay_lifecycle()
    {
        var owner = await CreateUserAsync("HotelOwner", "stay-lifecycle-owner");
        var traveler = await CreateUserAsync("Traveler", "stay-lifecycle-traveler");
        await UpsertHotelOwnerAsync(owner);
        await UpsertTravelerAsync(traveler);

        var create = await SendAsync(
            HttpMethod.Post,
            "/api/stays",
            owner.Token,
            new
            {
                name = "Lifecycle Stay",
                price = 750,
                description = "Original",
                latitude = 30.05,
                longitude = 31.25,
                imageLinks = new[] { "https://example.test/original.jpg" },
                amenities = new[] { "Wi-Fi" },
                bookingPlatforms = Array.Empty<object>()
            });
        create.EnsureSuccessStatusCode();
        using var createJson = await ReadJsonAsync(create);
        var stayId = createJson.RootElement.GetProperty("id").GetInt32();

        var update = await SendAsync(
            HttpMethod.Put,
            $"/api/stays/{stayId}",
            owner.Token,
            new
            {
                name = "Lifecycle Stay Updated",
                price = 900,
                description = "Updated",
                latitude = 30.06,
                longitude = 31.26,
                imageLinks = new[] { "https://example.test/updated.jpg" },
                amenities = new[] { "Pool", "Breakfast" },
                bookingPlatforms = Array.Empty<object>()
            });
        update.EnsureSuccessStatusCode();
        using var updateJson = await ReadJsonAsync(update);
        Assert.Equal("https://example.test/updated.jpg",
            updateJson.RootElement.GetProperty("images")[0].GetProperty("link").GetString());
        Assert.Equal(2, updateJson.RootElement.GetProperty("amenities").GetArrayLength());

        var deactivate = await SendAsync(
            HttpMethod.Patch,
            $"/api/stays/{stayId}/deactivate",
            owner.Token);
        deactivate.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.NotFound, (await Client.GetAsync($"/api/stays/{stayId}")).StatusCode);

        var mine = await SendAsync(HttpMethod.Get, "/api/stays/mine", owner.Token);
        mine.EnsureSuccessStatusCode();
        using var mineJson = await ReadJsonAsync(mine);
        Assert.Contains(
            mineJson.RootElement.EnumerateArray(),
            item => item.GetProperty("id").GetInt32() == stayId &&
                    !item.GetProperty("isActive").GetBoolean());

        (await SendAsync(HttpMethod.Patch, $"/api/stays/{stayId}/activate", owner.Token))
            .EnsureSuccessStatusCode();

        var checkIn = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7));
        var checkOut = checkIn.AddDays(3);
        var booking = await SendAsync(
            HttpMethod.Post,
            $"/api/stays/{stayId}/bookings",
            traveler.Token,
            new
            {
                checkInDate = checkIn.ToString("yyyy-MM-dd"),
                checkOutDate = checkOut.ToString("yyyy-MM-dd"),
                guestCount = 2
            });
        Assert.Equal(HttpStatusCode.Created, booking.StatusCode);
        using var bookingJson = await ReadJsonAsync(booking);
        var bookingId = bookingJson.RootElement.GetProperty("id").GetGuid();
        Assert.Equal(2700, bookingJson.RootElement.GetProperty("totalPrice").GetDecimal());
        var ownerNotifications = await SendAsync(
            HttpMethod.Get,
            "/api/notifications?page=1&pageSize=20",
            owner.Token);
        ownerNotifications.EnsureSuccessStatusCode();
        using var ownerNotificationsJson = await ReadJsonAsync(ownerNotifications);
        Assert.Contains(
            ownerNotificationsJson.RootElement.GetProperty("items").EnumerateArray(),
            item => item.GetProperty("type").GetString() == "Booking" &&
                    item.GetProperty("sourceEntityId").GetGuid() == bookingId);

        var ownerBookings = await SendAsync(
            HttpMethod.Get,
            $"/api/stays/{stayId}/bookings",
            owner.Token);
        ownerBookings.EnsureSuccessStatusCode();

        var confirm = await SendAsync(
            HttpMethod.Patch,
            $"/api/stay-bookings/{bookingId}/status",
            owner.Token,
            new { status = "Confirmed" });
        confirm.EnsureSuccessStatusCode();

        var myBookings = await SendAsync(
            HttpMethod.Get,
            "/api/stay-bookings/my",
            traveler.Token);
        myBookings.EnsureSuccessStatusCode();
        using var myBookingsJson = await ReadJsonAsync(myBookings);
        Assert.Equal("Confirmed", myBookingsJson.RootElement[0].GetProperty("status").GetString());

        var earlyReview = await SendAsync(
            HttpMethod.Post,
            $"/api/stays/{stayId}/reviews",
            traveler.Token,
            new { rating = 5, reviewText = "Excellent integration stay." });
        Assert.Equal(HttpStatusCode.BadRequest, earlyReview.StatusCode);

        await Factory.ExecuteAsync(
            $"""
             UPDATE stays.stay_bookings
             SET "CheckInDate" = CURRENT_DATE - 3,
                 "CheckOutDate" = CURRENT_DATE - 1
             WHERE "Id" = '{bookingId}'
             """);
        (await SendAsync(
            HttpMethod.Patch,
            $"/api/stay-bookings/{bookingId}/status",
            owner.Token,
            new { status = "Completed" })).EnsureSuccessStatusCode();

        var review = await SendAsync(
            HttpMethod.Post,
            $"/api/stays/{stayId}/reviews",
            traveler.Token,
            new { rating = 5, reviewText = "Excellent integration stay." });
        review.EnsureSuccessStatusCode();
        Assert.Equal(
            HttpStatusCode.BadRequest,
            (await SendAsync(
                HttpMethod.Post,
                $"/api/stays/{stayId}/reviews",
                traveler.Token,
                new { rating = 4, reviewText = "Duplicate review." })).StatusCode);

        var reviews = await Client.GetAsync($"/api/stays/{stayId}/reviews");
        reviews.EnsureSuccessStatusCode();
        using var reviewsJson = await ReadJsonAsync(reviews);
        Assert.Contains(
            reviewsJson.RootElement.EnumerateArray(),
            item => item.GetProperty("reviewText").GetString() == "Excellent integration stay.");
    }

    [Fact]
    public async Task Experience_provider_and_traveler_can_manage_the_full_experience_lifecycle()
    {
        var provider = await CreateUserAsync("ExperienceProvider", "experience-lifecycle-provider");
        var traveler = await CreateUserAsync("Traveler", "experience-lifecycle-traveler");
        await UpsertProviderAsync(provider);
        await UpsertTravelerAsync(traveler);

        var create = await SendAsync(
            HttpMethod.Post,
            "/api/experiences",
            provider.Token,
            new
            {
                category = "Historical",
                name = "Lifecycle Experience",
                description = "Original",
                address = "Cairo",
                latitude = 30.05,
                longitude = 31.25,
                featuredImageLinks = new[] { "https://example.test/original-experience.jpg" },
                hours = Array.Empty<object>(),
                popularTimes = Array.Empty<object>(),
                priceRange = "$20-$40",
                amenities = new[] { "Guide" }
            });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        using var createJson = await ReadJsonAsync(create);
        var experienceId = createJson.RootElement.GetProperty("id").GetInt32();
        Assert.Equal(20, createJson.RootElement.GetProperty("priceRangeMin").GetInt32());
        Assert.Equal(40, createJson.RootElement.GetProperty("priceRangeMax").GetInt32());
        Assert.Equal("$20-$40", createJson.RootElement.GetProperty("priceRange").GetString());

        var adminToken = await GetAdminTokenAsync();
        (await SendAsync(
            HttpMethod.Patch,
            $"/api/admin/experiences/{experienceId}/moderation",
            adminToken,
            new { moderationStatus = "Approved" })).EnsureSuccessStatusCode();

        var update = await SendAsync(
            HttpMethod.Put,
            $"/api/experiences/{experienceId}",
            provider.Token,
            new
            {
                category = "Nature",
                name = "Lifecycle Experience Updated",
                description = "Updated",
                address = "Giza",
                latitude = 30.06,
                longitude = 31.26,
                featuredImageLinks = new[] { "https://example.test/updated-experience.jpg" },
                hours = Array.Empty<object>(),
                popularTimes = Array.Empty<object>(),
                priceRange = "$30-$50",
                amenities = new[] { "Guide", "Transport" }
            });
        update.EnsureSuccessStatusCode();
        using var updateJson = await ReadJsonAsync(update);
        Assert.Equal("Nature", updateJson.RootElement.GetProperty("category").GetString());
        Assert.Equal(30, updateJson.RootElement.GetProperty("priceRangeMin").GetInt32());
        Assert.Equal(50, updateJson.RootElement.GetProperty("priceRangeMax").GetInt32());
        Assert.Equal("$30-$50", updateJson.RootElement.GetProperty("priceRange").GetString());
        Assert.Equal(
            "https://example.test/updated-experience.jpg",
            updateJson.RootElement.GetProperty("featuredImages")[0].GetProperty("link").GetString());

        (await SendAsync(
            HttpMethod.Patch,
            $"/api/experiences/{experienceId}/deactivate",
            provider.Token)).EnsureSuccessStatusCode();
        Assert.Equal(
            HttpStatusCode.NotFound,
            (await Client.GetAsync($"/api/experiences/{experienceId}")).StatusCode);

        var mine = await SendAsync(HttpMethod.Get, "/api/experiences/mine", provider.Token);
        mine.EnsureSuccessStatusCode();
        using var mineJson = await ReadJsonAsync(mine);
        Assert.Contains(
            mineJson.RootElement.EnumerateArray(),
            item => item.GetProperty("id").GetInt32() == experienceId &&
                    !item.GetProperty("isActive").GetBoolean());

        (await SendAsync(
            HttpMethod.Patch,
            $"/api/experiences/{experienceId}/activate",
            provider.Token)).EnsureSuccessStatusCode();

        var startsAt = DateTime.UtcNow.AddDays(7);
        var endsAt = startsAt.AddHours(3);
        var availability = await SendAsync(
            HttpMethod.Post,
            $"/api/experiences/{experienceId}/availability",
            provider.Token,
            new
            {
                startTimeUtc = startsAt,
                endTimeUtc = endsAt,
                capacity = 6,
                pricePerPerson = 40
            });
        Assert.Equal(HttpStatusCode.Created, availability.StatusCode);
        using var availabilityJson = await ReadJsonAsync(availability);
        var availabilityId = availabilityJson.RootElement.GetProperty("id").GetGuid();
        Assert.Equal(6, availabilityJson.RootElement.GetProperty("remainingCapacity").GetInt32());

        var pricedDetail = await Client.GetAsync($"/api/experiences/{experienceId}");
        pricedDetail.EnsureSuccessStatusCode();
        using var pricedDetailJson = await ReadJsonAsync(pricedDetail);
        Assert.Equal(
            40,
            pricedDetailJson.RootElement.GetProperty("startingPricePerPerson").GetDecimal());

        var pricedMap = await Client.GetAsync(
            $"/api/experiences/map?search={Uri.EscapeDataString("Lifecycle Experience Updated")}");
        pricedMap.EnsureSuccessStatusCode();
        using var pricedMapJson = await ReadJsonAsync(pricedMap);
        Assert.Contains(
            pricedMapJson.RootElement.EnumerateArray(),
            item => item.GetProperty("id").GetInt32() == experienceId &&
                    item.GetProperty("startingPricePerPerson").GetDecimal() == 40);

        var budgetCreate = await SendAsync(
            HttpMethod.Post,
            "/api/experiences",
            provider.Token,
            new
            {
                category = "Nature",
                name = "Lifecycle Experience Budget",
                description = "Lower priced option",
                address = "Alexandria",
                latitude = 31.2,
                longitude = 29.9,
                featuredImageLinks = Array.Empty<string>(),
                hours = Array.Empty<object>(),
                popularTimes = Array.Empty<object>(),
                priceRange = "$10",
                amenities = Array.Empty<string>()
            });
        budgetCreate.EnsureSuccessStatusCode();
        using var budgetCreateJson = await ReadJsonAsync(budgetCreate);
        var budgetExperienceId = budgetCreateJson.RootElement.GetProperty("id").GetInt32();
        (await SendAsync(
            HttpMethod.Patch,
            $"/api/admin/experiences/{budgetExperienceId}/moderation",
            adminToken,
            new { moderationStatus = "Approved" })).EnsureSuccessStatusCode();
        (await SendAsync(
            HttpMethod.Post,
            $"/api/experiences/{budgetExperienceId}/availability",
            provider.Token,
            new
            {
                startTimeUtc = startsAt.AddDays(1),
                endTimeUtc = endsAt.AddDays(1),
                capacity = 4,
                pricePerPerson = 10
            })).EnsureSuccessStatusCode();

        var priceSorted = await Client.GetAsync(
            "/api/experiences?search=Lifecycle%20Experience&sortBy=Price&sortDirection=Asc&pageSize=10");
        priceSorted.EnsureSuccessStatusCode();
        using var priceSortedJson = await ReadJsonAsync(priceSorted);
        Assert.Equal(
            budgetExperienceId,
            priceSortedJson.RootElement.GetProperty("items")[0].GetProperty("id").GetInt32());
        Assert.Equal(
            experienceId,
            priceSortedJson.RootElement.GetProperty("items")[1].GetProperty("id").GetInt32());

        var distanceSorted = await Client.GetAsync(
            "/api/experiences?search=Lifecycle%20Experience&sortBy=Distance&sortDirection=Asc" +
            "&currentLatitude=30.06&currentLongitude=31.26&pageSize=10");
        distanceSorted.EnsureSuccessStatusCode();
        using var distanceSortedJson = await ReadJsonAsync(distanceSorted);
        Assert.Equal(
            experienceId,
            distanceSortedJson.RootElement.GetProperty("items")[0].GetProperty("id").GetInt32());

        foreach (var sortBy in new[] { "Rating", "Reviews", "Name", "Newest", "Popularity", "OpenNow" })
        {
            (await Client.GetAsync(
                $"/api/experiences?search=Lifecycle%20Experience&sortBy={sortBy}&sortDirection=Desc"))
                .EnsureSuccessStatusCode();
        }

        await AssertProblemAsync(
            await Client.GetAsync("/api/experiences?sortBy=Distance&sortDirection=Asc"),
            400,
            "validation_error");

        var booking = await SendAsync(
            HttpMethod.Post,
            $"/api/experiences/{experienceId}/bookings",
            traveler.Token,
            new { availabilityId, guestsCount = 2 });
        Assert.Equal(HttpStatusCode.Created, booking.StatusCode);
        using var bookingJson = await ReadJsonAsync(booking);
        var bookingId = bookingJson.RootElement.GetProperty("id").GetGuid();
        Assert.Equal(80, bookingJson.RootElement.GetProperty("totalPrice").GetDecimal());
        var providerNotifications = await SendAsync(
            HttpMethod.Get,
            "/api/notifications?page=1&pageSize=20",
            provider.Token);
        providerNotifications.EnsureSuccessStatusCode();
        using var providerNotificationsJson = await ReadJsonAsync(providerNotifications);
        Assert.Contains(
            providerNotificationsJson.RootElement.GetProperty("items").EnumerateArray(),
            item => item.GetProperty("type").GetString() == "Booking" &&
                    item.GetProperty("sourceEntityId").GetGuid() == bookingId);

        var managedAvailability = await SendAsync(
            HttpMethod.Get,
            $"/api/experiences/{experienceId}/availability/manage",
            provider.Token);
        managedAvailability.EnsureSuccessStatusCode();
        using var managedAvailabilityJson = await ReadJsonAsync(managedAvailability);
        Assert.Equal(4, managedAvailabilityJson.RootElement[0]
            .GetProperty("remainingCapacity").GetInt32());

        var confirm = await SendAsync(
            HttpMethod.Patch,
            $"/api/experience-bookings/{bookingId}/status",
            provider.Token,
            new { status = "Confirmed" });
        confirm.EnsureSuccessStatusCode();

        var myBookings = await SendAsync(
            HttpMethod.Get,
            "/api/experience-bookings/my",
            traveler.Token);
        myBookings.EnsureSuccessStatusCode();
        using var myBookingsJson = await ReadJsonAsync(myBookings);
        Assert.Equal("Confirmed", myBookingsJson.RootElement[0].GetProperty("status").GetString());

        var earlyReview = await SendAsync(
            HttpMethod.Post,
            $"/api/experiences/{experienceId}/reviews",
            traveler.Token,
            new { rating = 5, reviewText = "Excellent integration experience." });
        Assert.Equal(HttpStatusCode.BadRequest, earlyReview.StatusCode);

        await Factory.ExecuteAsync(
            $"""
             UPDATE experiences.experience_availability
             SET "StartTimeUtc" = NOW() - INTERVAL '3 hours',
                 "EndTimeUtc" = NOW() - INTERVAL '1 hour'
             WHERE "Id" = '{availabilityId}'
             """);
        (await SendAsync(
            HttpMethod.Patch,
            $"/api/experience-bookings/{bookingId}/status",
            provider.Token,
            new { status = "Completed" })).EnsureSuccessStatusCode();

        var review = await SendAsync(
            HttpMethod.Post,
            $"/api/experiences/{experienceId}/reviews",
            traveler.Token,
            new { rating = 5, reviewText = "Excellent integration experience." });
        review.EnsureSuccessStatusCode();
        Assert.Equal(
            HttpStatusCode.BadRequest,
            (await SendAsync(
                HttpMethod.Post,
                $"/api/experiences/{experienceId}/reviews",
                traveler.Token,
                new { rating = 4, reviewText = "Duplicate review." })).StatusCode);

        var reviews = await Client.GetAsync($"/api/experiences/{experienceId}/reviews");
        reviews.EnsureSuccessStatusCode();
        using var reviewsJson = await ReadJsonAsync(reviews);
        Assert.Contains(
            reviewsJson.RootElement.EnumerateArray(),
            item => item.GetProperty("reviewText").GetString() == "Excellent integration experience.");
    }

    [Fact]
    public async Task Structured_hotel_recommendations_rank_by_preferences_without_groq()
    {
        var owner = await CreateUserAsync("HotelOwner", "recommendation-owner");
        await UpsertHotelOwnerAsync(owner);

        var provider = await CreateUserAsync("ExperienceProvider", "recommendation-provider");
        await UpsertProviderAsync(provider);

        var suffix = Guid.NewGuid().ToString("N");
        var adm0Gid = Random.Shared.Next(600_000, 699_999);
        var bestStayId = await CreateStayAsync(
            owner.Token,
            $"Recommendation Best Stay {suffix}",
            price: 1500,
            latitude: 30.0500,
            longitude: 31.2400,
            amenities: ["Wi-Fi", "Gym"],
            adm0Gid);

        await CreateStayAsync(
            owner.Token,
            $"Recommendation Far Stay {suffix}",
            price: 1500,
            latitude: 30.4500,
            longitude: 31.7000,
            amenities: ["Parking"],
            adm0Gid);

        await CreateStayAsync(
            owner.Token,
            $"Recommendation Luxury Stay {suffix}",
            price: 5000,
            latitude: 30.0520,
            longitude: 31.2420,
            amenities: ["Wi-Fi"],
            adm0Gid);

        await CreateStayAsync(
            owner.Token,
            $"Recommendation Budget Stay {suffix}",
            price: 500,
            latitude: 30.0550,
            longitude: 31.2450,
            amenities: ["Gym"],
            adm0Gid);

        await CreateStayAsync(
            owner.Token,
            $"Recommendation Economy Stay {suffix}",
            price: 1000,
            latitude: 30.0560,
            longitude: 31.2460,
            amenities: [],
            adm0Gid);

        var historicalId = await CreateExperienceAsync(
            provider.Token,
            "Historical",
            $"Recommendation Museum {suffix}",
            latitude: 30.0505,
            longitude: 31.2405,
            adm0Gid);

        var diningId = await CreateExperienceAsync(
            provider.Token,
            "Dining",
            $"Recommendation Restaurant {suffix}",
            latitude: 30.0510,
            longitude: 31.2410,
            adm0Gid);

        var adminToken = await GetAdminTokenAsync();
        await ModerateExperienceAsync(adminToken, historicalId, "Approved");
        await ModerateExperienceAsync(adminToken, diningId, "Approved");

        var response = await SendAsync(HttpMethod.Post, "/api/stays/recommendations", owner.Token, new
        {
            budgetLevel = 3,
            experienceCategories = new[]
            {
                new { category = "Historical", weight = 50 },
                new { category = "Dining", weight = 50 }
            },
            requestedAmenities = new[] { "WiFi", "Gym" },
            adm0Gid,
            limit = 5,
            preferredLanguage = "en"
        });

        response.EnsureSuccessStatusCode();
        using var json = await ReadJsonAsync(response);
        var items = json.RootElement.GetProperty("items").EnumerateArray().ToList();

        Assert.NotEmpty(items);
        Assert.Equal(bestStayId, items[0].GetProperty("hotelId").GetInt32());
        Assert.Equal(1, items[0].GetProperty("ranking").GetInt32());
        Assert.InRange(items[0].GetProperty("budgetLevel").GetInt32(), 1, 5);
        Assert.Contains(
            items[0].GetProperty("matchedAmenities").EnumerateArray(),
            amenity => amenity.GetString() == "WiFi");
        Assert.Contains(
            items[0].GetProperty("matchedAmenities").EnumerateArray(),
            amenity => amenity.GetString() == "Gym");
        Assert.False(items[0]
            .GetProperty("explanation")
            .GetProperty("isAiGenerated")
            .GetBoolean());
    }

    [Fact]
    public async Task Recommendations_exclude_inactive_stays_and_non_approved_experiences_and_redistribute_missing_categories()
    {
        var owner = await CreateUserAsync("HotelOwner", "recommendation-eligibility-owner");
        await UpsertHotelOwnerAsync(owner);
        var provider = await CreateUserAsync("ExperienceProvider", "recommendation-eligibility-provider");
        await UpsertProviderAsync(provider);
        var adminToken = await GetAdminTokenAsync();
        var adm0Gid = Random.Shared.Next(700_000, 800_000);
        var suffix = Guid.NewGuid().ToString("N");

        var activeStayId = await CreateStayAsync(
            owner.Token, $"Eligible Stay {suffix}", 100, 27.1, 30.1, [], adm0Gid);
        var inactiveStayId = await CreateStayAsync(
            owner.Token, $"Inactive Stay {suffix}", 90, 27.11, 30.11, [], adm0Gid);
        (await SendAsync(HttpMethod.Patch, $"/api/stays/{inactiveStayId}/deactivate", owner.Token))
            .EnsureSuccessStatusCode();

        var pendingId = await CreateExperienceAsync(
            provider.Token, "Historical", $"Pending Experience {suffix}", 27.101, 30.101, adm0Gid);
        var rejectedId = await CreateExperienceAsync(
            provider.Token, "Historical", $"Rejected Experience {suffix}", 27.102, 30.102, adm0Gid);
        var inactiveId = await CreateExperienceAsync(
            provider.Token, "Historical", $"Inactive Experience {suffix}", 27.103, 30.103, adm0Gid);
        var approvedId = await CreateExperienceAsync(
            provider.Token, "Historical", $"Approved Experience {suffix}", 27.104, 30.104, adm0Gid);

        await ModerateExperienceAsync(adminToken, rejectedId, "Rejected", "Not eligible for recommendation tests.");
        await ModerateExperienceAsync(adminToken, inactiveId, "Approved");
        (await SendAsync(HttpMethod.Patch, $"/api/experiences/{inactiveId}/deactivate", provider.Token))
            .EnsureSuccessStatusCode();
        await ModerateExperienceAsync(adminToken, approvedId, "Approved");

        var historical = await SendAsync(HttpMethod.Post, "/api/stays/recommendations", owner.Token, new
        {
            adm0Gid,
            experienceCategories = new[] { new { category = "Historical", weight = 1 } },
            requestedAmenities = Array.Empty<string>(),
            limit = 10
        });
        historical.EnsureSuccessStatusCode();
        using var historicalJson = await ReadJsonAsync(historical);
        var historicalItems = historicalJson.RootElement.GetProperty("items").EnumerateArray().ToList();
        Assert.Contains(historicalItems, item => item.GetProperty("hotelId").GetInt32() == activeStayId);
        Assert.DoesNotContain(historicalItems, item => item.GetProperty("hotelId").GetInt32() == inactiveStayId);
        var activeItem = historicalItems.Single(item => item.GetProperty("hotelId").GetInt32() == activeStayId);
        var nearbyNames = activeItem.GetProperty("nearbyExperiences")
            .EnumerateArray()
            .Select(item => item.GetProperty("name").GetString())
            .ToList();
        Assert.Contains($"Approved Experience {suffix}", nearbyNames);
        Assert.DoesNotContain($"Pending Experience {suffix}", nearbyNames);
        Assert.DoesNotContain($"Rejected Experience {suffix}", nearbyNames);
        Assert.DoesNotContain($"Inactive Experience {suffix}", nearbyNames);

        var redistributed = await SendAsync(HttpMethod.Post, "/api/stays/recommendations", owner.Token, new
        {
            adm0Gid,
            experienceCategories = new[]
            {
                new { category = "Historical", weight = 0.5 },
                new { category = "Dining", weight = 0.5 }
            },
            requestedAmenities = Array.Empty<string>(),
            limit = 10
        });
        redistributed.EnsureSuccessStatusCode();
        using var redistributedJson = await ReadJsonAsync(redistributed);
        var redistributedItem = redistributedJson.RootElement.GetProperty("items")
            .EnumerateArray()
            .Single(item => item.GetProperty("hotelId").GetInt32() == activeStayId);
        Assert.Equal(
            activeItem.GetProperty("scores").GetProperty("interestProximityScore").GetDouble(),
            redistributedItem.GetProperty("scores").GetProperty("interestProximityScore").GetDouble(),
            precision: 6);

        var unavailable = await SendAsync(HttpMethod.Post, "/api/stays/recommendations", owner.Token, new
        {
            adm0Gid,
            experienceCategories = new[] { new { category = "Nature", weight = 1 } },
            requestedAmenities = Array.Empty<string>(),
            limit = 10
        });
        unavailable.EnsureSuccessStatusCode();
        using var unavailableJson = await ReadJsonAsync(unavailable);
        var unavailableItem = unavailableJson.RootElement.GetProperty("items")
            .EnumerateArray()
            .Single(item => item.GetProperty("hotelId").GetInt32() == activeStayId);
        Assert.Equal(
            JsonValueKind.Null,
            unavailableItem.GetProperty("scores").GetProperty("interestProximityScore").ValueKind);

        Assert.True(pendingId > 0);
    }

    [Fact]
    public async Task Recommendation_experience_limit_is_applied_independently_per_category()
    {
        var owner = await CreateUserAsync("HotelOwner", "recommendation-category-owner");
        await UpsertHotelOwnerAsync(owner);
        var provider = await CreateUserAsync("ExperienceProvider", "recommendation-category-provider");
        await UpsertProviderAsync(provider);
        var adminToken = await GetAdminTokenAsync();
        var adm0Gid = Random.Shared.Next(800_001, 900_000);
        var suffix = Guid.NewGuid().ToString("N");

        var stayId = await CreateStayAsync(owner.Token, $"Category Stay {suffix}", 100, 28, 31, [], adm0Gid);
        foreach (var category in new[] { "Historical", "Historical", "Dining", "Dining" })
        {
            var experienceId = await CreateExperienceAsync(
                provider.Token, category, $"{category} {Guid.NewGuid():N}", 28.001, 31.001, adm0Gid);
            await ModerateExperienceAsync(adminToken, experienceId, "Approved");
        }

        var response = await SendAsync(HttpMethod.Post, "/api/stays/recommendations", owner.Token, new
        {
            adm0Gid,
            experienceCategories = new[]
            {
                new { category = "Historical", weight = 1 },
                new { category = "Dining", weight = 1 }
            },
            requestedAmenities = Array.Empty<string>(),
            limit = 5
        });
        response.EnsureSuccessStatusCode();
        using var json = await ReadJsonAsync(response);
        var item = json.RootElement.GetProperty("items").EnumerateArray()
            .Single(item => item.GetProperty("hotelId").GetInt32() == stayId);
        var nearby = item.GetProperty("nearbyExperiences").EnumerateArray().ToList();
        Assert.Equal(2, nearby.Count);
        Assert.Single(nearby, value => value.GetProperty("category").GetString() == "Historical");
        Assert.Single(nearby, value => value.GetProperty("category").GetString() == "Dining");
    }

    [Fact]
    public async Task Recommendation_quintiles_use_all_active_prices_and_unbiased_global_fallback()
    {
        var requester = await CreateUserAsync("Traveler", "recommendation-quintiles");
        var localAdm0 = Random.Shared.Next(900_001, 950_000);
        var localPrefix = $"quintile-{Guid.NewGuid():N}";
        foreach (var price in new decimal[] { 10, 20, 30, 40, 50 })
            await InsertDistributionStayAsync(localPrefix, price, localAdm0, isActive: true, withCoordinates: true);
        await InsertDistributionStayAsync(localPrefix, 100_000, localAdm0, isActive: false, withCoordinates: false);
        await InsertDistributionStayAsync(localPrefix, 0, localAdm0, isActive: true, withCoordinates: true);
        await InsertDistributionStayAsync(localPrefix, null, localAdm0, isActive: true, withCoordinates: true);

        var localResponse = await SendAsync(HttpMethod.Post, "/api/stays/recommendations", requester.Token, new
        {
            adm0Gid = localAdm0,
            experienceCategories = Array.Empty<object>(),
            requestedAmenities = Array.Empty<string>(),
            limit = 10
        });
        localResponse.EnsureSuccessStatusCode();
        using var localJson = await ReadJsonAsync(localResponse);
        var localItems = localJson.RootElement.GetProperty("items").EnumerateArray().ToList();
        Assert.Equal(5, localItems.Single(item =>
                item.GetProperty("price").ValueKind == JsonValueKind.Number &&
                item.GetProperty("price").GetDecimal() == 50)
            .GetProperty("budgetLevel").GetInt32());
        foreach (var item in localItems.Where(item =>
                     item.GetProperty("price").ValueKind == JsonValueKind.Null ||
                     item.GetProperty("price").GetDecimal() == 0))
            Assert.Equal(JsonValueKind.Null, item.GetProperty("budgetLevel").ValueKind);

        var cappedAdm0 = Random.Shared.Next(1_000_000, 1_050_000);
        var cappedPrefix = $"candidate-cap-{Guid.NewGuid():N}";
        await Factory.ExecuteAsync($"""
            INSERT INTO stays.stays
                ("SourceType", "Name", "Price", "Adm0Gid", "Latitude", "Longitude", "IsActive", "CreatedAtUtc")
            SELECT 'ThirdParty', '{cappedPrefix}-' || value, value::numeric, {cappedAdm0}, NULL, NULL, TRUE, NOW()
            FROM generate_series(1, 301) AS value;
            """);
        var cappedCandidateId = await InsertDistributionStayAsync(
            cappedPrefix, 250, cappedAdm0, isActive: true, withCoordinates: true);
        var cappedResponse = await SendAsync(HttpMethod.Post, "/api/stays/recommendations", requester.Token, new
        {
            adm0Gid = cappedAdm0,
            experienceCategories = Array.Empty<object>(),
            requestedAmenities = Array.Empty<string>(),
            limit = 5
        });
        cappedResponse.EnsureSuccessStatusCode();
        using var cappedJson = await ReadJsonAsync(cappedResponse);
        var cappedCandidate = cappedJson.RootElement.GetProperty("items").EnumerateArray()
            .Single(item => item.GetProperty("hotelId").GetInt32() == cappedCandidateId);
        Assert.Equal(5, cappedCandidate.GetProperty("budgetLevel").GetInt32());

        var globalPrefix = $"global-{Guid.NewGuid():N}";
        await Factory.ExecuteAsync($"""
            INSERT INTO stays.stays
                ("SourceType", "Name", "Price", "Latitude", "Longitude", "IsActive", "CreatedAtUtc")
            SELECT 'ThirdParty', '{globalPrefix}-' || value, value::numeric, NULL, NULL, TRUE, NOW()
            FROM generate_series(1, 6001) AS value;
            """);
        var fallbackAdm0 = Random.Shared.Next(950_001, 999_999);
        var candidateId = await InsertDistributionStayAsync(
            globalPrefix, 4500, fallbackAdm0, isActive: true, withCoordinates: true);

        var globalResponse = await SendAsync(HttpMethod.Post, "/api/stays/recommendations", requester.Token, new
        {
            adm0Gid = fallbackAdm0,
            experienceCategories = Array.Empty<object>(),
            requestedAmenities = Array.Empty<string>(),
            limit = 5
        });
        globalResponse.EnsureSuccessStatusCode();
        using var globalJson = await ReadJsonAsync(globalResponse);
        var candidate = globalJson.RootElement.GetProperty("items").EnumerateArray()
            .Single(item => item.GetProperty("hotelId").GetInt32() == candidateId);
        Assert.Equal(4, candidate.GetProperty("budgetLevel").GetInt32());
    }

    [Fact]
    public async Task Recommendation_input_limits_and_named_rate_limit_are_enforced()
    {
        var user = await CreateUserAsync("Traveler", "recommendation-limits");
        var overlong = await SendAsync(
            HttpMethod.Post,
            "/api/stays/recommendations/natural-language",
            user.Token,
            new { text = new string('x', 1001), limit = 3 });
        Assert.Equal(HttpStatusCode.BadRequest, overlong.StatusCode);

        var tooManyAmenities = await SendAsync(
            HttpMethod.Post,
            "/api/stays/recommendations",
            user.Token,
            new
            {
                experienceCategories = Array.Empty<object>(),
                requestedAmenities = Enumerable.Range(1, 21).Select(index => $"Amenity {index}").ToArray(),
                limit = 3
            });
        Assert.Equal(HttpStatusCode.BadRequest, tooManyAmenities.StatusCode);

        var rateUser = await CreateUserAsync("Traveler", "recommendation-rate-limit");
        HttpResponseMessage? last = null;
        for (var attempt = 0; attempt < 11; attempt++)
        {
            last?.Dispose();
            last = await SendAsync(
                HttpMethod.Post,
                "/api/stays/recommendations",
                rateUser.Token,
                new
                {
                    experienceCategories = Array.Empty<object>(),
                    requestedAmenities = Array.Empty<string>(),
                    limit = 1
                });
        }

        using (last)
            Assert.Equal(HttpStatusCode.TooManyRequests, last?.StatusCode);
    }

    [Fact]
    public async Task Bookable_only_uses_real_future_slot_capacity_and_availability_scores()
    {
        var provider = await CreateUserAsync("ExperienceProvider", "bookable-recommendation");
        await UpsertProviderAsync(provider);
        var admin = await GetAdminTokenAsync();
        var experienceId = await CreateExperienceAsync(
            provider.Token,
            "Historical",
            $"Bookable Recommendation {Guid.NewGuid():N}",
            30.05,
            31.25);
        await ModerateExperienceAsync(admin, experienceId, "Approved");
        var start = DateTime.UtcNow.AddDays(4);
        (await SendAsync(
            HttpMethod.Post,
            $"/api/experiences/{experienceId}/availability",
            provider.Token,
            new
            {
                startTimeUtc = start,
                endTimeUtc = start.AddHours(2),
                capacity = 6,
                pricePerPerson = 25
            })).EnsureSuccessStatusCode();

        var response = await SendAsync(
            HttpMethod.Post,
            "/api/experiences/recommendations",
            body: new
            {
                categories = new[] { new { category = "Historical" } },
                guestsCount = 2,
                bookableOnly = true,
                limit = 10
            });
        response.EnsureSuccessStatusCode();
        using var json = await ReadJsonAsync(response);
        var item = json.RootElement.GetProperty("items").EnumerateArray()
            .Single(value => value.GetProperty("experienceId").GetInt32() == experienceId);
        Assert.True(item.GetProperty("availabilityDataAvailable").GetBoolean());
        Assert.True(item.GetProperty("isBookable").GetBoolean());
        Assert.Equal(6, item.GetProperty("availableCapacity").GetInt32());
        Assert.True(item.GetProperty("matchingSlotCount").GetInt32() > 0);
        Assert.True(item.GetProperty("scores").GetProperty("availabilityScore").GetDouble() > 0);
        Assert.NotEqual(
            Guid.Empty,
            item.GetProperty("nextAvailableSlot").GetProperty("availabilityId").GetGuid());
    }

    [Fact]
    public async Task Favorite_pagination_and_batch_status_are_stable_across_pages()
    {
        var owner = await CreateUserAsync("HotelOwner", "favorite-owner");
        await UpsertHotelOwnerAsync(owner);
        var traveler = await CreateUserAsync("Traveler", "favorite-traveler");
        var first = await CreateStayAsync(owner.Token, $"Favorite A {Guid.NewGuid():N}", 100, 30, 31, []);
        var second = await CreateStayAsync(owner.Token, $"Favorite B {Guid.NewGuid():N}", 120, 30.1, 31.1, []);
        (await SendAsync(HttpMethod.Post, $"/api/stays/{first}/favorite", traveler.Token)).EnsureSuccessStatusCode();
        (await SendAsync(HttpMethod.Post, $"/api/stays/{second}/favorite", traveler.Token)).EnsureSuccessStatusCode();

        var page = await SendAsync(HttpMethod.Get, "/api/stays/favorites?page=1&pageSize=1", traveler.Token);
        page.EnsureSuccessStatusCode();
        using var pageJson = await ReadJsonAsync(page);
        Assert.Equal(2, pageJson.RootElement.GetProperty("totalCount").GetInt32());
        Assert.Equal(2, pageJson.RootElement.GetProperty("totalPages").GetInt32());
        Assert.Single(pageJson.RootElement.GetProperty("items").EnumerateArray());

        var statuses = await SendAsync(
            HttpMethod.Post,
            "/api/stays/favorite-statuses",
            traveler.Token,
            new { stayIds = new[] { first, second, int.MaxValue } });
        statuses.EnsureSuccessStatusCode();
        using var statusJson = await ReadJsonAsync(statuses);
        var ids = statusJson.RootElement.GetProperty("favoriteStayIds")
            .EnumerateArray().Select(value => value.GetInt32()).ToArray();
        Assert.Equal(new[] { first, second }.Order(), ids.Order());
    }

    private async Task<int> CreateStayAsync(
        string token,
        string name,
        decimal price,
        double latitude,
        double longitude,
        string[] amenities,
        int? adm0Gid = null)
    {
        var response = await SendAsync(
            HttpMethod.Post,
            "/api/stays",
            token,
            new
            {
                name,
                price,
                description = "Recommendation integration stay",
                latitude,
                longitude,
                amenities,
                adm0Gid
            });

        response.EnsureSuccessStatusCode();
        using var json = await ReadJsonAsync(response);
        return json.RootElement.GetProperty("id").GetInt32();
    }

    private async Task<HttpResponseMessage> ImportExperiencesJsonAsync(
        string adminToken,
        string category,
        string json)
    {
        using var content = new MultipartFormDataContent();
        content.Add(new StringContent(category), "Category");

        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(json));
        fileContent.Headers.ContentType =
            new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
        content.Add(fileContent, "File", "experiences.json");

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/experiences/import");
        request.Headers.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", adminToken);
        request.Content = content;

        return await Client.SendAsync(request);
    }

    private async Task<int> CreateExperienceAsync(
        string token,
        string category,
        string name,
        double latitude,
        double longitude,
        int? adm0Gid = null)
    {
        var response = await SendAsync(
            HttpMethod.Post,
            "/api/experiences",
            token,
            new
            {
                category,
                name,
                description = "Recommendation integration experience",
                address = "Recommendation integration neighbourhood",
                latitude,
                longitude,
                adm0Gid
            });

        response.EnsureSuccessStatusCode();
        using var json = await ReadJsonAsync(response);
        return json.RootElement.GetProperty("id").GetInt32();
    }

    private async Task ModerateExperienceAsync(
        string adminToken,
        int experienceId,
        string status,
        string? rejectionReason = null)
    {
        var response = await SendAsync(
            HttpMethod.Patch,
            $"/api/admin/experiences/{experienceId}/moderation",
            adminToken,
            new { moderationStatus = status, moderationNotes = rejectionReason });
        response.EnsureSuccessStatusCode();
    }

    private async Task<int> InsertDistributionStayAsync(
        string prefix,
        decimal? price,
        int adm0Gid,
        bool isActive,
        bool withCoordinates)
    {
        var priceSql = price?.ToString(CultureInfo.InvariantCulture) ?? "NULL";
        var coordinateSql = withCoordinates ? "29.0" : "NULL";
        var name = $"{prefix}-{Guid.NewGuid():N}";
        return await Factory.ScalarAsync<int>($"""
            INSERT INTO stays.stays
                ("SourceType", "Name", "Price", "Adm0Gid", "Latitude", "Longitude", "IsActive", "CreatedAtUtc")
            VALUES
                ('ThirdParty', '{name}', {priceSql}, {adm0Gid}, {coordinateSql}, {coordinateSql}, {isActive.ToString().ToUpperInvariant()}, NOW())
            RETURNING "Id";
            """);
    }
}
