using HRMS.Application.Common.Exceptions;
using HRMS.Application.DTOs.Organization;
using HRMS.Application.Interfaces;
using HRMS.Domain.Constants;
using HRMS.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace HRMS.Application.Services;

public class OrganizationService : IOrganizationService
{
    private readonly IRepository<Organization> _organizationRepository;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;
    private readonly ITokenService _tokenService;
    private readonly ICurrentUserService _currentUser;

    public OrganizationService(
        IRepository<Organization> organizationRepository,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole<Guid>> roleManager,
        ITokenService tokenService,
        ICurrentUserService currentUser)
    {
        _organizationRepository = organizationRepository;
        _userManager = userManager;
        _roleManager = roleManager;
        _tokenService = tokenService;
        _currentUser = currentUser;
    }

    public async Task<OrganizationResponseDto> CreateOrganizationAsync(CreateOrganizationDto dto)
    {
        var user = await _userManager.FindByIdAsync(_currentUser.UserId.ToString())
                   ?? throw new NotFoundException("User", _currentUser.UserId);

        var isFirstOrganization = user.OrganizationId is null;

        if (!isFirstOrganization)
        {
            // Already belongs to an org. Only an existing Admin may create an
            // ADDITIONAL organization (real-world: one Admin can own several
            // companies). Anyone else (HR) stays blocked, same as before —
            // just with a more accurate error than the old generic "conflict".
            var isAdmin = await _userManager.IsInRoleAsync(user, AppRoles.Admin);
            if (!isAdmin)
            {
                throw new ForbiddenAppException("Only an Admin can create additional organizations.");
            }
        }

        var organization = new Organization
        {
            Name = dto.Name,
            Address = dto.Address,
            CreatedByUserId = user.Id
        };

        await _organizationRepository.AddAsync(organization);
        await _organizationRepository.SaveChangesAsync();

        var adminRole = await EnsureRoleExistsAsync(AppRoles.Admin);

        if (isFirstOrganization)
        {
            // First organization: attach it as the user's primary org and promote to Admin.
            // Additional organizations for an existing Admin do NOT change their
            // primary OrganizationId — that stays as their first/default org, and
            // ownership of every org they've created is tracked via CreatedByUserId
            // instead (see EmployeeService/HrUserService "owned organizations" checks).
            user.OrganizationId = organization.Id;

            // A self-registered user who creates their own org has no external
            // creator — reference themselves instead of leaving this null, so
            // CreatedByUserId is always populated and every account is traceable.
            user.CreatedByUserId ??= user.Id;

            // Explicit role assignment — RoleId is never defaulted; this is the
            // one place it becomes non-null for a self-onboarded Admin.
            user.RoleId = adminRole.Id;

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                throw new BadRequestException("Failed to attach user to the new organization.");
            }

            if (!await _userManager.IsInRoleAsync(user, AppRoles.Admin))
            {
                await _userManager.AddToRoleAsync(user, AppRoles.Admin);
            }
        }

        // Roles/OrganizationId may have changed (first org only), so a fresh token
        // is issued either way — harmless to reissue even when nothing changed.
        var roles = await _userManager.GetRolesAsync(user);
        var token = _tokenService.GenerateToken(user, roles);

        return new OrganizationResponseDto
        {
            Id = organization.Id,
            Name = organization.Name,
            Address = organization.Address,
            CreatedByUserId = organization.CreatedByUserId,
            CreatedAt = organization.CreatedAt,
            Token = token.Token,
            ExpiresAtUtc = token.ExpiresAtUtc
        };
    }

    public async Task<OrganizationResponseDto> GetMyOrganizationAsync()
    {
        if (_currentUser.OrganizationId is null)
        {
            throw new NotFoundException("You do not belong to an organization yet.");
        }

        var organization = await _organizationRepository.GetByIdAsync(_currentUser.OrganizationId.Value)
                            ?? throw new NotFoundException("Organization", _currentUser.OrganizationId.Value);

        return new OrganizationResponseDto
        {
            Id = organization.Id,
            Name = organization.Name,
            Address = organization.Address,
            CreatedByUserId = organization.CreatedByUserId,
            CreatedAt = organization.CreatedAt
        };
    }

    /// <summary>
    /// All organizations owned by the current user — for a single-org Admin this
    /// returns exactly one organization (their primary one), same as GetMyOrganizationAsync.
    /// For a multi-org Admin, returns every organization they've created.
    /// </summary>
    public async Task<List<OrganizationResponseDto>> GetMyOrganizationsAsync()
    {
        // Only Admins own organizations. HR calling this previously got a
        // misleading 200 with an empty array (since they own zero orgs) —
        // that's now an explicit 403 instead.
        if (!_currentUser.IsInRole(AppRoles.Admin))
        {
            throw new ForbiddenAppException("Only an Admin can list organizations.");
        }

        var allOrganizations = await _organizationRepository.GetAllAsync();

        return allOrganizations
            .Where(o => o.CreatedByUserId == _currentUser.UserId)
            .OrderBy(o => o.CreatedAt)
            .Select(o => new OrganizationResponseDto
            {
                Id = o.Id,
                Name = o.Name,
                Address = o.Address,
                CreatedByUserId = o.CreatedByUserId,
                CreatedAt = o.CreatedAt
            })
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
}
