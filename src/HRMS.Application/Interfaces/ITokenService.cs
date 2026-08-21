using HRMS.Domain.Entities;

namespace HRMS.Application.Interfaces;

public record GeneratedToken(string Token, DateTime ExpiresAtUtc);

public interface ITokenService
{
    /// <summary>Builds a signed JWT carrying the user id, email, org id (if any) and role claims.</summary>
    GeneratedToken GenerateToken(ApplicationUser user, IList<string> roles);
}
