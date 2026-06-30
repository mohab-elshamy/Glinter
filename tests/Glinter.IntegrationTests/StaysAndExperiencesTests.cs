using Glinter.IntegrationTests.Infrastructure;
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

        var review = await SendAsync(
            HttpMethod.Post,
            $"/api/stays/{stayId}/reviews",
            traveler.Token,
            new { rating = 5, reviewText = "Excellent integration stay." });
        review.EnsureSuccessStatusCode();

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

        var booking = await SendAsync(
            HttpMethod.Post,
            $"/api/experiences/{experienceId}/bookings",
            traveler.Token,
            new { availabilityId, guestsCount = 2 });
        Assert.Equal(HttpStatusCode.Created, booking.StatusCode);
        using var bookingJson = await ReadJsonAsync(booking);
        var bookingId = bookingJson.RootElement.GetProperty("id").GetGuid();
        Assert.Equal(80, bookingJson.RootElement.GetProperty("totalPrice").GetDecimal());

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

        var review = await SendAsync(
            HttpMethod.Post,
            $"/api/experiences/{experienceId}/reviews",
            traveler.Token,
            new { rating = 5, reviewText = "Excellent integration experience." });
        review.EnsureSuccessStatusCode();

        var reviews = await Client.GetAsync($"/api/experiences/{experienceId}/reviews");
        reviews.EnsureSuccessStatusCode();
        using var reviewsJson = await ReadJsonAsync(reviews);
        Assert.Contains(
            reviewsJson.RootElement.EnumerateArray(),
            item => item.GetProperty("reviewText").GetString() == "Excellent integration experience.");
    }
}
