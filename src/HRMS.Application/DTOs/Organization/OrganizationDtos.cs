namespace HRMS.Application.DTOs.Organization;

public class CreateOrganizationDto
{
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
}

public class OrganizationResponseDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public Guid CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Populated only right after creation, so the caller can swap in a token
    /// that carries the new Admin role + OrganizationId claims.
    /// </summary>
    public string? Token { get; set; }
    public DateTime? ExpiresAtUtc { get; set; }
}
