namespace UniEvent.Api.Modules.Identity;

/// <summary>
/// A registered account. Password is stored hashed via
/// <see cref="Microsoft.AspNetCore.Identity.PasswordHasher{TUser}"/> — never
/// hash it manually.
/// </summary>
public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public required string Email { get; set; }

    public required string PasswordHash { get; set; }

    public Role Role { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
