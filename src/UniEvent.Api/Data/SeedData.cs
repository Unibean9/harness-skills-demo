using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
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
    public static async Task EnsureDefaultOrganizerAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        await db.Database.MigrateAsync();

        if (await db.Users.AnyAsync(u => u.Role == Role.Organizer))
        {
            return;
        }

        var email = config["SeedOrganizer:Email"] ?? "organizer@uni-event.local";
        // Demo-only default; not a production secret. Override via
        // SeedOrganizer:Password (user-secrets/env) before any real deploy.
        var password = config["SeedOrganizer:Password"] ?? "ChangeMe!Organizer1";

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
