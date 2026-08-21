using HRMS.Application.Common.Exceptions;
using HRMS.Application.DTOs.Employee;
using HRMS.Application.Interfaces;
using HRMS.Domain.Constants;
using HRMS.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace HRMS.Application.Services;

public class EmployeeService : IEmployeeService
{
    private readonly IRepository<Employee> _employeeRepository;         // EF Core — writes
    private readonly IEmployeeRepository _employeeReadRepository;       // Dapper — reads
    private readonly IRepository<Organization> _organizationRepository; // EF Core — ownership checks
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;
    private readonly ICurrentUserService _currentUser;

    public EmployeeService(
        IRepository<Employee> employeeRepository,
        IEmployeeRepository employeeReadRepository,
        IRepository<Organization> organizationRepository,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole<Guid>> roleManager,
        ICurrentUserService currentUser)
    {
        _employeeRepository = employeeRepository;
        _employeeReadRepository = employeeReadRepository;
        _organizationRepository = organizationRepository;
        _userManager = userManager;
        _roleManager = roleManager;
        _currentUser = currentUser;
    }

    public async Task<EmployeeResponseDto> CreateEmployeeAsync(CreateEmployeeDto dto)
    {
        var organizationId = RequireOrganization();
        var employeeRole = await EnsureRoleExistsAsync(AppRoles.Employee);

        var employee = new Employee
        {
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Email = dto.Email,
            PhoneNumber = dto.PhoneNumber,
            Address = dto.Address,
            OrganizationId = organizationId,
            CreatedByUserId = _currentUser.UserId,
            RoleId = employeeRole.Id
        };

        await _employeeRepository.AddAsync(employee);
        await _employeeRepository.SaveChangesAsync();

        return await MapToDtoAsync(employee);
    }

    public async Task<EmployeeResponseDto> UpdateEmployeeAsync(Guid id, UpdateEmployeeDto dto)
    {
        var employee = await GetOwnedEmployeeOrThrowAsync(id, forWrite: true);

        employee.FirstName = dto.FirstName;
        employee.LastName = dto.LastName;
        employee.Email = dto.Email;
        employee.PhoneNumber = dto.PhoneNumber;
        employee.Address = dto.Address;
        employee.UpdatedAt = DateTime.UtcNow;

        _employeeRepository.Update(employee);
        await _employeeRepository.SaveChangesAsync();

        return await MapToDtoAsync(employee);
    }

    public async Task DeleteEmployeeAsync(Guid id)
    {
        var employee = await GetOwnedEmployeeOrThrowAsync(id, forWrite: true);

        _employeeRepository.Remove(employee);
        await _employeeRepository.SaveChangesAsync();
    }

    public async Task<EmployeeResponseDto> GetByIdAsync(Guid id)
    {
        var employee = await GetOwnedEmployeeOrThrowAsync(id, forWrite: false);
        return await MapToDtoAsync(employee);
    }

    public async Task<List<EmployeeResponseDto>> GetAllAsync()
    {
        var organizationId = RequireOrganization();

        IEnumerable<Employee> employees;
        if (_currentUser.IsInRole(AppRoles.Admin))
        {
            // Admin sees every employee in the organization, including ones HR created.
            employees = await _employeeReadRepository.GetByOrganizationAsync(organizationId);
        }
        else
        {
            // HR sees only the employees they personally created.
            employees = await _employeeReadRepository.GetByCreatedByAsync(_currentUser.UserId);
        }

        var result = new List<EmployeeResponseDto>();
        foreach (var employee in employees)
        {
            result.Add(await MapToDtoAsync(employee));
        }
        return result;
    }

    public async Task<List<StaffMemberDto>> GetAllStaffAsync(Guid? organizationId, StaffType staffType)
    {
        if (!_currentUser.IsInRole(AppRoles.Admin))
        {
            throw new ForbiddenAppException("Only an Admin can view the combined staff list.");
        }

        var ownedOrgIds = await GetOwnedOrganizationIdsAsync();
        if (ownedOrgIds.Count == 0)
        {
            throw new BadRequestException("You must create an organization before viewing staff.");
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
            // For an Admin who only ever created one organization, this is identical
            // to filtering by that single org, so single-org Admins see no change.
            targetOrgIds = ownedOrgIds;
        }

        var staff = new List<StaffMemberDto>();

        if (staffType is StaffType.All or StaffType.HR)
        {
            var hrUsers = _userManager.Users
                .Where(u => u.OrganizationId != null && targetOrgIds.Contains(u.OrganizationId.Value))
                .ToList();

            foreach (var user in hrUsers)
            {
                if (await _userManager.IsInRoleAsync(user, AppRoles.HR))
                {
                    staff.Add(MapHrUserToStaffDto(user));
                }
            }
        }

        if (staffType is StaffType.All or StaffType.Employee)
        {
            foreach (var orgId in targetOrgIds)
            {
                var employees = await _employeeReadRepository.GetByOrganizationAsync(orgId);
                foreach (var employee in employees)
                {
                    staff.Add(MapEmployeeToStaffDto(employee));
                }
            }
        }

        return staff
            .OrderBy(s => s.OrganizationId)
            .ThenBy(s => s.StaffType)
            .ThenBy(s => s.CreatedAt)
            .ToList();
    }

