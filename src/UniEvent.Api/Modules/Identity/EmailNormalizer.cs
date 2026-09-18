namespace UniEvent.Api.Modules.Identity;

/// <summary>
/// Single definition of how emails are canonicalized before they are
/// stored or looked up, so registration, login, and seeding can never
/// disagree about casing or whitespace.
/// </summary>
public static class EmailNormalizer
{
    public static string Normalize(string email) => email.Trim().ToLowerInvariant();
}
