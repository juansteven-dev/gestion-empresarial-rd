using GestionEmpresarial.Api.Contracts;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace GestionEmpresarial.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController(
    SignInManager<IdentityUser> signInManager,
    UserManager<IdentityUser> userManager,
    IAntiforgery antiforgery) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("csrf")]
    public ActionResult<CsrfTokenResponse> GetCsrfToken()
    {
        var tokens = antiforgery.GetAndStoreTokens(HttpContext);
        return Ok(new CsrfTokenResponse(tokens.RequestToken!));
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<UserSessionResponse>> Login(LoginRequest request)
    {
        var result = await signInManager.PasswordSignInAsync(
            request.UserName,
            request.Password,
            isPersistent: false,
            lockoutOnFailure: true);

        if (!result.Succeeded)
        {
            return Unauthorized(new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Credenciales inválidas",
                Detail = "El usuario o la contraseña son incorrectos."
            });
        }

        var user = await userManager.FindByNameAsync(request.UserName);
        if (user is null)
        {
            await signInManager.SignOutAsync();
            return Unauthorized();
        }

        return Ok(await ToSessionResponseAsync(user));
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();
        return NoContent();
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<UserSessionResponse>> GetCurrentUser()
    {
        var user = await userManager.GetUserAsync(User);
        return user is null
            ? Unauthorized()
            : Ok(await ToSessionResponseAsync(user));
    }

    private async Task<UserSessionResponse> ToSessionResponseAsync(IdentityUser user)
    {
        var roles = await userManager.GetRolesAsync(user);
        return new UserSessionResponse(user.Id, user.UserName ?? string.Empty, user.Email, roles.ToArray());
    }
}