    public async Task<StaffMemberDto> GetStaffByIdAsync(Guid id)
    {
        if (!_currentUser.IsInRole(AppRoles.Admin))
        {
            throw new ForbiddenAppException("Only an Admin can search the combined staff list by id.");
        }

        var ownedOrgIds = await GetOwnedOrganizationIdsAsync();
        if (ownedOrgIds.Count == 0)
        {
            throw new BadRequestException("You must create an organization before viewing staff.");
        }

        // Check Employee records first.
        var employee = await _employeeRepository.GetByIdAsync(id);
        if (employee is not null && ownedOrgIds.Contains(employee.OrganizationId))
        {
            return MapEmployeeToStaffDto(employee);
        }

        // Not an employee (or not in an org this Admin owns) — check HR accounts.
        var hrUser = await _userManager.FindByIdAsync(id.ToString());
        if (hrUser is not null
            && hrUser.OrganizationId is not null
            && ownedOrgIds.Contains(hrUser.OrganizationId.Value)
            && await _userManager.IsInRoleAsync(hrUser, AppRoles.HR))
        {
            return MapHrUserToStaffDto(hrUser);
        }

        throw new NotFoundException("Staff member", id);
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

    private static StaffMemberDto MapHrUserToStaffDto(ApplicationUser user) => new()
    {
        Id = user.Id,
        StaffType = AppRoles.HR,
        FirstName = user.FirstName,
        LastName = user.LastName,
        Email = user.Email!,
        OrganizationId = user.OrganizationId!.Value,
        CreatedByUserId = user.CreatedByUserId,
        RoleId = user.RoleId,
        IsActive = user.IsActive,
        CreatedAt = user.CreatedAt
    };

    private static StaffMemberDto MapEmployeeToStaffDto(Employee employee) => new()
    {
        Id = employee.Id,
        StaffType = "Employee",
        FirstName = employee.FirstName,
        LastName = employee.LastName,
        Email = employee.Email,
        PhoneNumber = employee.PhoneNumber,
        Address = employee.Address,
        OrganizationId = employee.OrganizationId,
        CreatedByUserId = employee.CreatedByUserId,
        RoleId = employee.RoleId,
        CreatedAt = employee.CreatedAt
    };

    /// <summary>
    /// Central authorization point: fetches an employee and enforces
    /// org scoping + HR "own records only" rule for write operations.
    /// </summary>
    private async Task<Employee> GetOwnedEmployeeOrThrowAsync(Guid id, bool forWrite)
    {
        var organizationId = RequireOrganization();

        var employee = await _employeeRepository.GetByIdAsync(id)
                        ?? throw new NotFoundException("Employee", id);

        if (employee.OrganizationId != organizationId)
        {
            throw new NotFoundException("Employee", id);
        }

        var isAdmin = _currentUser.IsInRole(AppRoles.Admin);
        var isOwner = employee.CreatedByUserId == _currentUser.UserId;

        if (forWrite)
        {
            // Only the HR user who created the record may edit/delete it (Admin cannot per spec).
            if (!isOwner)
            {
                throw new ForbiddenAppException("You can only modify employees you created.");
            }
        }
        else
        {
            // Read: Admin can view anyone in the org; HR can only view their own.
            if (!isAdmin && !isOwner)
            {
                throw new ForbiddenAppException("You can only view employees you created.");
            }
        }

        return employee;
    }

    private Guid RequireOrganization()
    {
        return _currentUser.OrganizationId
               ?? throw new BadRequestException("You must belong to an organization to manage employees.");
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

    private async Task<EmployeeResponseDto> MapToDtoAsync(Employee employee)
    {
        var creator = await _userManager.FindByIdAsync(employee.CreatedByUserId.ToString());

        return new EmployeeResponseDto
        {
            Id = employee.Id,
            FirstName = employee.FirstName,
            LastName = employee.LastName,
            Email = employee.Email,
            PhoneNumber = employee.PhoneNumber,
            Address = employee.Address,
            OrganizationId = employee.OrganizationId,
            CreatedByUserId = employee.CreatedByUserId,
            CreatedByName = creator is null ? null : $"{creator.FirstName} {creator.LastName}",
            RoleId = employee.RoleId,
            CreatedAt = employee.CreatedAt
        };
    }
}
