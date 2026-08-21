using HRMS.Application.Common.Exceptions;
using HRMS.Application.DTOs.HrUser;
using HRMS.Application.Interfaces;
using HRMS.Domain.Constants;
using HRMS.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace HRMS.Application.Services;

public class HrUserService : IHrUserService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;
    private readonly IRepository<Organization> _organizationRepository;
    private readonly ICurrentUserService _currentUser;

    public HrUserService(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole<Guid>> roleManager,
        IRepository<Organization> organizationRepository,
        ICurrentUserService currentUser)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _organizationRepository = organizationRepository;
        _currentUser = currentUser;
    }

    public async Task<HrUserResponseDto> CreateHrUserAsync(CreateHrUserDto dto)
    {
        var organizationId = await ResolveTargetOrganizationAsync(dto.OrganizationId);
        var hrRole = await EnsureRoleExistsAsync(AppRoles.HR);

        var existing = await _userManager.FindByEmailAsync(dto.Email);
        if (existing is not null)
        {
            throw new ConflictException("An account with this email already exists.");
        }

        var hrUser = new ApplicationUser
        {
            UserName = dto.Email,
            Email = dto.Email,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            OrganizationId = organizationId,
            CreatedByUserId = _currentUser.UserId,
            IsActive = true
        };

        var createResult = await _userManager.CreateAsync(hrUser, dto.Password);
        if (!createResult.Succeeded)
        {
            throw new BadRequestException(string.Join(" ", createResult.Errors.Select(e => e.Description)));
        }

        // Explicit follow-up update: guarantees OrganizationId / CreatedByUserId /
        // RoleId are persisted (same pattern used in OrganizationService), instead
        // of relying solely on values set on the object passed into CreateAsync.
        // RoleId is never defaulted — it's explicitly the HR role's Id, looked up above.
        hrUser.OrganizationId = organizationId;
        hrUser.CreatedByUserId = _currentUser.UserId;
        hrUser.RoleId = hrRole.Id;
        var linkResult = await _userManager.UpdateAsync(hrUser);
        if (!linkResult.Succeeded)
        {
            throw new BadRequestException(
                "HR user was created but could not be linked to the organization: " +
                string.Join(" ", linkResult.Errors.Select(e => e.Description)));
        }

        var roleResult = await _userManager.AddToRoleAsync(hrUser, AppRoles.HR);
        if (!roleResult.Succeeded)
        {
            throw new BadRequestException(
                "HR user was created but the HR role could not be assigned: " +
                string.Join(" ", roleResult.Errors.Select(e => e.Description)));
        }

        // Reload from the store so the response reflects exactly what is in the
        // database right now, not just the in-memory object.
        var persisted = await _userManager.FindByIdAsync(hrUser.Id.ToString())
                         ?? throw new BadRequestException("HR user was created but could not be re-read.");

        return MapToDto(persisted);
    }

    public async Task RemoveHrUserAsync(Guid hrUserId)
    {
        var ownedOrgIds = await GetOwnedOrganizationIdsAsync();

        var hrUser = await _userManager.FindByIdAsync(hrUserId.ToString())
                     ?? throw new NotFoundException("HR user", hrUserId);

        if (hrUser.OrganizationId is null || !ownedOrgIds.Contains(hrUser.OrganizationId.Value))
        {
            throw new ForbiddenAppException("This HR user does not belong to an organization you own.");
        }

        if (!await _userManager.IsInRoleAsync(hrUser, AppRoles.HR))
        {
            throw new BadRequestException("The specified user is not an HR user.");
        }

        if (!hrUser.IsActive)
        {
            throw new BadRequestException("This HR user has already been removed.");
        }

        // Soft-remove: deactivate + lock out indefinitely instead of hard delete,
        // since employees created by this HR user still reference their Id.
        hrUser.IsActive = false;
        await _userManager.UpdateAsync(hrUser);
        await _userManager.SetLockoutEnabledAsync(hrUser, true);
        await _userManager.SetLockoutEndDateAsync(hrUser, DateTimeOffset.MaxValue);
    }

    public async Task<List<HrUserResponseDto>> GetHrUsersAsync(Guid? organizationId = null)
    {
        var ownedOrgIds = await GetOwnedOrganizationIdsAsync();
        if (ownedOrgIds.Count == 0)
        {
            throw new BadRequestException("You must create an organization before managing HR users.");
        }

        List<Guid> targetOrgIds;
        if (organizationId is not null)
        {
            if (!ownedOrgIds.Contains(organizationId.Value))
            {
                throw new ForbiddenAppException("You do not own this organization.");
            }
            targetOrgIds = new List<Guid> { organizationId.Value };
        }
        else
        {
            // No organization specified — aggregate across every org this Admin owns.
            // For a single-org Admin this is identical to filtering by that one org.
            targetOrgIds = ownedOrgIds;
        }

        // Filter by organization AND active status up front — a removed HR user
        // (IsActive = false, set by RemoveHrUserAsync) must not reappear here,
        // otherwise DELETE looks like it silently did nothing.
        var usersInOrgs = _userManager.Users
            .Where(u => u.OrganizationId != null && targetOrgIds.Contains(u.OrganizationId.Value) && u.IsActive)
            .ToList();

        var hrUsers = new List<HrUserResponseDto>();
        foreach (var user in usersInOrgs)
        {
            if (await _userManager.IsInRoleAsync(user, AppRoles.HR))
            {
                hrUsers.Add(MapToDto(user));
            }
        }

        return hrUsers.OrderBy(u => u.OrganizationId).ThenBy(u => u.CreatedAt).ToList();
    }

    /// <summary>
    /// Picks which organization a new HR user belongs to: the explicitly requested
    /// one (validated as owned by the caller) or, if none was given, the caller's
    /// primary organization — identical to the original single-org behavior.
    /// </summary>
    private async Task<Guid> ResolveTargetOrganizationAsync(Guid? requestedOrganizationId)
    {
        if (requestedOrganizationId is null)
        {
            return _currentUser.OrganizationId
                   ?? throw new BadRequestException("You must create an organization before managing HR users.");
        }

        var ownedOrgIds = await GetOwnedOrganizationIdsAsync();
        if (!ownedOrgIds.Contains(requestedOrganizationId.Value))
        {
            throw new ForbiddenAppException("You do not own this organization.");
        }

        return requestedOrganizationId.Value;
    }

    /// <summary>
    /// Organizations this Admin owns (created). POC-scale: pulls every org and
    /// filters in memory — fine at small scale; for a larger dataset this should
    /// become a direct indexed query (e.g. WHERE CreatedByUserId = @id) instead.
    /// </summary>
    private async Task<List<Guid>> GetOwnedOrganizationIdsAsync()
    {
        var allOrganizations = await _organizationRepository.GetAllAsync();
        return allOrganizations
            .Where(o => o.CreatedByUserId == _currentUser.UserId)
            .Select(o => o.Id)
            .ToList();
    }

    private async Task<IdentityRole<Guid>> EnsureRoleExistsAsync(string roleName)
    {
        var role = await _roleManager.FindByNameAsync(roleName);
        if (role is not null)
        {
            return role;
        }

        role = new IdentityRole<Guid>(roleName);
        var result = await _roleManager.CreateAsync(role);
        if (!result.Succeeded)
        {
            throw new BadRequestException(
                $"Failed to create role '{roleName}': " + string.Join(" ", result.Errors.Select(e => e.Description)));
        }

        return role;
    }

    private static HrUserResponseDto MapToDto(ApplicationUser user) => new()
    {
        Id = user.Id,
        FirstName = user.FirstName,
        LastName = user.LastName,
        Email = user.Email!,
        OrganizationId = user.OrganizationId ?? Guid.Empty,
        RoleId = user.RoleId,
        IsActive = user.IsActive,
        CreatedAt = user.CreatedAt
    };
}
