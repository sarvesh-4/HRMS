namespace HRMS.Application.DTOs.Auth;

public class RegisterRequestDto
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class LoginRequestDto
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// Returned by register/login and also re-issued after CreateOrganization,
/// since roles/organization claims change at that point.
/// </summary>
public class AuthResponseDto
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public Guid? OrganizationId { get; set; }

    /// <summary>Direct FK to the Roles table. Null until an explicit action assigns one (see AppRoles).</summary>
    public Guid? RoleId { get; set; }
    public List<string> Roles { get; set; } = new();
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
}
