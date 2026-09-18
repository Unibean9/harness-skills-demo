using System.ComponentModel.DataAnnotations;

namespace UniEvent.Api.Modules.Identity;

/// <summary>
/// Binds the "Jwt" configuration section. Key is intentionally never given
/// a default here - it must come from user-secrets or an environment
/// variable, never be committed (see appsettings.Development.json.example).
/// [Required] here (not just the `required` modifier) is what makes
/// ValidateDataAnnotations().ValidateOnStart() actually fail fast at
/// startup if Key is missing, since config binding sets properties via
/// reflection and does not honor `required` on its own.
/// </summary>
public class JwtOptions
{
    public const string SectionName = "Jwt";

    [Required] public required string Issuer { get; set; }

    [Required] public required string Audience { get; set; }

    [Required, MinLength(32)] public required string Key { get; set; }

    public int ExpiryMinutes { get; set; } = 60;
}
