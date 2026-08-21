using HRMS.Domain.Constants;
using Microsoft.AspNetCore.Identity;

namespace HRMS.Infrastructure.Persistence.Seed;

/// <summary>Called once at startup (see Program.cs) so Admin/HR roles always exist.</summary>
public static class RoleSeeder
{
    public static async Task SeedAsync(RoleManager<IdentityRole<Guid>> roleManager)
    {
        foreach (var roleName in AppRoles.All)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole<Guid>(roleName));
            }
        }
    }
}
