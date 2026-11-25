using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GestionEmpresarial.Api.Authorization;

namespace GestionEmpresarial.UnitTests;

[Collection(ApiIntegrationCollection.Name)]
public sealed class AuthenticationEndpointTests(ApiFactory factory)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task Login_WithValidCredentials_SetsSecureCookieAndReturnsSession()
    {
        var account = await factory.CreateUserAsync(RoleNames.Employee);
        using var client = factory.CreateHttpsClient();
        var csrfToken = await GetCsrfTokenAsync(client);

        using var loginResponse = await PostWithCsrfAsync(
            client,
            "/api/v1/auth/login",
            csrfToken,
            new { account.UserName, account.Password });

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        var authCookie = Assert.Single(loginResponse.Headers.GetValues("Set-Cookie"));
        Assert.Contains("__Host-GestionEmpresarial.Auth=", authCookie, StringComparison.Ordinal);
        Assert.Contains("httponly", authCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("secure", authCookie, StringComparison.OrdinalIgnoreCase);

        using var currentUserResponse = await client.GetAsync("/api/v1/auth/me");
        Assert.Equal(HttpStatusCode.OK, currentUserResponse.StatusCode);
        var currentUser = await currentUserResponse.Content.ReadFromJsonAsync<UserSessionResponse>(JsonOptions);
        Assert.NotNull(currentUser);
        Assert.Equal(account.UserName, currentUser.UserName);
        Assert.Contains(RoleNames.Employee, currentUser.Roles);
    }

    [Fact]
    public async Task Login_WithIncorrectPassword_ReturnsGenericUnauthorizedResponse()
    {
        var account = await factory.CreateUserAsync(RoleNames.Employee);
        using var client = factory.CreateHttpsClient();
        var csrfToken = await GetCsrfTokenAsync(client);

        using var response = await PostWithCsrfAsync(
            client,
            "/api/v1/auth/login",
            csrfToken,
            new { account.UserName, Password = $"Wrong!{Guid.NewGuid():N}9a" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var responseBody = await response.Content.ReadAsStringAsync();
        Assert.Contains("usuario o la contraseña", responseBody, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CurrentUser_WithoutSession_ReturnsUnauthorized()
    {
        using var client = factory.CreateHttpsClient();

        using var response = await client.GetAsync("/api/v1/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Employee_CannotCreateUser_ReturnsForbidden()
    {
        var account = await factory.CreateUserAsync(RoleNames.Employee);
        using var client = factory.CreateHttpsClient();
        var anonymousCsrfToken = await GetCsrfTokenAsync(client);
        using var loginResponse = await PostWithCsrfAsync(
            client,
            "/api/v1/auth/login",
            anonymousCsrfToken,
            new { account.UserName, account.Password });
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        var authenticatedCsrfToken = await GetCsrfTokenAsync(client);

        using var response = await PostWithCsrfAsync(
            client,
            "/api/v1/users",
            authenticatedCsrfToken,
            new
            {
                UserName = $"new-{Guid.NewGuid():N}",
                Email = $"new-{Guid.NewGuid():N}@example.test",
                Password = $"A9!b{Guid.NewGuid():N}Z",
                Role = RoleNames.Employee
            });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Administrator_CanCreateUserWithAnAllowedRole()
    {
        var account = await factory.CreateUserAsync(RoleNames.Administrator);
        using var client = factory.CreateHttpsClient();
        var anonymousCsrfToken = await GetCsrfTokenAsync(client);
        using var loginResponse = await PostWithCsrfAsync(
            client,
            "/api/v1/auth/login",
            anonymousCsrfToken,
            new { account.UserName, account.Password });
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        var authenticatedCsrfToken = await GetCsrfTokenAsync(client);
        var newUserName = $"created-{Guid.NewGuid():N}";
        using var createResponse = await PostWithCsrfAsync(
            client,
            "/api/v1/users",
            authenticatedCsrfToken,
            new
            {
                UserName = newUserName,
                Email = $"{newUserName}@example.test",
                Password = $"A9!b{Guid.NewGuid():N}Z",
                Role = RoleNames.WarehouseManager
            });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var createdUser = await createResponse.Content.ReadFromJsonAsync<UserSessionResponse>(JsonOptions);
        Assert.NotNull(createdUser);
        Assert.Equal(newUserName, createdUser.UserName);
        Assert.Contains(RoleNames.WarehouseManager, createdUser.Roles);
    }

    [Fact]
    public async Task Logout_ClearsSession()
    {
        var account = await factory.CreateUserAsync(RoleNames.Employee);
        using var client = factory.CreateHttpsClient();
        var anonymousCsrfToken = await GetCsrfTokenAsync(client);
        using var loginResponse = await PostWithCsrfAsync(
            client,
            "/api/v1/auth/login",
            anonymousCsrfToken,
            new { account.UserName, account.Password });
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        var authenticatedCsrfToken = await GetCsrfTokenAsync(client);
        using var logoutResponse = await PostWithCsrfAsync(
            client,
            "/api/v1/auth/logout",
            authenticatedCsrfToken,
            new { });

        Assert.Equal(HttpStatusCode.NoContent, logoutResponse.StatusCode);
        using var currentUserResponse = await client.GetAsync("/api/v1/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, currentUserResponse.StatusCode);
    }

    [Fact]
    public async Task Login_WithoutAntiforgeryToken_ReturnsBadRequest()
    {
        using var client = factory.CreateHttpsClient();

        using var response = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new { UserName = "someone", Password = "not-a-real-password" });

        var responseBody = await response.Content.ReadAsStringAsync();
        Assert.True(
            response.StatusCode == HttpStatusCode.BadRequest,
            $"Expected 400 but got {(int)response.StatusCode}: {responseBody}");
    }

    private static async Task<string> GetCsrfTokenAsync(HttpClient client)
    {
        using var response = await client.GetAsync("/api/v1/auth/csrf");
        var responseBody = await response.Content.ReadAsStringAsync();
        Assert.True(
            response.IsSuccessStatusCode,
            $"CSRF endpoint returned {(int)response.StatusCode}: {responseBody}");
        var payload = JsonSerializer.Deserialize<CsrfPayload>(responseBody, JsonOptions);
        Assert.NotNull(payload);
        Assert.False(string.IsNullOrWhiteSpace(payload.RequestToken));
        return payload.RequestToken;
    }

    private static Task<HttpResponseMessage> PostWithCsrfAsync<T>(
        HttpClient client,
        string path,
        string token,
        T payload)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = JsonContent.Create(payload)
        };
        request.Headers.Add("X-CSRF-TOKEN", token);
        return client.SendAsync(request);
    }

    private sealed record CsrfPayload(string RequestToken);
    private sealed record UserSessionResponse(string Id, string UserName, string? Email, string[] Roles);
}
