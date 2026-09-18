using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniEvent.Api.Data;

namespace UniEvent.Api.Modules.Identity;

[ApiController]
[Route("api/auth")]
public class AuthController(AppDbContext db, IJwtTokenService jwtTokenService) : ControllerBase
{
    private static readonly PasswordHasher<User> PasswordHasher = new();

    /// <summary>
    /// Public self-registration always creates a Participant. Organizer and
    /// Staff accounts are provisioned separately (seed / future admin flow),
    /// never through this endpoint.
    /// </summary>
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        if (await db.Users.AnyAsync(u => u.Email == normalizedEmail))
        {
            return Conflict(new { message = "Email is already registered." });
        }

        var user = new User
        {
            Email = normalizedEmail,
            Role = Role.Participant,
            PasswordHash = string.Empty
        };
        user.PasswordHash = PasswordHasher.HashPassword(user, request.Password);

        db.Users.Add(user);
        await db.SaveChangesAsync();

        var token = jwtTokenService.CreateToken(user);
        return Created(string.Empty, new AuthResponse(token, user.Id, user.Email, user.Role.ToString()));
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await db.Users.SingleOrDefaultAsync(u => u.Email == normalizedEmail);

        if (user is null)
        {
            return Unauthorized(new { message = "Invalid email or password." });
        }

        var verification = PasswordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (verification == PasswordVerificationResult.Failed)
        {
            return Unauthorized(new { message = "Invalid email or password." });
        }

        var token = jwtTokenService.CreateToken(user);
        return Ok(new AuthResponse(token, user.Id, user.Email, user.Role.ToString()));
    }

    [HttpGet("me")]
    [Authorize]
    public ActionResult<MeResponse> Me()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var email = User.FindFirstValue(ClaimTypes.Email);
        var role = User.FindFirstValue(ClaimTypes.Role);

        if (userId is null || email is null || role is null)
        {
            return Unauthorized();
        }

        return Ok(new MeResponse(Guid.Parse(userId), email, role));
    }

    /// <summary>
    /// Role-authorization smoke test: only an Organizer token may call this.
    /// Exists to prove [Authorize(Roles = "...")] actually enforces role
    /// boundaries (see phase-01 acceptance criteria).
    /// </summary>
    [HttpGet("organizer-only")]
    [Authorize(Roles = "Organizer")]
    public ActionResult OrganizerOnly()
    {
        return Ok(new { message = "OK" });
    }
}
