using System.ComponentModel.DataAnnotations;

namespace UniEvent.Api.Modules.Identity;

// Attributes target the primary-constructor parameters (not `[property: ...]`)
// because ASP.NET Core's model validator throws at request time for a
// record whose validation metadata sits on the property instead of the
// constructor parameter that actually binds the request body.
public record RegisterRequest([Required, EmailAddress] string Email, [Required, MinLength(8)] string Password);

public record LoginRequest([Required, EmailAddress] string Email, [Required] string Password);

public record AuthResponse(string Token, Guid UserId, string Email, string Role);

public record MeResponse(Guid UserId, string Email, string Role);
