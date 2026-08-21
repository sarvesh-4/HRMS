using HRMS.Application.DTOs.Organization;

namespace HRMS.Application.Interfaces;

public interface IOrganizationService
{
    /// <summary>
    /// Creates an organization. First call for a user: attaches it as their
    /// primary org and promotes them to Admin. Subsequent calls: only an
    /// existing Admin may call this again, to create an ADDITIONAL organization
    /// (their primary org/role stay unchanged).
    /// </summary>
    Task<OrganizationResponseDto> CreateOrganizationAsync(CreateOrganizationDto dto);

    /// <summary>Returns the caller's primary organization (the first one they created).</summary>
    Task<OrganizationResponseDto> GetMyOrganizationAsync();

    /// <summary>Returns every organization the caller owns (created) — one item for a single-org Admin.</summary>
    Task<List<OrganizationResponseDto>> GetMyOrganizationsAsync();
}
