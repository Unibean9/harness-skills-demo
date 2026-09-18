using System.Net;
using System.Net.Http.Json;
using UniEvent.Api.Modules.Identity;

namespace UniEvent.Api.Tests;

/// <summary>
/// Regression tests for the hs-code-review findings on issue #1.
/// </summary>
public class ReviewFixRegressionTests
{
    [Fact]
    public async Task Register_ConcurrentSameEmail_YieldsExactlyOneCreated_AndRestConflict()
    {
        using var factory = new CustomWebApplicationFactory();
        var client = factory.CreateClient();
        var email = $"racer-{Guid.NewGuid():N}@example.com";

        var responses = await Task.WhenAll(Enumerable.Range(0, 12).Select(_ =>
            client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, "P@ssword123"))));

        Assert.Equal(1, responses.Count(r => r.StatusCode == HttpStatusCode.Created));
        Assert.All(responses.Where(r => r.StatusCode != HttpStatusCode.Created),
            r => Assert.Equal(HttpStatusCode.Conflict, r.StatusCode));
    }

    [Theory]
    [InlineData("Admin@Uni.edu")]
    [InlineData("admin@uni.edu")]
    public async Task SeededOrganizer_WithMixedCaseConfiguredEmail_CanLogInWithAnyCasing(string loginEmail)
    {
        using var factory = new CustomWebApplicationFactory
        {
            Overrides = { ["SeedOrganizer:Email"] = "Admin@Uni.edu" }
        };
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest(loginEmail, CustomWebApplicationFactory.SeedOrganizerPassword));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Seed_WithoutConfiguredPassword_CreatesNoOrganizer_AndNoDefaultCredentialsWork()
    {
        using var factory = new CustomWebApplicationFactory
        {
            Overrides = { ["SeedOrganizer:Password"] = "" }
        };
        var client = factory.CreateClient();

        // The old hard-coded fallback credentials must not log in.
        var fallback = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest("organizer@uni-event.local", "ChangeMe!Organizer1"));
        Assert.Equal(HttpStatusCode.Unauthorized, fallback.StatusCode);

        var blank = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest("organizer@uni-event.local", CustomWebApplicationFactory.SeedOrganizerPassword));
        Assert.Equal(HttpStatusCode.Unauthorized, blank.StatusCode);
    }
}
