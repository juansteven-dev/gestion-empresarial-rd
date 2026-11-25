using System.ComponentModel.DataAnnotations;

namespace GestionEmpresarial.Api.Contracts;

public sealed class LoginRequest
{
    [Required]
    public string UserName { get; init; } = string.Empty;

    [Required]
    public string Password { get; init; } = string.Empty;
}

public sealed class CreateUserRequest
{
    [Required]
    [StringLength(256, MinimumLength = 3)]
    public string UserName { get; init; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(256)]
    public string Email { get; init; } = string.Empty;

    [Required]
    public string Password { get; init; } = string.Empty;

    [Required]
    public string Role { get; init; } = string.Empty;
}

public sealed record UserSessionResponse(
    string Id,
    string UserName,
    string? Email,
    IReadOnlyList<string> Roles);

public sealed record CsrfTokenResponse(string RequestToken);
