using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace UniEvent.Api.Tests;

/// <summary>
/// Boots the real API pipeline (auth, EF Core, seeding) against a fresh
/// SQLite file per factory instance instead of the dev database, and
/// supplies a test-only JWT signing key (never the real one).
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    public const string SeedOrganizerPassword = "Test!Organizer1";

    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"uni-event-tests-{Guid.NewGuid():N}.db");

    /// <summary>
    /// Config overrides applied after the defaults; set via an object
    /// initializer before the first client is created. An empty string
    /// blanks a key even if user-secrets supplied a value for it.
    /// </summary>
    public Dictionary<string, string?> Overrides { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            var settings = new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = $"Data Source={_dbPath}",
                ["Jwt:Issuer"] = "test-issuer",
                ["Jwt:Audience"] = "test-audience",
                ["Jwt:Key"] = "integration-test-signing-key-at-least-32-characters",
                ["SeedOrganizer:Email"] = "organizer@uni-event.local",
                ["SeedOrganizer:Password"] = SeedOrganizerPassword
            };
            foreach (var (key, value) in Overrides)
            {
                settings[key] = value;
            }

            config.AddInMemoryCollection(settings);
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        try
        {
            File.Delete(_dbPath);
        }
        catch (IOException)
        {
            // Best-effort cleanup; the OS temp dir gets swept regardless.
        }
    }
}
