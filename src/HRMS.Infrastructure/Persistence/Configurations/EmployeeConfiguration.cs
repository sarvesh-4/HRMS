using HRMS.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HRMS.Infrastructure.Persistence.Configurations;

public class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.ToTable("Employees");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
        builder.Property(e => e.LastName).IsRequired().HasMaxLength(100);
        builder.Property(e => e.Email).IsRequired().HasMaxLength(256);
        builder.Property(e => e.PhoneNumber).IsRequired().HasMaxLength(20);
        builder.Property(e => e.Address).IsRequired().HasMaxLength(500);

        builder.HasIndex(e => new { e.OrganizationId, e.Email });

        // The HR user who created the record. Restrict delete: an HR account is
        // deactivated (see HrUserService.RemoveHrUserAsync), never hard-deleted,
        // precisely so this reference stays valid.
        builder.HasOne(e => e.CreatedByUser)
            .WithMany()
            .HasForeignKey(e => e.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Direct, explicit FK to the Roles table — always the "Employee" role's
        // Id, required (every Employee record is that role, by definition).
        builder.HasOne<IdentityRole<Guid>>()
            .WithMany()
            .HasForeignKey(e => e.RoleId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);
    }
}
