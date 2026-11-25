using GestionEmpresarial.Api.Authorization;
using Microsoft.AspNetCore.Identity;

namespace GestionEmpresarial.Api.Services;

public sealed class IdentityBootstrapper(
    RoleManager<IdentityRole> roleManager,
    UserManager<IdentityUser> userManager,
    IConfiguration configuration)
{
    public async Task SeedRolesAsync()
    {
        foreach (var role in RoleNames.All)
        {
            if (await roleManager.RoleExistsAsync(role))
            {
                continue;
            }

            var result = await roleManager.CreateAsync(new IdentityRole(role));
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Could not create the required role '{role}': {string.Join("; ", result.Errors.Select(error => error.Code))}.");
            }
        }
    }

    public async Task<bool> CreateFirstAdministratorAsync()
    {
        var userName = configuration["BootstrapAdmin:UserName"];
        var email = configuration["BootstrapAdmin:Email"];
        var password = configuration["BootstrapAdmin:Password"];
        if (string.IsNullOrWhiteSpace(userName)
            || string.IsNullOrWhiteSpace(email)
            || string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException(
                "BootstrapAdmin:UserName, BootstrapAdmin:Email, and BootstrapAdmin:Password must be set with User Secrets or environment variables.");
        }

        var existingUser = await userManager.FindByNameAsync(userName);
        if (existingUser is not null)
        {
            if (await userManager.IsInRoleAsync(existingUser, RoleNames.Administrator))
            {
                return false;
            }

            throw new InvalidOperationException(
                "The configured bootstrap username already belongs to a non-administrator account.");
        }

        if ((await userManager.GetUsersInRoleAsync(RoleNames.Administrator)).Count != 0)
        {
            throw new InvalidOperationException(
                "An administrator already exists. The bootstrap command only creates the first administrator.");
        }

        var administrator = new IdentityUser
        {
            UserName = userName,
            Email = email
        };

        var createResult = await userManager.CreateAsync(administrator, password);
        if (!createResult.Succeeded)
        {
            throw new InvalidOperationException(
                $"The bootstrap administrator could not be created: {string.Join("; ", createResult.Errors.Select(error => error.Code))}.");
        }

        var roleResult = await userManager.AddToRoleAsync(administrator, RoleNames.Administrator);
        if (!roleResult.Succeeded)
        {
            await userManager.DeleteAsync(administrator);
            throw new InvalidOperationException(
                "The bootstrap administrator role could not be assigned; the new account was removed.");
        }

        return true;
    }
}
