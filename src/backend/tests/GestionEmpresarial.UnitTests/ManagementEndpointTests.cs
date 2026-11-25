using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GestionEmpresarial.Api.Authorization;
using GestionEmpresarial.Domain.Entities;

namespace GestionEmpresarial.UnitTests;

[Collection(ApiIntegrationCollection.Name)]
public sealed class ManagementEndpointTests(ApiFactory factory)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task Administrator_CanReadDashboardReportsUsersAndAuditFromApiData()
    {
        using var administrator = await CreateAuthenticatedClientAsync(RoleNames.Administrator);
        var categoryId = await CreateCategoryAsync(administrator);
        var sku = $"MGT-{Guid.NewGuid():N}"[..20].ToUpperInvariant();
        var productId = await CreateProductAsync(administrator, categoryId, sku);
        await AdjustStockAsync(administrator, productId, delta: 3);

        using var saleResponse = await SendWithCsrfAsync(
            administrator.Client,
            HttpMethod.Post,
            "/api/v1/sales",
            administrator.CsrfToken,
            new { PaymentMethod = 0, Items = new[] { new { ProductId = productId, Quantity = 1 } } });
        Assert.Equal(HttpStatusCode.Created, saleResponse.StatusCode);
        var createdSale = await saleResponse.Content.ReadFromJsonAsync<CreatedSaleResponse>(JsonOptions);
        Assert.NotNull(createdSale);

        var date = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd");
        using var dashboardResponse = await administrator.Client.GetAsync($"/api/v1/dashboard?fromDate={date}&toDate={date}");
        Assert.True(dashboardResponse.StatusCode == HttpStatusCode.OK,
            $"Dashboard returned {(int)dashboardResponse.StatusCode}: {await dashboardResponse.Content.ReadAsStringAsync()}");
        var dashboard = await dashboardResponse.Content.ReadFromJsonAsync<DashboardResponse>(JsonOptions);
        Assert.NotNull(dashboard);
        Assert.True(dashboard.SaleCount >= 1);
        Assert.True(dashboard.SalesTotal >= 12.34m);
        Assert.True(dashboard.TodaySaleCount >= 1);
        Assert.True(dashboard.TodaySalesTotal >= 12.34m);
        Assert.Contains(dashboard.LowStockProducts, product => product.SKU == sku);

        using var reportsResponse = await administrator.Client.GetAsync($"/api/v1/reports?fromDate={date}&toDate={date}");
        Assert.Equal(HttpStatusCode.OK, reportsResponse.StatusCode);
        var reports = await reportsResponse.Content.ReadFromJsonAsync<ReportsResponse>(JsonOptions);
        Assert.NotNull(reports);
        Assert.Equal(reports.SaleCount, reports.SalesByDay.Sum(item => item.SaleCount));
        Assert.Equal(reports.SalesTotal, reports.SalesByDay.Sum(item => item.Total));
        Assert.Contains(reports.SalesByDay, item => item.SaleCount >= 1 && item.Total >= 12.34m);
        Assert.Contains(reports.TopProducts, item => item.SKU == sku && item.Quantity == 1);
        Assert.Contains(reports.Movements, item => item.Type == 1 && item.MovementCount >= 1);

        using var salesPageResponse = await administrator.Client.GetAsync(
            $"/api/v1/sales?search={Uri.EscapeDataString(createdSale.SaleNumber)}&fromDate={date}&toDate={date}&page=1&pageSize=1");
        Assert.Equal(HttpStatusCode.OK, salesPageResponse.StatusCode);
        var salesPage = await salesPageResponse.Content.ReadFromJsonAsync<PageResponse<SaleRowResponse>>(JsonOptions);
        Assert.NotNull(salesPage);
        var listedSale = Assert.Single(salesPage.Items);
        Assert.Equal(createdSale.SaleId, listedSale.SaleId);
        Assert.Equal(createdSale.Total, listedSale.Total);

        var fromUtc = Uri.EscapeDataString($"{date}T00:00:00.0000000Z");
        var toUtc = Uri.EscapeDataString($"{date}T23:59:59.9999999Z");
        using var movementResponse = await administrator.Client.GetAsync(
            $"/api/v1/inventory/movements?productId={productId}&fromUtc={fromUtc}&toUtc={toUtc}&page=1&pageSize=1");
        Assert.Equal(HttpStatusCode.OK, movementResponse.StatusCode);
        var movementPage = await movementResponse.Content.ReadFromJsonAsync<PageResponse<MovementRowResponse>>(JsonOptions);
        Assert.NotNull(movementPage);
        Assert.True(movementPage.TotalCount >= 2);
        Assert.All(movementPage.Items, item => Assert.Equal(productId, item.ProductId));
        Assert.Single(movementPage.Items);

        var oldDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-500).ToString("yyyy-MM-dd");
        using var oldPeriodResponse = await administrator.Client.GetAsync($"/api/v1/reports?fromDate={oldDate}&toDate={oldDate}");
        Assert.Equal(HttpStatusCode.OK, oldPeriodResponse.StatusCode);
        var oldPeriod = await oldPeriodResponse.Content.ReadFromJsonAsync<ReportsResponse>(JsonOptions);
        Assert.NotNull(oldPeriod);
        Assert.Equal(0, oldPeriod.SaleCount);
        Assert.Equal(0m, oldPeriod.SalesTotal);
        Assert.Empty(oldPeriod.SalesByDay);

        var account = await factory.CreateUserAsync(RoleNames.Employee);
        using var usersResponse = await administrator.Client.GetAsync($"/api/v1/users?search={Uri.EscapeDataString(account.UserName)}&page=1&pageSize=10");
        Assert.Equal(HttpStatusCode.OK, usersResponse.StatusCode);
        var users = await usersResponse.Content.ReadFromJsonAsync<PageResponse<UserResponse>>(JsonOptions);
        Assert.NotNull(users);
        var listedUser = Assert.Single(users.Items);
        Assert.Equal(account.UserName, listedUser.UserName);
        Assert.Contains(RoleNames.Employee, listedUser.Roles);

        using var auditResponse = await administrator.Client.GetAsync("/api/v1/audit?search=CategoryCreated&page=1&pageSize=10");
        Assert.Equal(HttpStatusCode.OK, auditResponse.StatusCode);
        var audits = await auditResponse.Content.ReadFromJsonAsync<PageResponse<AuditResponse>>(JsonOptions);
        Assert.NotNull(audits);
        Assert.Contains(audits.Items, item => item.Action == "CategoryCreated" && item.EntityName == "Category");
    }

    [Fact]
    public async Task Employee_CanReadScopedDashboardButCannotReadReportsUsersOrAudit()
    {
        using var employee = await CreateAuthenticatedClientAsync(RoleNames.Employee);

        using var dashboardResponse = await employee.Client.GetAsync("/api/v1/dashboard");
        Assert.True(dashboardResponse.StatusCode == HttpStatusCode.OK,
            $"Dashboard returned {(int)dashboardResponse.StatusCode}: {await dashboardResponse.Content.ReadAsStringAsync()}");
        var dashboard = await dashboardResponse.Content.ReadFromJsonAsync<DashboardResponse>(JsonOptions);
        Assert.NotNull(dashboard);
        Assert.Null(dashboard.LowStockCount);
        Assert.Equal(0, dashboard.TodaySaleCount);

        using var reportsResponse = await employee.Client.GetAsync("/api/v1/reports");
        using var usersResponse = await employee.Client.GetAsync("/api/v1/users");
        using var auditResponse = await employee.Client.GetAsync("/api/v1/audit");
        Assert.Equal(HttpStatusCode.Forbidden, reportsResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, usersResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, auditResponse.StatusCode);
    }

    private async Task<AuthenticatedClient> CreateAuthenticatedClientAsync(string role)
    {
        var account = await factory.CreateUserAsync(role);
        var client = factory.CreateHttpsClient();
        var csrfToken = await GetCsrfTokenAsync(client);
        using var loginResponse = await SendWithCsrfAsync(
            client,
            HttpMethod.Post,
            "/api/v1/auth/login",
            csrfToken,
            new { account.UserName, account.Password });
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        return new AuthenticatedClient(client, await GetCsrfTokenAsync(client));
    }

    private static async Task<int> CreateCategoryAsync(AuthenticatedClient client)
    {
        using var response = await SendWithCsrfAsync(
            client.Client,
            HttpMethod.Post,
            "/api/v1/categories",
            client.CsrfToken,
            new { Name = $"management-category-{Guid.NewGuid():N}", IsActive = true });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<CategoryResponse>(JsonOptions))!.CategoryId;
    }

    private static async Task<int> CreateProductAsync(AuthenticatedClient client, int categoryId, string sku)
    {
        using var response = await SendWithCsrfAsync(
            client.Client,
            HttpMethod.Post,
            "/api/v1/products",
            client.CsrfToken,
            new
            {
                SKU = sku,
                Name = $"Management product {Guid.NewGuid():N}"[..28],
                CategoryId = categoryId,
                CostPrice = 4m,
                SalePrice = 12.34m,
                MinimumStock = 3,
                IsActive = true
            });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<ProductResponse>(JsonOptions))!.ProductId;
    }

    private static async Task AdjustStockAsync(AuthenticatedClient client, int productId, int delta)
    {
        using var response = await SendWithCsrfAsync(
            client.Client,
            HttpMethod.Post,
            "/api/v1/inventory/adjustments",
            client.CsrfToken,
            new { ProductId = productId, QuantityDelta = delta, Reason = "Prepare management report test" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private static async Task<string> GetCsrfTokenAsync(HttpClient client)
    {
        using var response = await client.GetAsync("/api/v1/auth/csrf");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<CsrfResponse>(JsonOptions))!.RequestToken;
    }

    private static async Task<HttpResponseMessage> SendWithCsrfAsync(
        HttpClient client,
        HttpMethod method,
        string path,
        string csrfToken,
        object? payload = null)
    {
        using var request = new HttpRequestMessage(method, path);
        if (payload is not null) request.Content = JsonContent.Create(payload);
        request.Headers.Add("X-CSRF-TOKEN", csrfToken);
        return await client.SendAsync(request);
    }

    private sealed record AuthenticatedClient(HttpClient Client, string CsrfToken) : IDisposable
    {
        public void Dispose() => Client.Dispose();
    }

    private sealed record CsrfResponse(string RequestToken);
    private sealed record CategoryResponse(int CategoryId);
    private sealed record ProductResponse(int ProductId);
    private sealed record DashboardResponse(decimal? TodaySalesTotal, int? TodaySaleCount, decimal? SalesTotal, int? SaleCount, int? LowStockCount, LowStockResponse[] LowStockProducts);
    private sealed record LowStockResponse(int ProductId, string SKU, int CurrentStock, int MinimumStock);
    private sealed record ReportsResponse(int SaleCount, decimal SalesTotal, SalesByDayResponse[] SalesByDay, TopProductResponse[] TopProducts, MovementResponse[] Movements);
    private sealed record SalesByDayResponse(DateTime DayUtc, int SaleCount, decimal Total);
    private sealed record TopProductResponse(int ProductId, string SKU, int Quantity, decimal Total);
    private sealed record MovementResponse(int Type, int MovementCount, int QuantityDelta);
    private sealed record CreatedSaleResponse(long SaleId, string SaleNumber, decimal Total);
    private sealed record SaleRowResponse(long SaleId, string SaleNumber, decimal Total);
    private sealed record MovementRowResponse(int ProductId, string ProductName, DateTime CreatedAtUtc);
    private sealed record UserResponse(string Id, string UserName, string? Email, string[] Roles, bool IsLockedOut);
    private sealed record AuditResponse(long AuditLogId, string UserId, string UserName, string Action, string EntityName, string? EntityId, string? DetailsJson, string? IpAddress, DateTime CreatedAtUtc);
    private sealed record PageResponse<T>(T[] Items, int Page, int PageSize, int TotalCount, int TotalPages);
}

public sealed class AuditLogPrivacyTests
{
    [Fact]
    public void AuditLog_RemovesSensitiveFieldsFromNestedDetails()
    {
        const string details = """
            {
              "name": "safe value",
              "Password": "plain password",
              "nested": {
                "refresh_token": "refresh secret",
                "Cookie": "session cookie",
                "authorization": "bearer token",
                "quantity": 4
              },
              "items": [{ "csrfToken": "csrf value", "sku": "SKU-1" }]
            }
            """;

        var audit = new AuditLog("actor-1", "EntityUpdated", "Entity", "1", details, null);
        using var sanitized = JsonDocument.Parse(audit.DetailsJson!);

        Assert.Equal("safe value", sanitized.RootElement.GetProperty("name").GetString());
        Assert.False(sanitized.RootElement.TryGetProperty("Password", out _));
        var nested = sanitized.RootElement.GetProperty("nested");
        Assert.Equal(4, nested.GetProperty("quantity").GetInt32());
        Assert.False(nested.TryGetProperty("refresh_token", out _));
        Assert.False(nested.TryGetProperty("Cookie", out _));
        Assert.False(nested.TryGetProperty("authorization", out _));
        var item = sanitized.RootElement.GetProperty("items")[0];
        Assert.Equal("SKU-1", item.GetProperty("sku").GetString());
        Assert.False(item.TryGetProperty("csrfToken", out _));
        Assert.DoesNotContain("plain password", audit.DetailsJson, StringComparison.Ordinal);
        Assert.DoesNotContain("refresh secret", audit.DetailsJson, StringComparison.Ordinal);
        Assert.DoesNotContain("session cookie", audit.DetailsJson, StringComparison.Ordinal);
        Assert.DoesNotContain("bearer token", audit.DetailsJson, StringComparison.Ordinal);
        Assert.DoesNotContain("csrf value", audit.DetailsJson, StringComparison.Ordinal);
    }
}
