using FluentValidation;
using HRMS.Application.Common;
using HRMS.Application.DTOs.HrUser;
using HRMS.Application.Interfaces;
using HRMS.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRMS.API.Controllers;

/// <summary>
/// Admin-only endpoints: add/remove HR users under any organization the Admin
/// owns (an Admin may own more than one — see IOrganizationService). Combined
/// employee/HR "staff" visibility is served from GET /api/employees — see
/// EmployeesController, which branches Admin vs HR internally.
/// </summary>
[ApiController]
[Route("api/admin/hr-users")]
[Authorize(Roles = AppRoles.Admin)]
public class AdminController : ControllerBase
{
    private readonly IHrUserService _hrUserService;
    private readonly IValidator<CreateHrUserDto> _validator;

    public AdminController(IHrUserService hrUserService, IValidator<CreateHrUserDto> validator)
    {
        _hrUserService = hrUserService;
        _validator = validator;
    }

    /// <summary>Admin creates a new HR user under their organization.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<HrUserResponseDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateHrUser([FromBody] CreateHrUserDto dto)
    {
        await _validator.ValidateAndThrowAsync(dto);

        var result = await _hrUserService.CreateHrUserAsync(dto);
        return StatusCode(StatusCodes.Status201Created,
            ApiResponse<HrUserResponseDto>.SuccessResponse(result, "HR user created."));
    }

    /// <summary>
    /// Admin lists HR users. Optional <c>organizationId</c> query param narrows
    /// the list to one organization the Admin owns; omit it to see HR users
    /// across every organization the Admin owns (identical to "just my org" for
    /// an Admin who only owns one).
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<HrUserResponseDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetHrUsers([FromQuery] Guid? organizationId)
    {
        var result = await _hrUserService.GetHrUsersAsync(organizationId);
        return Ok(ApiResponse<List<HrUserResponseDto>>.SuccessResponse(result));
    }

    /// <summary>Admin removes (deactivates) an HR user from their organization.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> RemoveHrUser(Guid id)
    {
        await _hrUserService.RemoveHrUserAsync(id);
        return Ok(ApiResponse<object>.SuccessResponse(new { }, "HR user removed."));
    }
}
