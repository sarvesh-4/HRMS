using HRMS.Domain.Constants;
using HRMS.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace HRMS.Infrastructure.Persistence.Seed;

/// <summary>
/// Seeds exactly one default Admin account (and an organization for it) if one
/// doesn't already exist, so the API is usable immediately without going through
/// register -> login -> create-organization by hand first.
///
/// Credentials are read from configuration ("DefaultAdmin" section in
/// appsettings.json) with hardcoded fallbacks if that section is missing.
/// Idempotent: safe to run on every startup — it no-ops if the admin already exists.
/// </summary>
public static class AdminUserSeeder
{
    public static async Task SeedAsync(
        ApplicationDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole<Guid>> roleManager,
        IConfiguration configuration,
        ILogger logger)
    {
        var section = configuration.GetSection("DefaultAdmin");

        var email = section["Email"] ?? "admin@hrms.com";
        var password = section["Password"] ?? "Admin@123";
        var firstName = section["FirstName"] ?? "System";
        var lastName = section["LastName"] ?? "Admin";
        var orgName = section["OrganizationName"] ?? "Default Organization";
        var orgAddress = section["OrganizationAddress"] ?? "Not specified";

        // Already seeded (or someone registered with this email) — nothing to do.
        var existingUser = await userManager.FindByEmailAsync(email);
        if (existingUser is not null)
        {
            logger.LogInformation("Default admin user '{Email}' already exists — skipping seed.", email);
            return;
        }

        var adminRole = await roleManager.FindByNameAsync(AppRoles.Admin);
        if (adminRole is null)
        {
            adminRole = new IdentityRole<Guid>(AppRoles.Admin);
            await roleManager.CreateAsync(adminRole);
        }

        var adminUser = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FirstName = firstName,
            LastName = lastName,
            IsActive = true
        };

        var createResult = await userManager.CreateAsync(adminUser, password);
        if (!createResult.Succeeded)
        {
            var errors = string.Join(" ", createResult.Errors.Select(e => e.Description));
            logger.LogError("Failed to seed default admin user: {Errors}", errors);
            return;
        }

        // No one created this account either — reference itself, same convention
        // used in OrganizationService when any user self-onboards as Admin.
        adminUser.CreatedByUserId = adminUser.Id;

        // Explicit role assignment — never defaulted, same as every other
        // account creation path in the app.
        adminUser.RoleId = adminRole.Id;

        var organization = new Organization
        {
            Name = orgName,
            Address = orgAddress,
            CreatedByUserId = adminUser.Id
        };

        dbContext.Organizations.Add(organization);
        await dbContext.SaveChangesAsync();

        adminUser.OrganizationId = organization.Id;
        await userManager.UpdateAsync(adminUser);
        await userManager.AddToRoleAsync(adminUser, AppRoles.Admin);

        logger.LogInformation(
            "Seeded default admin user '{Email}' with organization '{OrgName}'. " +
            "Change this password after first login.", email, orgName);
    }
}
