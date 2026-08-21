using FluentValidation;
using HRMS.Application.Common;
using HRMS.Application.DTOs.Auth;
using HRMS.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRMS.API.Controllers;

/// <summary>
/// Step 1 of the flow: create an account, then log in to get a JWT.
/// The token issued here has no roles/organization yet — call
/// POST /api/organizations next to become an Admin.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IValidator<RegisterRequestDto> _registerValidator;
    private readonly IValidator<LoginRequestDto> _loginValidator;

    public AuthController(
        IAuthService authService,
        IValidator<RegisterRequestDto> registerValidator,
        IValidator<LoginRequestDto> loginValidator)
    {
        _authService = authService;
        _registerValidator = registerValidator;
        _loginValidator = loginValidator;
    }

    /// <summary>Register a new user account. No organization or role is assigned yet.</summary>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto dto)
    {
        await _registerValidator.ValidateAndThrowAsync(dto);

        var result = await _authService.RegisterAsync(dto);
        return StatusCode(StatusCodes.Status201Created,
            ApiResponse<AuthResponseDto>.SuccessResponse(result, "Registration successful. Create an organization next."));
    }

    /// <summary>Log in with email + password and receive a JWT.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto dto)
    {
        await _loginValidator.ValidateAndThrowAsync(dto);

        var result = await _authService.LoginAsync(dto);
        return Ok(ApiResponse<AuthResponseDto>.SuccessResponse(result, "Login successful."));
    }

    /// <summary>
    /// Returns the current user's Organization/role state read straight from the
    /// database. Use this to confirm what's actually persisted (e.g. after creating
    /// an organization, or being added as HR) without needing direct DB access.
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Me()
    {
        var result = await _authService.GetCurrentUserAsync();
        return Ok(ApiResponse<AuthResponseDto>.SuccessResponse(result));
    }
}
