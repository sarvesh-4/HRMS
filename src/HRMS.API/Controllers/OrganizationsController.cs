using FluentValidation;
using HRMS.Application.Common;
using HRMS.Application.DTOs.Organization;
using HRMS.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRMS.API.Controllers;

/// <summary>
/// Step 2 of the flow: any authenticated (but org-less) user creates an organization
/// and is automatically promoted to Admin for it. Requires re-login / using the
/// returned token, since the JWT claims change.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OrganizationsController : ControllerBase
{
    private readonly IOrganizationService _organizationService;
    private readonly IValidator<CreateOrganizationDto> _validator;

    public OrganizationsController(IOrganizationService organizationService, IValidator<CreateOrganizationDto> validator)
    {
        _organizationService = organizationService;
        _validator = validator;
    }

    /// <summary>
    /// Create an organization. First time: becomes your primary org, and you're
    /// promoted to Admin. If you're already an Admin: creates an ADDITIONAL
    /// organization you own (your primary org/token role are unaffected).
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<OrganizationResponseDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateOrganizationDto dto)
    {
        await _validator.ValidateAndThrowAsync(dto);

        var result = await _organizationService.CreateOrganizationAsync(dto);
        return StatusCode(StatusCodes.Status201Created, ApiResponse<OrganizationResponseDto>.SuccessResponse(
            result, "Organization created. You are now Admin — use the new token returned here for further requests."));
    }

    /// <summary>Get the caller's primary organization (the first one they created).</summary>
    [HttpGet("me")]
    [ProducesResponseType(typeof(ApiResponse<OrganizationResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMine()
    {
        var result = await _organizationService.GetMyOrganizationAsync();
        return Ok(ApiResponse<OrganizationResponseDto>.SuccessResponse(result));
    }

    /// <summary>List every organization the caller owns — one item for a single-org Admin, more for a multi-org Admin.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<OrganizationResponseDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyOrganizations()
    {
        var result = await _organizationService.GetMyOrganizationsAsync();
        return Ok(ApiResponse<List<OrganizationResponseDto>>.SuccessResponse(result));
    }
}
