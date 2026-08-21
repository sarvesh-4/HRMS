using FluentValidation;
using HRMS.Application.Common;
using HRMS.Application.Common.Exceptions;
using HRMS.Application.DTOs.Employee;
using HRMS.Application.Interfaces;
using HRMS.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRMS.API.Controllers;

/// <summary>
/// Employee CRUD. Create/Update/Delete are HR-only and scoped to records the HR
/// user created themselves. Read (list/get) is available to both Admin (sees all
/// employees in the org) and HR (sees only their own) — the service layer decides
/// which set to return based on the caller's role.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EmployeesController : ControllerBase
{
    private readonly IEmployeeService _employeeService;
    private readonly IValidator<CreateEmployeeDto> _createValidator;
    private readonly IValidator<UpdateEmployeeDto> _updateValidator;

    public EmployeesController(
        IEmployeeService employeeService,
        IValidator<CreateEmployeeDto> createValidator,
        IValidator<UpdateEmployeeDto> updateValidator)
    {
        _employeeService = employeeService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    /// <summary>HR only. Creates an employee owned by the current HR user.</summary>
    [HttpPost]
    [Authorize(Roles = AppRoles.HR)]
    [ProducesResponseType(typeof(ApiResponse<EmployeeResponseDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateEmployeeDto dto)
    {
        await _createValidator.ValidateAndThrowAsync(dto);

        var result = await _employeeService.CreateEmployeeAsync(dto);
        return StatusCode(StatusCodes.Status201Created,
            ApiResponse<EmployeeResponseDto>.SuccessResponse(result, "Employee created."));
    }

    /// <summary>HR only. Can only update employees the caller created.</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = AppRoles.HR)]
    [ProducesResponseType(typeof(ApiResponse<EmployeeResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateEmployeeDto dto)
    {
        await _updateValidator.ValidateAndThrowAsync(dto);

        var result = await _employeeService.UpdateEmployeeAsync(id, dto);
        return Ok(ApiResponse<EmployeeResponseDto>.SuccessResponse(result, "Employee updated."));
    }

    /// <summary>HR only. Can only delete employees the caller created.</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = AppRoles.HR)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _employeeService.DeleteEmployeeAsync(id);
        return Ok(ApiResponse<object>.SuccessResponse(new { }, "Employee deleted."));
    }

    /// <summary>
    /// Admin: combined staff directory — HR accounts AND Employee records, across
    /// one or all organizations the Admin owns. Supports two optional query filters:
    /// <c>staffType</c> (All | HR | Employee — defaults to All) and
    /// <c>organizationId</c> (narrows to one org; omit to see every org you own).
    /// HR: unchanged — returns only the employees the caller created, same as
    /// before. HR is not permitted to use the Admin-only filters at all — supplying
    /// either one returns 403, rather than silently ignoring them.
    /// </summary>
    [HttpGet]
    [Authorize(Roles = AppRoles.Admin + "," + AppRoles.HR)]
    [ProducesResponseType(typeof(ApiResponse<List<EmployeeResponseDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] Guid? organizationId, [FromQuery] string? staffType)
    {
        var isAdmin = User.IsInRole(AppRoles.Admin);

        if (!isAdmin && (organizationId.HasValue || !string.IsNullOrWhiteSpace(staffType)))
        {
            throw new ForbiddenAppException(
                "HR users cannot use the organizationId or staffType filters — those are Admin-only.");
        }

        if (isAdmin)
        {
            var parsedStaffType = ParseStaffType(staffType);
            var staffResult = await _employeeService.GetAllStaffAsync(organizationId, parsedStaffType);
            return Ok(ApiResponse<List<StaffMemberDto>>.SuccessResponse(staffResult));
        }

        // HR path — identical to the original behavior, no filters applied.
        var result = await _employeeService.GetAllAsync();
        return Ok(ApiResponse<List<EmployeeResponseDto>>.SuccessResponse(result));
    }

    private static StaffType ParseStaffType(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return StaffType.All;
        }

        return Enum.TryParse<StaffType>(value, ignoreCase: true, out var parsed)
            ? parsed
            : throw new BadRequestException(
                $"Invalid staffType '{value}'. Valid values are: All, HR, Employee.");
    }

    /// <summary>
    /// Admin: searches BOTH HR accounts and Employee records for this id, across
    /// any organization the Admin owns — same scope as the combined GET /api/employees
    /// list, narrowed to one id.
    /// HR: unchanged — only if the caller created that employee record.
    /// </summary>
    [HttpGet("{id:guid}")]
    [Authorize(Roles = AppRoles.Admin + "," + AppRoles.HR)]
    [ProducesResponseType(typeof(ApiResponse<EmployeeResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetById(Guid id)
    {
        if (User.IsInRole(AppRoles.Admin))
        {
            var staffResult = await _employeeService.GetStaffByIdAsync(id);
            return Ok(ApiResponse<StaffMemberDto>.SuccessResponse(staffResult));
        }

        // HR path — identical to the original behavior.
        var result = await _employeeService.GetByIdAsync(id);
        return Ok(ApiResponse<EmployeeResponseDto>.SuccessResponse(result));
    }
}
