using HRMS.Domain.Common;

namespace HRMS.Domain.Entities;

public class Organization : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;

    /// <summary>The user who created the organization. This user becomes the Admin.</summary>
    public Guid CreatedByUserId { get; set; }

    public ApplicationUser? CreatedByUser { get; set; }

    public ICollection<ApplicationUser> Users { get; set; } = new List<ApplicationUser>();

    public ICollection<Employee> Employees { get; set; } = new List<Employee>();
}
