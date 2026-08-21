using HRMS.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HRMS.Infrastructure.Persistence.Configurations;

public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.Property(u => u.FirstName).IsRequired().HasMaxLength(100);
        builder.Property(u => u.LastName).IsRequired().HasMaxLength(100);

        // Self-referencing: an HR user's CreatedByUserId points at the Admin who
        // created them. Explicit + Restrict, since self-referencing cascade delete
        // is not supported and would otherwise throw at model-build time.
        builder.HasOne(u => u.CreatedByUser)
            .WithMany()
            .HasForeignKey(u => u.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Direct, explicit FK to the Roles table — nullable (null until an
        // account is promoted to Admin or created as HR; never defaulted).
        // No CLR navigation property needed on either side for this one.
        builder.HasOne<IdentityRole<Guid>>()
            .WithMany()
            .HasForeignKey(u => u.RoleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
