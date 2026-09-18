using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Options;
using UniEvent.Api.Modules.Identity;

namespace UniEvent.Api.Tests;

public class JwtTokenServiceTests
{
    private static JwtTokenService CreateService() => new(Options.Create(new JwtOptions
    {
        Issuer = "test-issuer",
        Audience = "test-audience",
        Key = "unit-test-signing-key-at-least-32-characters-long",
        ExpiryMinutes = 15
    }));

    [Fact]
    public void CreateToken_EmbedsUserIdEmailAndRoleClaims()
    {
        var service = CreateService();
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "organizer@uni-event.local",
            Role = Role.Organizer,
            PasswordHash = "irrelevant-for-this-test"
        };

        var token = service.CreateToken(user);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.Equal(user.Id.ToString(), jwt.Claims.Single(c => c.Type == ClaimTypes.NameIdentifier).Value);
        Assert.Equal(user.Email, jwt.Claims.Single(c => c.Type == ClaimTypes.Email).Value);
        Assert.Equal(Role.Organizer.ToString(), jwt.Claims.Single(c => c.Type == ClaimTypes.Role).Value);
        Assert.Equal("test-issuer", jwt.Issuer);
        Assert.Contains("test-audience", jwt.Audiences);
    }

    [Fact]
    public void CreateToken_SetsExpiryAccordingToConfiguredLifetime()
    {
        var service = CreateService();
        var user = new User { Email = "a@b.com", Role = Role.Participant, PasswordHash = "x" };

        var before = DateTime.UtcNow;
        var token = service.CreateToken(user);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        var expectedExpiry = before.AddMinutes(15);
        Assert.InRange(jwt.ValidTo, expectedExpiry.AddSeconds(-30), expectedExpiry.AddSeconds(30));
    }

    [Fact]
    public void CreateToken_TwoTokensForSameUser_HaveDifferentJtiClaims()
    {
        var service = CreateService();
        var user = new User { Email = "a@b.com", Role = Role.Staff, PasswordHash = "x" };

        var token1 = new JwtSecurityTokenHandler().ReadJwtToken(service.CreateToken(user));
        var token2 = new JwtSecurityTokenHandler().ReadJwtToken(service.CreateToken(user));

        var jti1 = token1.Claims.Single(c => c.Type == JwtRegisteredClaimNames.Jti).Value;
        var jti2 = token2.Claims.Single(c => c.Type == JwtRegisteredClaimNames.Jti).Value;
        Assert.NotEqual(jti1, jti2);
    }
}
