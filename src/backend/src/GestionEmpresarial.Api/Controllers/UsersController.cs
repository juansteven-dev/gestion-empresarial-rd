using GestionEmpresarial.Api.Authorization;
using GestionEmpresarial.Api.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace GestionEmpresarial.Api.Controllers;

[ApiController]
[Authorize(Roles = RoleNames.Administrator)]
[Route("api/v1/users")]
public sealed class UsersController(UserManager<IdentityUser> userManager) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<UserSessionResponse>> Create(CreateUserRequest request)
    {
        if (!RoleNames.IsKnown(request.Role))
        {
            ModelState.AddModelError(nameof(request.Role), "El rol solicitado no está permitido.");
            return ValidationProblem(ModelState);
        }

        var user = new IdentityUser
        {
            UserName = request.UserName,
            Email = request.Email
        };

        var createResult = await userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
        {
            return IdentityValidationProblem(createResult);
        }

        var roleResult = await userManager.AddToRoleAsync(user, request.Role);
        if (!roleResult.Succeeded)
        {
            await userManager.DeleteAsync(user);
            return Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "No se pudo asignar el rol al usuario.");
        }

        var roles = await userManager.GetRolesAsync(user);
        return StatusCode(
            StatusCodes.Status201Created,
            new UserSessionResponse(user.Id, user.UserName ?? string.Empty, user.Email, roles.ToArray()));
    }

    private ActionResult<UserSessionResponse> IdentityValidationProblem(IdentityResult result)
    {
        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(error.Code, error.Description);
        }

        return ValidationProblem(ModelState);
    }
}
