using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using UniEvent.Api.Modules.Identity;

namespace UniEvent.Api.Data;

/// <summary>
/// Seeds the one Organizer account the system needs at first run, since
/// Organizer/Staff accounts are never created through public registration
/// (see AuthController). Runs once at startup and is a no-op once any
/// Organizer already exists.
/// </summary>
public static class SeedData
{
    public const string DefaultOrganizerEmail = "organizer@uni-event.local";

    public static async Task EnsureDefaultOrganizerAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(SeedData));

        await db.Database.MigrateAsync();

        if (await db.Users.AnyAsync(u => u.Role == Role.Organizer))
        {
            return;
        }

        // No built-in password: a default that lives in the repo would be a
        // working Organizer login on any deploy that forgot to override it.
        var password = config["SeedOrganizer:Password"];
        if (string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning(
                "No Organizer exists and SeedOrganizer:Password is not configured; skipping Organizer seed. " +
                "Set it via user-secrets or an environment variable (SeedOrganizer__Password).");
            return;
        }

        var email = EmailNormalizer.Normalize(config["SeedOrganizer:Email"] ?? DefaultOrganizerEmail);

        var organizer = new User
        {
            Email = email,
            Role = Role.Organizer,
            PasswordHash = string.Empty
        };
        organizer.PasswordHash = new PasswordHasher<User>().HashPassword(organizer, password);

        db.Users.Add(organizer);
        await db.SaveChangesAsync();
    }
}
