namespace HRMS.Application.DTOs.HrUser;

public class CreateHrUserDto
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Optional. Which of the Admin's organizations this HR user belongs to.
    /// Omit it to use the Admin's primary organization (the original, still
    /// the common case for an Admin who owns only one company).
    /// </summary>
    public Guid? OrganizationId { get; set; }
}

public class HrUserResponseDto
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public Guid OrganizationId { get; set; }

    /// <summary>Direct FK to the Roles table — always the HR role's Id for an HR account.</summary>
    public Guid? RoleId { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
