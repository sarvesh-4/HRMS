using HRMS.Application.DTOs.Auth;

namespace HRMS.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResponseDto> RegisterAsync(RegisterRequestDto dto);
    Task<AuthResponseDto> LoginAsync(LoginRequestDto dto);

    /// <summary>
    /// Reads the current user's Organization/role state straight from the
    /// database (not from the JWT) — useful for confirming what's actually
    /// persisted, independent of whether the caller's token is stale.
    /// </summary>
    Task<AuthResponseDto> GetCurrentUserAsync();
}
