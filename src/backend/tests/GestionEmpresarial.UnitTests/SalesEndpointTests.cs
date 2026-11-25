using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GestionEmpresarial.Api.Authorization;
using GestionEmpresarial.Domain.Enums;
using GestionEmpresarial.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GestionEmpresarial.UnitTests;

[Collection(ApiIntegrationCollection.Name)]
public sealed class SalesEndpointTests(ApiFactory factory)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task CreateSale_UsesServerPriceAndCreatesReceiptStockMovementAndAudit()
    {
        using var client = await CreateAuthenticatedClientAsync();
        var (categoryId, productId) = await CreateProductAsync(client, 4, salePrice: 9.25m);

        using var response = await SendWithCsrfAsync(
            client.Client,
            HttpMethod.Post,
            "/api/v1/sales",
            client.CsrfToken,
            new
            {
                PaymentMethod = (int)PaymentMethod.Cash,
                Items = new[] { new { ProductId = productId, Quantity = 2, UnitPrice = 0.01m, LineTotal = 0.01m } },
                Subtotal = 0.01m,
                Total = 0.01m
            });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var sale = await response.Content.ReadFromJsonAsync<SaleDetailResponse>(JsonOptions);
        Assert.NotNull(sale);
        Assert.StartsWith("SAL-", sale.SaleNumber, StringComparison.Ordinal);
        Assert.Equal(18.50m, sale.Subtotal);
        Assert.Equal(0m, sale.Discount);
        Assert.Equal(18.50m, sale.Total);
        Assert.Single(sale.Items);
        Assert.Equal(9.25m, sale.Items[0].UnitPrice);
        Assert.Equal(18.50m, sale.Items[0].LineTotal);

        Assert.Equal(2, (await GetProductAsync(client.Client, productId)).CurrentStock);
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var movement = await dbContext.StockMovements.AsNoTracking().SingleAsync(item =>
            item.ReferenceType == StockReferenceType.Sale && item.ReferenceId == sale.SaleId);
        Assert.Equal(MovementType.Sale, movement.MovementType);
        Assert.Equal(-2, movement.QuantityDelta);
        Assert.Equal(4, movement.StockBefore);
        Assert.Equal(2, movement.StockAfter);
        var audit = await dbContext.AuditLogs.AsNoTracking().SingleAsync(item =>
            item.Action == "SaleCreated" && item.EntityId == sale.SaleId.ToString());
        Assert.Contains(sale.SaleNumber, audit.DetailsJson, StringComparison.Ordinal);
        Assert.True(categoryId > 0);
    }

    [Fact]
    public async Task CreateSale_RejectsEmptyLinesInsufficientStockAndInactiveProductsWithoutPartialSale()
    {
        using var client = await CreateAuthenticatedClientAsync();
        var (_, productId) = await CreateProductAsync(client, stock: 1);

        using var emptyResponse = await SendWithCsrfAsync(
            client.Client,
            HttpMethod.Post,
            "/api/v1/sales",
            client.CsrfToken,
            new { PaymentMethod = (int)PaymentMethod.Cash, Items = Array.Empty<object>() });
        Assert.Equal(HttpStatusCode.BadRequest, emptyResponse.StatusCode);

        using var insufficientResponse = await SendWithCsrfAsync(
            client.Client,
            HttpMethod.Post,
            "/api/v1/sales",
            client.CsrfToken,
            NewSale(productId, quantity: 2));
        Assert.Equal(HttpStatusCode.Conflict, insufficientResponse.StatusCode);

        using var deactivateResponse = await SendWithCsrfAsync(
            client.Client,
            HttpMethod.Delete,
            $"/api/v1/products/{productId}",
            client.CsrfToken);
        Assert.Equal(HttpStatusCode.NoContent, deactivateResponse.StatusCode);

        using var inactiveResponse = await SendWithCsrfAsync(
            client.Client,
            HttpMethod.Post,
            "/api/v1/sales",
            client.CsrfToken,
            NewSale(productId, quantity: 1));
        Assert.Equal(HttpStatusCode.Conflict, inactiveResponse.StatusCode);
        Assert.Equal(1, (await GetProductAsync(client.Client, productId)).CurrentStock);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.False(await dbContext.SaleItems.AsNoTracking().AnyAsync(item => item.ProductId == productId));
        Assert.False(await dbContext.StockMovements.AsNoTracking().AnyAsync(item =>
            item.ReferenceType == StockReferenceType.Sale && item.ProductId == productId));
        var productAuditToken = $"\"productId\":{productId}";
        Assert.False(await dbContext.AuditLogs.AsNoTracking().AnyAsync(item =>
            item.Action == "SaleCreated"
            && item.DetailsJson != null
            && item.DetailsJson.Contains(productAuditToken)));
    }

    [Fact]
    public async Task SalesEndpoints_RequireAdministratorOrEmployeeAndEmployeeSeesOnlyOwnHistory()
    {
        using var unauthenticatedClient = factory.CreateHttpsClient();
        using var unauthorizedResponse = await unauthenticatedClient.GetAsync("/api/v1/sales");
        Assert.Equal(HttpStatusCode.Unauthorized, unauthorizedResponse.StatusCode);

        using var warehouse = await CreateAuthenticatedClientAsync(RoleNames.WarehouseManager);
        using var forbiddenResponse = await warehouse.Client.GetAsync("/api/v1/sales");
        Assert.Equal(HttpStatusCode.Forbidden, forbiddenResponse.StatusCode);

        using var administrator = await CreateAuthenticatedClientAsync(RoleNames.Administrator);
        var (_, productId) = await CreateProductAsync(administrator, stock: 2);
        using var firstEmployee = await CreateAuthenticatedClientAsync(RoleNames.Employee);
        using var secondEmployee = await CreateAuthenticatedClientAsync(RoleNames.Employee);
        using var createResponse = await SendWithCsrfAsync(
            firstEmployee.Client,
            HttpMethod.Post,
            "/api/v1/sales",
            firstEmployee.CsrfToken,
            NewSale(productId, quantity: 1));
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var sale = await createResponse.Content.ReadFromJsonAsync<SaleDetailResponse>(JsonOptions);
        Assert.NotNull(sale);

        using var firstHistoryResponse = await firstEmployee.Client.GetAsync("/api/v1/sales?page=1&pageSize=100");
        using var secondHistoryResponse = await secondEmployee.Client.GetAsync("/api/v1/sales?page=1&pageSize=100");
        using var adminHistoryResponse = await administrator.Client.GetAsync("/api/v1/sales?page=1&pageSize=100");
        Assert.Equal(HttpStatusCode.OK, firstHistoryResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, secondHistoryResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, adminHistoryResponse.StatusCode);
        var firstHistory = await firstHistoryResponse.Content.ReadFromJsonAsync<SalePageResponse>(JsonOptions);
        var secondHistory = await secondHistoryResponse.Content.ReadFromJsonAsync<SalePageResponse>(JsonOptions);
        var adminHistory = await adminHistoryResponse.Content.ReadFromJsonAsync<SalePageResponse>(JsonOptions);
        Assert.NotNull(firstHistory);
        Assert.NotNull(secondHistory);
        Assert.NotNull(adminHistory);
        Assert.Contains(firstHistory.Items, item => item.SaleId == sale.SaleId);
        Assert.DoesNotContain(secondHistory.Items, item => item.SaleId == sale.SaleId);
        Assert.Contains(adminHistory.Items, item => item.SaleId == sale.SaleId);

        var saleDate = sale.CreatedAtUtc.ToString("yyyy-MM-dd");
        using var filteredHistoryResponse = await firstEmployee.Client.GetAsync(
            $"/api/v1/sales?search={Uri.EscapeDataString(sale.SaleNumber)}&fromDate={saleDate}&toDate={saleDate}&page=1&pageSize=1");
        Assert.Equal(HttpStatusCode.OK, filteredHistoryResponse.StatusCode);
        var filteredHistory = await filteredHistoryResponse.Content.ReadFromJsonAsync<SalePageResponse>(JsonOptions);
        Assert.NotNull(filteredHistory);
        Assert.Equal(1, filteredHistory.TotalCount);
        Assert.Single(filteredHistory.Items);

        using var hiddenDetailResponse = await secondEmployee.Client.GetAsync($"/api/v1/sales/{sale.SaleId}");
        Assert.Equal(HttpStatusCode.NotFound, hiddenDetailResponse.StatusCode);
    }

    [Fact]
    public async Task RejectedMultiLineSale_LeavesEveryProductAndSaleRecordUnchanged()
    {
        using var client = await CreateAuthenticatedClientAsync();
        var (_, firstProductId) = await CreateProductAsync(client, stock: 5);
        var (_, secondProductId) = await CreateProductAsync(client, stock: 1);

        using var response = await SendWithCsrfAsync(
            client.Client,
            HttpMethod.Post,
            "/api/v1/sales",
            client.CsrfToken,
            new
            {
                PaymentMethod = (int)PaymentMethod.Card,
                Items = new[]
                {
                    new { ProductId = firstProductId, Quantity = 2 },
                    new { ProductId = secondProductId, Quantity = 2 }
                }
            });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(5, (await GetProductAsync(client.Client, firstProductId)).CurrentStock);
        Assert.Equal(1, (await GetProductAsync(client.Client, secondProductId)).CurrentStock);
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.False(await dbContext.SaleItems.AsNoTracking().AnyAsync(item =>
            item.ProductId == firstProductId || item.ProductId == secondProductId));
        Assert.False(await dbContext.StockMovements.AsNoTracking().AnyAsync(item =>
            item.ReferenceType == StockReferenceType.Sale
            && (item.ProductId == firstProductId || item.ProductId == secondProductId)));
    }

    [Fact]
    public async Task ConcurrentSalesForLastUnit_AllowOnlyOneAndKeepStockNonnegative()
    {
        using var administrator = await CreateAuthenticatedClientAsync(RoleNames.Administrator);
        var (_, productId) = await CreateProductAsync(administrator, stock: 1);
        using var firstEmployee = await CreateAuthenticatedClientAsync(RoleNames.Employee);
        using var secondEmployee = await CreateAuthenticatedClientAsync(RoleNames.Employee);

        factory.SaleSaveChangesInterceptor.ArmForTwoConcurrentSales();
        var firstRequest = SendWithCsrfAsync(
            firstEmployee.Client,
            HttpMethod.Post,
            "/api/v1/sales",
            firstEmployee.CsrfToken,
            NewSale(productId, quantity: 1));
        var secondRequest = SendWithCsrfAsync(
            secondEmployee.Client,
            HttpMethod.Post,
            "/api/v1/sales",
            secondEmployee.CsrfToken,
            NewSale(productId, quantity: 1));
        using var firstResponse = await firstRequest;
        using var secondResponse = await secondRequest;

        Assert.Contains(new[] { firstResponse.StatusCode, secondResponse.StatusCode }, status => status == HttpStatusCode.Created);
        Assert.Contains(new[] { firstResponse.StatusCode, secondResponse.StatusCode }, status => status == HttpStatusCode.Conflict);
        Assert.Equal(0, (await GetProductAsync(administrator.Client, productId)).CurrentStock);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Equal(1, await dbContext.SaleItems.AsNoTracking().CountAsync(item => item.ProductId == productId));
        Assert.Equal(1, await dbContext.StockMovements.AsNoTracking().CountAsync(item =>
            item.ReferenceType == StockReferenceType.Sale && item.ProductId == productId));
    }

    private async Task<AuthenticatedClient> CreateAuthenticatedClientAsync(string role = RoleNames.Administrator)
    {
        var account = await factory.CreateUserAsync(role);
        var client = factory.CreateHttpsClient();
        var csrfToken = await GetCsrfTokenAsync(client);
        using var response = await SendWithCsrfAsync(
            client,
            HttpMethod.Post,
            "/api/v1/auth/login",
            csrfToken,
            new { account.UserName, account.Password });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        csrfToken = await GetCsrfTokenAsync(client);
        return new AuthenticatedClient(client, csrfToken);
    }

    private async Task<(int CategoryId, int ProductId)> CreateProductAsync(
        AuthenticatedClient client,
        int stock,
        decimal salePrice = 9m)
    {
        using var categoryResponse = await SendWithCsrfAsync(
            client.Client,
            HttpMethod.Post,
            "/api/v1/categories",
            client.CsrfToken,
            new { Name = $"sale-category-{Guid.NewGuid():N}", IsActive = true });
        Assert.True(categoryResponse.StatusCode == HttpStatusCode.Created,
            $"Category creation returned {(int)categoryResponse.StatusCode}: {await categoryResponse.Content.ReadAsStringAsync()}");
        var category = await categoryResponse.Content.ReadFromJsonAsync<CategoryResponse>(JsonOptions);
        Assert.NotNull(category);

        using var productResponse = await SendWithCsrfAsync(
            client.Client,
            HttpMethod.Post,
            "/api/v1/products",
            client.CsrfToken,
            new
            {
                SKU = $"SAL-{Guid.NewGuid():N}"[..20],
                Name = $"Sale product {Guid.NewGuid():N}"[..24],
                CategoryId = category.CategoryId,
                CostPrice = 4m,
                SalePrice = salePrice,
                MinimumStock = 0,
                IsActive = true
            });
        Assert.True(productResponse.StatusCode == HttpStatusCode.Created,
            $"Product creation returned {(int)productResponse.StatusCode}: {await productResponse.Content.ReadAsStringAsync()}");
        var product = await productResponse.Content.ReadFromJsonAsync<ProductResponse>(JsonOptions);
        Assert.NotNull(product);

        if (stock > 0)
        {
            using var adjustmentResponse = await SendWithCsrfAsync(
                client.Client,
                HttpMethod.Post,
                "/api/v1/inventory/adjustments",
                client.CsrfToken,
                new { ProductId = product.ProductId, QuantityDelta = stock, Reason = "Prepare sale test stock" });
            Assert.Equal(HttpStatusCode.OK, adjustmentResponse.StatusCode);
        }

        return (category.CategoryId, product.ProductId);
    }

    private static async Task<ProductResponse> GetProductAsync(HttpClient client, int productId)
    {
        using var response = await client.GetAsync($"/api/v1/products/{productId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<ProductResponse>(JsonOptions))!;
    }

    private static object NewSale(int productId, int quantity) => new
    {
        PaymentMethod = (int)PaymentMethod.Cash,
        Items = new[] { new { ProductId = productId, Quantity = quantity } }
    };

    private static async Task<string> GetCsrfTokenAsync(HttpClient client)
    {
        using var response = await client.GetAsync("/api/v1/auth/csrf");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<CsrfResponse>(JsonOptions);
        Assert.NotNull(payload);
        return payload.RequestToken;
    }

    private static async Task<HttpResponseMessage> SendWithCsrfAsync(
        HttpClient client,
        HttpMethod method,
        string path,
        string csrfToken,
        object? payload = null)
    {
        using var request = new HttpRequestMessage(method, path);
        if (payload is not null)
        {
            request.Content = JsonContent.Create(payload);
        }

        request.Headers.Add("X-CSRF-TOKEN", csrfToken);
        return await client.SendAsync(request);
    }

    private sealed record AuthenticatedClient(HttpClient Client, string CsrfToken) : IDisposable
    {
        public void Dispose() => Client.Dispose();
    }

    private sealed record CsrfResponse(string RequestToken);
    private sealed record CategoryResponse(int CategoryId);
    private sealed record ProductResponse(int ProductId, int CurrentStock);
    private sealed record SaleLineResponse(long SaleItemId, int ProductId, string SKU, string ProductName, int Quantity, decimal UnitPrice, decimal LineTotal);
    private sealed record SaleDetailResponse(long SaleId, string SaleNumber, int Status, decimal Subtotal, decimal Discount, decimal Total, int PaymentMethod, string CreatedByUserId, DateTime CreatedAtUtc, SaleLineResponse[] Items);
    private sealed record SaleSummaryResponse(long SaleId, string SaleNumber, decimal Total);
    private sealed record SalePageResponse(SaleSummaryResponse[] Items, int Page, int PageSize, int TotalCount, int TotalPages);
}
