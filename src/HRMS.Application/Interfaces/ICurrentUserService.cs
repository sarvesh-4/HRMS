namespace HRMS.Application.Interfaces;

/// <summary>
/// Reads the current HTTP request's JWT claims. Implemented in Infrastructure
/// (needs IHttpContextAccessor) so Application stays framework-agnostic.
/// </summary>
public interface ICurrentUserService
{
    Guid UserId { get; }
    string? Email { get; }
    Guid? OrganizationId { get; }
    IReadOnlyList<string> Roles { get; }
    bool IsInRole(string role);
    bool IsAuthenticated { get; }
}
