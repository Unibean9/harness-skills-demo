using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using UniEvent.Api.Modules.Identity;

namespace UniEvent.Api.Tests;

public class AuthEndpointsTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private static string UniqueEmail() => $"participant-{Guid.NewGuid():N}@example.com";

    [Fact]
    public async Task Register_ThenLogin_IssuesJwt_AndMeReturnsMatchingClaims()
    {
        var email = UniqueEmail();
        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest(email, "P@ssword123"));

        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);
        var registered = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(registered);
        Assert.Equal("Participant", registered!.Role);

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest(email, "P@ssword123"));
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        var loggedIn = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(loggedIn);

        using var meRequest = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        meRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", loggedIn!.Token);
        var meResponse = await _client.SendAsync(meRequest);

        Assert.Equal(HttpStatusCode.OK, meResponse.StatusCode);
        var me = await meResponse.Content.ReadFromJsonAsync<MeResponse>();
        Assert.NotNull(me);
        Assert.Equal(registered.UserId, me!.UserId);
        Assert.Equal(email, me.Email);
        Assert.Equal("Participant", me.Role);
    }

    [Fact]
    public async Task Register_DuplicateEmail_ReturnsConflict()
    {
        var email = UniqueEmail();
        var first = await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, "P@ssword123"));
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var second = await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, "AnotherPass1"));
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Login_WrongPassword_ReturnsUnauthorized()
    {
        var email = UniqueEmail();
        await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, "P@ssword123"));

        var response = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, "wrong-password"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Me_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task OrganizerOnly_WithParticipantToken_ReturnsForbidden_WithOrganizerToken_ReturnsOk()
    {
        var participantEmail = UniqueEmail();
        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest(participantEmail, "P@ssword123"));
        var participant = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>();

        using var participantRequest = new HttpRequestMessage(HttpMethod.Get, "/api/auth/organizer-only");
        participantRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", participant!.Token);
        var participantResponse = await _client.SendAsync(participantRequest);
        Assert.Equal(HttpStatusCode.Forbidden, participantResponse.StatusCode);

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest("organizer@uni-event.local", "Test!Organizer1"));
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        var organizer = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();

        using var organizerRequest = new HttpRequestMessage(HttpMethod.Get, "/api/auth/organizer-only");
        organizerRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", organizer!.Token);
        var organizerResponse = await _client.SendAsync(organizerRequest);
        Assert.Equal(HttpStatusCode.OK, organizerResponse.StatusCode);
    }
}
