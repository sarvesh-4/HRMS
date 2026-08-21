using HRMS.Application.DTOs.HrUser;

namespace HRMS.Application.Interfaces;

/// <summary>Admin-only operations for managing HR users under their organization(s).</summary>
public interface IHrUserService
{
    /// <summary>Creates an HR user under one of the Admin's organizations (see CreateHrUserDto.OrganizationId).</summary>
    Task<HrUserResponseDto> CreateHrUserAsync(CreateHrUserDto dto);

    Task RemoveHrUserAsync(Guid hrUserId);

    /// <summary>
    /// Lists HR users. When <paramref name="organizationId"/> is null, aggregates
    /// across every organization the Admin owns (identical to "just my one org"
    /// for a single-org Admin). When provided, narrows to that one organization.
    /// </summary>
    Task<List<HrUserResponseDto>> GetHrUsersAsync(Guid? organizationId = null);
}
