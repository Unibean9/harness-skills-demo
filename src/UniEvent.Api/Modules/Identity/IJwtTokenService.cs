namespace UniEvent.Api.Modules.Identity;

public interface IJwtTokenService
{
    /// <summary>Issues a signed JWT carrying the user's id, email, and role claims.</summary>
    string CreateToken(User user);
}
