using HRMS.Application.Common.Exceptions;
using HRMS.Application.DTOs.Auth;
using HRMS.Application.Interfaces;
using HRMS.Domain.Constants;
using HRMS.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace HRMS.Application.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ITokenService _tokenService;
    private readonly ICurrentUserService _currentUser;

    public AuthService(UserManager<ApplicationUser> userManager, ITokenService tokenService, ICurrentUserService currentUser)
    {
        _userManager = userManager;
        _tokenService = tokenService;
        _currentUser = currentUser;
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterRequestDto dto)
    {
        // Register is meant for brand-new, unauthenticated signups. HR accounts
        // are only ever created via POST /api/admin/hr-users by an Admin — an
        // already-authenticated HR caller has no business hitting this endpoint.
        // Anonymous callers (the normal case) and authenticated Admins are unaffected.
        if (_currentUser.IsAuthenticated && _currentUser.IsInRole(AppRoles.HR))
        {
            throw new ForbiddenAppException("HR users cannot access the registration endpoint.");
        }

        var existing = await _userManager.FindByEmailAsync(dto.Email);
        if (existing is not null)
        {
            throw new ConflictException("An account with this email already exists.");
        }

        var user = new ApplicationUser
        {
            UserName = dto.Email,
            Email = dto.Email,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            OrganizationId = null, // no org yet — created via POST /api/organizations
            RoleId = null,         // no role yet — never defaulted; set explicitly later
            IsActive = true
        };

        var result = await _userManager.CreateAsync(user, dto.Password);
        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description).ToList();
            throw new BadRequestException(string.Join(" ", errors));
        }

        // No role is assigned here. This account stays role-less until the user
        // takes an explicit action — creating an org (becomes Admin) or being
        // added by an Admin (becomes HR). Nothing defaults it to anything.
        var roles = Array.Empty<string>();
        var token = _tokenService.GenerateToken(user, roles);

        return MapToAuthResponse(user, roles, token);
    }

    public async Task<AuthResponseDto> LoginAsync(LoginRequestDto dto)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user is null || !user.IsActive)
        {
            throw new UnauthorizedAppException("Invalid email or password.");
        }

        // Respect the lockout window set by a failed-attempt streak (Identity's own
        // policy — see AddInfrastructureServices) OR by an Admin removing this HR
        // user (RemoveHrUserAsync locks them out until DateTimeOffset.MaxValue).
        if (_userManager.SupportsUserLockout && await _userManager.IsLockedOutAsync(user))
        {
            throw new UnauthorizedAppException("This account is locked due to multiple failed login attempts. Please try again later.");
        }

        var passwordValid = await _userManager.CheckPasswordAsync(user, dto.Password);
        if (!passwordValid)
        {
            // Previously nothing tracked failed attempts, so the configured
            // MaxFailedAccessAttempts lockout could never actually trigger.
            if (_userManager.SupportsUserLockout)
            {
                await _userManager.AccessFailedAsync(user);
            }
            throw new UnauthorizedAppException("Invalid email or password.");
        }

        if (_userManager.SupportsUserLockout)
        {
            await _userManager.ResetAccessFailedCountAsync(user);
        }

        var roles = await _userManager.GetRolesAsync(user);
        var token = _tokenService.GenerateToken(user, roles);

        return MapToAuthResponse(user, roles, token);
    }

    /// <summary>
    /// Diagnostic/self-check endpoint: loads the caller's row fresh from the
    /// database and returns their actual current OrganizationId/roles, so you
    /// can confirm what's really persisted instead of trusting a JWT that may
    /// have been issued before an org/role change (no new token is issued here).
    /// </summary>
    public async Task<AuthResponseDto> GetCurrentUserAsync()
    {
        var user = await _userManager.FindByIdAsync(_currentUser.UserId.ToString())
                   ?? throw new NotFoundException("User", _currentUser.UserId);

        var roles = await _userManager.GetRolesAsync(user);

        return new AuthResponseDto
        {
            UserId = user.Id,
            Email = user.Email!,
            FirstName = user.FirstName,
            LastName = user.LastName,
            OrganizationId = user.OrganizationId,
            RoleId = user.RoleId,
            Roles = roles.ToList(),
            Token = string.Empty,
            ExpiresAtUtc = default
        };
    }

    private static AuthResponseDto MapToAuthResponse(ApplicationUser user, IList<string> roles, GeneratedToken token) => new()
    {
        UserId = user.Id,
        Email = user.Email!,
        FirstName = user.FirstName,
        LastName = user.LastName,
        OrganizationId = user.OrganizationId,
        RoleId = user.RoleId,
        Roles = roles.ToList(),
        Token = token.Token,
        ExpiresAtUtc = token.ExpiresAtUtc
    };
}
