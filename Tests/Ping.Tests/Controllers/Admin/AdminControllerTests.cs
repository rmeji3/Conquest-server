using Xunit;
using Xunit.Abstractions;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http.Json;
using Ping.Dtos.Auth;
using Ping.Dtos.Pings;
using Ping.Dtos.Stickers;
using Ping.Models.Pings;
using Microsoft.AspNetCore.Mvc;

namespace Ping.Tests.Controllers.Admin;

public class AdminControllerTests : BaseIntegrationTest
{
    private readonly ITestOutputHelper _output;

    public AdminControllerTests(IntegrationTestFactory factory, ITestOutputHelper output) : base(factory)
    {
        _output = output;
    }

    [Fact]
    public async Task TestMakeAdmin()
    {
        // Arrange
        // Authenticate as Admin first
        Authenticate("adminUser", "Admin");
        
        // create a user (we don't need to authenticate as them, just register them)
        var userRequest = new RegisterDto("test@email.com", "Password1!", "First", "Last", "user1");
    
        // Act
        // 1. Register the user
        var userResponse = await _client.PostAsJsonAsync("/api/auth/register", userRequest);
        Assert.Equal(HttpStatusCode.OK, userResponse.StatusCode);

        // 2. Make user admin (using correct endpoint and email)
        var adminResponse = await _client.PostAsJsonAsync($"/api/admin/users/make-admin?email={userRequest.Email}", new { });
        
        // Debug output 
        var content = await adminResponse.Content.ReadAsStringAsync();
        _output.WriteLine($"Admin Response: {content}");
        _output.WriteLine($"Detailed Status: {adminResponse.StatusCode}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, adminResponse.StatusCode);
    }

    [Fact]
    public async Task TestRemoveAdmin()
    {
        // Arrange
        Authenticate("adminUser", "Admin");
        
        // 1. Register a user (User2)
        var userRequest = new RegisterDto("user2@email.com", "Password1!", "User", "Two", "user2");
        var userResponse = await _client.PostAsJsonAsync("/api/auth/register", userRequest);
        Assert.Equal(HttpStatusCode.OK, userResponse.StatusCode);

        // 2. Make User2 an Admin first
        var makeAdminResponse = await _client.PostAsJsonAsync($"/api/admin/users/make-admin?email={userRequest.Email}", new { });
        Assert.Equal(HttpStatusCode.OK, makeAdminResponse.StatusCode);

        // Act
        // 3. Remove Admin from User2
        var removeAdminResponse = await _client.PostAsJsonAsync($"/api/admin/users/remove-admin?email={userRequest.Email}", new { });
        
        // Debug output
        var content = await removeAdminResponse.Content.ReadAsStringAsync();
        _output.WriteLine($"Remove Admin Response: {content}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, removeAdminResponse.StatusCode);
    }

    [Fact]
    public async Task TestBanUser()
    {
        // Arrange
        Authenticate("adminUser", "Admin");
        
        // 1. Register a user (User5)
        var userRequest = new RegisterDto("user5@email.com", "Password1!", "User", "Five", "user5");
        var userResponse = await _client.PostAsJsonAsync("/api/auth/register", userRequest);
        Assert.Equal(HttpStatusCode.OK, userResponse.StatusCode);

        // Act
        // 2. Ban User5
        var banResponse = await _client.PostAsJsonAsync($"/api/admin/users/ban?username={userRequest.UserName}&reason=Testing", new { });
        
        // Debug output
        var content = await banResponse.Content.ReadAsStringAsync();
        _output.WriteLine($"Ban Response: {content}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, banResponse.StatusCode);

        // 3. Try to login as banned user
        _client.DefaultRequestHeaders.Authorization = null;
        var loginRequest = new LoginDto(userRequest.UserName, "Password1!");
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
        Assert.Equal(HttpStatusCode.Unauthorized, loginResponse.StatusCode);
        
        // 4. check if user is banned (Re-authenticate as Admin first)
        Authenticate("adminUser", "Admin");
        var searchBanned = await _client.GetFromJsonAsync<Ping.Dtos.Moderation.BannedUserDto>($"/api/admin/users/banned?username={userRequest.UserName}");
        Assert.NotNull(searchBanned);
        Assert.Equal(userRequest.UserName.ToUpper(), searchBanned.Username.ToUpper()); 
    }

    [Fact]
    public async Task TestUnbanUser()
    {
        // Arrange
        Authenticate("adminUser", "Admin");
        
        // 1. Register a user (User2)
        var userRequest = new RegisterDto("user4@email.com", "Password1!", "User", "Four", "user4");
        var userResponse = await _client.PostAsJsonAsync("/api/auth/register", userRequest);
        Assert.Equal(HttpStatusCode.OK, userResponse.StatusCode);

        // Act
        // 2. Ban User2
        var banResponse = await _client.PostAsJsonAsync($"/api/admin/users/ban?username={userRequest.UserName}&reason=Testing", new { });
        Assert.Equal(HttpStatusCode.OK, banResponse.StatusCode);

        // 3. Try to login as banned user
        _client.DefaultRequestHeaders.Authorization = null;
        var loginRequest = new LoginDto(userRequest.UserName, "Password1!");
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
        Assert.Equal(HttpStatusCode.Unauthorized, loginResponse.StatusCode);
        
        // 4. Unban User2 (Re-authenticate as Admin first)
        Authenticate("adminUser", "Admin");
        var unbanResponse = await _client.PostAsJsonAsync($"/api/admin/users/unban?username={userRequest.UserName}", new { });
        Assert.Equal(HttpStatusCode.OK, unbanResponse.StatusCode);
        
        // 5. Try to login as unbanned user (or check banned status endpoint)
        var unbannedResponse = await _client.GetAsync($"/api/admin/users/banned?username={userRequest.UserName}");
        Assert.Equal(HttpStatusCode.NotFound, unbannedResponse.StatusCode);
    }

    [Fact]
    public async Task TestGetBannedUsers()
    {
        // Arrange
        Authenticate("adminUser", "Admin");
        
        // 1. Register a user (User3)
        var userRequest = new RegisterDto("user3@email.com", "Password1!", "User", "Three", "user3");
        var userResponse = await _client.PostAsJsonAsync("/api/auth/register", userRequest);
        Assert.Equal(HttpStatusCode.OK, userResponse.StatusCode);

        // 2. Ban User3
        await _client.PostAsJsonAsync($"/api/admin/users/ban?username={userRequest.UserName}&reason=Testing", new { });

        // Act & Assert
        // 3. List banned users
        var listResponse = await _client.GetFromJsonAsync<Ping.Dtos.Common.PagedResult<Ping.Dtos.Moderation.BannedUserDto>>("/api/admin/users/banned");
        Assert.NotNull(listResponse);
        
        // use assert.contains to check if listResponse.Items contains userRequest.UserName
        Assert.Contains(listResponse.Items, u => u.Username == userRequest.UserName);
        // 4. Get specific banned user by username
        var userResponse3 = await _client.GetFromJsonAsync<Ping.Dtos.Moderation.BannedUserDto>($"/api/admin/users/banned?username={userRequest.UserName}");
        Assert.NotNull(userResponse3);
        Assert.Equal(userRequest.UserName.ToUpper(), userResponse3.Username.ToUpper());
        Assert.Equal("Testing", userResponse3.BanReason);
    }

    [Fact]
    public async Task TestGetUsersSortedByRecent()
    {
        // Arrange
        Authenticate("adminUser", "Admin");

        var olderUser = new RegisterDto("older@email.com", "Password1!", "Old", "User", "olderuser");
        Assert.Equal(HttpStatusCode.OK, (await _client.PostAsJsonAsync("/api/auth/register", olderUser)).StatusCode);

        var newerUser = new RegisterDto("newer@email.com", "Password1!", "New", "User", "neweruser");
        Assert.Equal(HttpStatusCode.OK, (await _client.PostAsJsonAsync("/api/auth/register", newerUser)).StatusCode);

        // Act: sort by most recently joined
        var result = await _client.GetFromJsonAsync<PaginatedResultShape<UserDto>>(
            "/api/admin/users?sortBy=recent&limit=2");

        // Assert: the most recently registered user comes first
        Assert.NotNull(result);
        var items = result.Items.ToList();
        Assert.True(items.Count >= 2);
        Assert.Equal(newerUser.UserName.ToUpper(), items[0].DisplayName!.ToUpper());
        Assert.NotNull(items[0].CreatedUtc);
    }

    // System.Text.Json can't bind PaginatedResult<T>'s primary constructor (param names
    // don't match property names), so tests deserialize into this equivalent shape instead.
    private record PaginatedResultShape<T>(List<T> Items, int TotalCount, int PageNumber, int PageSize, int TotalPages);

    [Fact]
    public async Task TestUpdateSticker()
    {
        // Arrange
        Authenticate("adminUser", "Admin");

        using var createForm = new MultipartFormDataContent
        {
            { new StringContent("edit_test_sticker"), "key" },
            { new StringContent("Original Name"), "name" },
            { new StringContent("custom"), "category" }
        };
        var createResponse = await _client.PostAsync("/api/admin/stickers", createForm);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<StickerDto>();
        Assert.NotNull(created);

        // Act: edit the sticker's name and category
        using var updateForm = new MultipartFormDataContent
        {
            { new StringContent("Updated Name"), "name" },
            { new StringContent("badge"), "category" }
        };
        var updateResponse = await _client.PutAsync($"/api/admin/stickers/{created.Id}", updateForm);

        // Assert
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updated = await updateResponse.Content.ReadFromJsonAsync<StickerDto>();
        Assert.NotNull(updated);
        Assert.Equal("Updated Name", updated.Name);
        Assert.Equal("badge", updated.Category);
        Assert.Equal(created.Key, updated.Key);
    }

    [Fact]
    public async Task TestSetPingVerified()
    {
        // Arrange
        Authenticate("adminUser", "Admin");

        var placeRequest = new UpsertPingDto(
            "Verify Test Place",
            "123 Verify St",
            40.7128,
            -74.0060,
            PingVisibility.Public,
            PingType.Custom,
            null,
            null);
        var jsonOptions = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        jsonOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());

        var placeResponse = await _client.PostAsJsonAsync("/api/pings", placeRequest);
        Assert.Equal(HttpStatusCode.Created, placeResponse.StatusCode);
        var place = await placeResponse.Content.ReadFromJsonAsync<PingDetailsDto>(jsonOptions);
        Assert.NotNull(place);
        Assert.Equal(PingType.Custom, place.Type);

        // Act: verify it
        var verifyResponse = await _client.PutAsJsonAsync($"/api/admin/pings/{place.Id}/verify", new { verified = true });

        // Assert
        Assert.Equal(HttpStatusCode.OK, verifyResponse.StatusCode);
        var verified = await verifyResponse.Content.ReadFromJsonAsync<PingDetailsDto>(jsonOptions);
        Assert.NotNull(verified);
        Assert.Equal(PingType.Verified, verified.Type);

        // Act: unverify it
        var unverifyResponse = await _client.PutAsJsonAsync($"/api/admin/pings/{place.Id}/verify", new { verified = false });
        Assert.Equal(HttpStatusCode.OK, unverifyResponse.StatusCode);
        var unverified = await unverifyResponse.Content.ReadFromJsonAsync<PingDetailsDto>(jsonOptions);
        Assert.NotNull(unverified);
        Assert.Equal(PingType.Custom, unverified.Type);
    }
}

