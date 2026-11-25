using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GestionEmpresarial.Api.Authorization;
using GestionEmpresarial.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GestionEmpresarial.UnitTests;

[Collection(ApiIntegrationCollection.Name)]
public sealed class CatalogEndpointTests(ApiFactory factory)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task Categories_ArePagedAndCannotBeDeactivatedWhileTheyHaveActiveProducts()
    {
        using var client = await CreateAuthenticatedClientAsync(RoleNames.Administrator);
        var csrfToken = await GetCsrfTokenAsync(client);
        var prefix = $"paging-{Guid.NewGuid():N}";
        var categoryIds = new List<int>();

        for (var index = 0; index < 3; index++)
        {
            using var response = await SendWithCsrfAsync(
                client,
                HttpMethod.Post,
                "/api/v1/categories",
                csrfToken,
                new { Name = $"{prefix}-{index}", IsActive = true });
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            categoryIds.Add((await response.Content.ReadFromJsonAsync<CategoryResponse>(JsonOptions))!.CategoryId);
        }

        using var firstPageResponse = await client.GetAsync(
            $"/api/v1/categories?search={prefix}&page=1&pageSize=2");
        var firstPage = await firstPageResponse.Content.ReadFromJsonAsync<PageResponse<CategoryResponse>>(JsonOptions);
        Assert.Equal(HttpStatusCode.OK, firstPageResponse.StatusCode);
        Assert.NotNull(firstPage);
        Assert.Equal(3, firstPage.TotalCount);
        Assert.Equal(2, firstPage.Items.Length);
        Assert.Equal(2, firstPage.TotalPages);

        using var secondPageResponse = await client.GetAsync(
            $"/api/v1/categories?search={prefix}&page=2&pageSize=2");
        var secondPage = await secondPageResponse.Content.ReadFromJsonAsync<PageResponse<CategoryResponse>>(JsonOptions);
        Assert.NotNull(secondPage);
        Assert.Single(secondPage.Items);

        using var updateCategoryResponse = await SendWithCsrfAsync(
            client,
            HttpMethod.Put,
            $"/api/v1/categories/{categoryIds[0]}",
            csrfToken,
            new { Name = $"{prefix}-renamed", Description = "Updated category", IsActive = true });
        Assert.Equal(HttpStatusCode.OK, updateCategoryResponse.StatusCode);

        using var productResponse = await SendWithCsrfAsync(
            client,
            HttpMethod.Post,
            "/api/v1/products",
            csrfToken,
            NewProduct($"guard-{Guid.NewGuid():N}", "category guard", categoryIds[0]));
        Assert.Equal(HttpStatusCode.Created, productResponse.StatusCode);

        using var deactivateResponse = await SendWithCsrfAsync(
            client,
            HttpMethod.Delete,
            $"/api/v1/categories/{categoryIds[0]}",
            csrfToken);
        Assert.Equal(HttpStatusCode.Conflict, deactivateResponse.StatusCode);

        using var updateInactiveResponse = await SendWithCsrfAsync(
            client,
            HttpMethod.Put,
            $"/api/v1/categories/{categoryIds[0]}",
            csrfToken,
            new { Name = $"{prefix}-renamed", IsActive = false });
        Assert.Equal(HttpStatusCode.Conflict, updateInactiveResponse.StatusCode);
    }

    [Fact]
    public async Task Products_RejectDuplicateSkuInvalidDataAndMissingCategory()
    {
        using var client = await CreateAuthenticatedClientAsync(RoleNames.WarehouseManager);
        var csrfToken = await GetCsrfTokenAsync(client);
        var categoryId = await CreateCategoryAsync(client, csrfToken, $"products-{Guid.NewGuid():N}");
        var sku = $"sku-{Guid.NewGuid():N}";

        using var createResponse = await SendWithCsrfAsync(
            client,
            HttpMethod.Post,
            "/api/v1/products",
            csrfToken,
            NewProduct(sku, "Valid product", categoryId));
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        using var duplicateResponse = await SendWithCsrfAsync(
            client,
            HttpMethod.Post,
            "/api/v1/products",
            csrfToken,
            NewProduct(sku.ToLowerInvariant(), "Duplicate SKU", categoryId));
        Assert.Equal(HttpStatusCode.Conflict, duplicateResponse.StatusCode);

        using var invalidResponse = await SendWithCsrfAsync(
            client,
            HttpMethod.Post,
            "/api/v1/products",
            csrfToken,
            new
            {
                SKU = $"invalid-{Guid.NewGuid():N}",
                Name = "Invalid price",
                CategoryId = categoryId,
                CostPrice = 0m,
                SalePrice = -1m,
                MinimumStock = 0,
                IsActive = true
            });
        Assert.Equal(HttpStatusCode.BadRequest, invalidResponse.StatusCode);

        using var precisionResponse = await SendWithCsrfAsync(
            client,
            HttpMethod.Post,
            "/api/v1/products",
            csrfToken,
            new
            {
                SKU = $"precision-{Guid.NewGuid():N}",
                Name = "Precise amount",
                CategoryId = categoryId,
                CostPrice = 1.001m,
                SalePrice = 2m,
                MinimumStock = 0,
                IsActive = true
            });
        Assert.Equal(HttpStatusCode.BadRequest, precisionResponse.StatusCode);

        using var missingCategoryResponse = await SendWithCsrfAsync(
            client,
            HttpMethod.Post,
            "/api/v1/products",
            csrfToken,
            NewProduct($"missing-{Guid.NewGuid():N}", "Missing category", int.MaxValue));
        Assert.Equal(HttpStatusCode.BadRequest, missingCategoryResponse.StatusCode);
    }

    [Fact]
    public async Task Inventory_RequiresReasonAndRejectsNegativeStockThenAuditsValidAdjustment()
    {
        using var client = await CreateAuthenticatedClientAsync(RoleNames.Administrator);
        var csrfToken = await GetCsrfTokenAsync(client);
        var categoryId = await CreateCategoryAsync(client, csrfToken, $"inventory-{Guid.NewGuid():N}");
        var productId = await CreateProductAsync(
            client,
            csrfToken,
            NewProduct($"stock-{Guid.NewGuid():N}", "Stock test", categoryId, minimumStock: 5));

        using var missingReasonResponse = await SendWithCsrfAsync(
            client,
            HttpMethod.Post,
            "/api/v1/inventory/adjustments",
            csrfToken,
            new { ProductId = productId, QuantityDelta = 2, Reason = "" });
        Assert.Equal(HttpStatusCode.BadRequest, missingReasonResponse.StatusCode);

        using var negativeResponse = await SendWithCsrfAsync(
            client,
            HttpMethod.Post,
            "/api/v1/inventory/adjustments",
            csrfToken,
            new { ProductId = productId, QuantityDelta = -1, Reason = "Correction test" });
        Assert.Equal(HttpStatusCode.Conflict, negativeResponse.StatusCode);

        using var successfulResponse = await SendWithCsrfAsync(
            client,
            HttpMethod.Post,
            "/api/v1/inventory/adjustments",
            csrfToken,
            new { ProductId = productId, QuantityDelta = 4, Reason = "Initial count" });
        Assert.Equal(HttpStatusCode.OK, successfulResponse.StatusCode);
        var result = await successfulResponse.Content.ReadFromJsonAsync<AdjustmentResponse>(JsonOptions);
        Assert.NotNull(result);
        Assert.Equal(4, result.Product.CurrentStock);
        Assert.Equal(0, result.Movement.StockBefore);
        Assert.Equal(4, result.Movement.StockAfter);
        Assert.Equal("Initial count", result.Movement.Notes);

        using var lowStockResponse = await client.GetAsync("/api/v1/inventory/low-stock?page=1&pageSize=5");
        var lowStock = await lowStockResponse.Content.ReadFromJsonAsync<PageResponse<ProductResponse>>(JsonOptions);
        Assert.NotNull(lowStock);
        Assert.Contains(lowStock.Items, product => product.ProductId == productId);

        using var movementResponse = await client.GetAsync(
            $"/api/v1/inventory/movements?productId={productId}&page=1&pageSize=5");
        var movements = await movementResponse.Content.ReadFromJsonAsync<PageResponse<MovementResponse>>(JsonOptions);
        Assert.NotNull(movements);
        var movement = Assert.Single(movements.Items);
        Assert.Equal(4, movement.QuantityDelta);
        Assert.Equal("Initial count", movement.Notes);

        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var audit = await dbContext.AuditLogs.AsNoTracking().SingleAsync(log =>
                log.Action == "StockAdjusted" && log.EntityId == productId.ToString());
            Assert.Contains("Initial count", audit.DetailsJson, StringComparison.Ordinal);
        }

        using var updateProductResponse = await SendWithCsrfAsync(
            client,
            HttpMethod.Put,
            $"/api/v1/products/{productId}",
            csrfToken,
            NewProduct(result.Product.SKU, "Updated stock test", categoryId, minimumStock: 6));
        Assert.Equal(HttpStatusCode.OK, updateProductResponse.StatusCode);
        var updatedProduct = await updateProductResponse.Content.ReadFromJsonAsync<ProductResponse>(JsonOptions);
        Assert.NotNull(updatedProduct);
        Assert.Equal(4, updatedProduct.CurrentStock);

        using var deactivateProductResponse = await SendWithCsrfAsync(
            client,
            HttpMethod.Delete,
            $"/api/v1/products/{productId}",
            csrfToken);
        Assert.Equal(HttpStatusCode.NoContent, deactivateProductResponse.StatusCode);
        using var getInactiveProductResponse = await client.GetAsync($"/api/v1/products/{productId}");
        var inactiveProduct = await getInactiveProductResponse.Content.ReadFromJsonAsync<ProductResponse>(JsonOptions);
        Assert.NotNull(inactiveProduct);
        Assert.False(inactiveProduct.IsActive);
        Assert.Equal(4, inactiveProduct.CurrentStock);
    }

    [Fact]
    public async Task Suppliers_CanBeDeactivatedWithoutDeletingTheRecord()
    {
        using var client = await CreateAuthenticatedClientAsync(RoleNames.Administrator);
        var csrfToken = await GetCsrfTokenAsync(client);
        using var createResponse = await SendWithCsrfAsync(
            client,
            HttpMethod.Post,
            "/api/v1/suppliers",
            csrfToken,
            new { Name = $"supplier-{Guid.NewGuid():N}", Email = "supplier@example.test", IsActive = true });
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var supplier = await createResponse.Content.ReadFromJsonAsync<SupplierResponse>(JsonOptions);
        Assert.NotNull(supplier);

        using var updateResponse = await SendWithCsrfAsync(
            client,
            HttpMethod.Put,
            $"/api/v1/suppliers/{supplier.SupplierId}",
            csrfToken,
            new { Name = "Updated supplier", Email = "updated@example.test", IsActive = true });
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        using var deactivateResponse = await SendWithCsrfAsync(
            client,
            HttpMethod.Delete,
            $"/api/v1/suppliers/{supplier.SupplierId}",
            csrfToken);
        Assert.Equal(HttpStatusCode.NoContent, deactivateResponse.StatusCode);

        using var getResponse = await client.GetAsync($"/api/v1/suppliers/{supplier.SupplierId}");
        var deactivated = await getResponse.Content.ReadFromJsonAsync<SupplierResponse>(JsonOptions);
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        Assert.NotNull(deactivated);
        Assert.False(deactivated.IsActive);
        Assert.Equal("Updated supplier", deactivated.Name);
    }

    [Fact]
    public async Task Employee_CanReadProductsButCannotManageCatalogOrInventory()
    {
        using var client = await CreateAuthenticatedClientAsync(RoleNames.Employee);

        using var productsResponse = await client.GetAsync("/api/v1/products?page=1&pageSize=5");
        Assert.Equal(HttpStatusCode.OK, productsResponse.StatusCode);

        using var categoriesResponse = await client.GetAsync("/api/v1/categories");
        Assert.Equal(HttpStatusCode.OK, categoriesResponse.StatusCode);

        var csrfToken = await GetCsrfTokenAsync(client);
        using var createCategoryResponse = await SendWithCsrfAsync(
            client,
            HttpMethod.Post,
            "/api/v1/categories",
            csrfToken,
            new { Name = $"employee-category-{Guid.NewGuid():N}", IsActive = true });
        Assert.Equal(HttpStatusCode.Forbidden, createCategoryResponse.StatusCode);

        using var inventoryResponse = await client.GetAsync("/api/v1/inventory/stock");
        Assert.Equal(HttpStatusCode.Forbidden, inventoryResponse.StatusCode);
    }

    [Fact]
    public async Task CatalogEndpoints_RequireAnAuthenticatedUser()
    {
        using var client = factory.CreateHttpsClient();

        using var response = await client.GetAsync("/api/v1/products");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private async Task<HttpClient> CreateAuthenticatedClientAsync(string role)
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
        return client;
    }

    private static async Task<int> CreateCategoryAsync(HttpClient client, string csrfToken, string name)
    {
        using var response = await SendWithCsrfAsync(
            client,
            HttpMethod.Post,
            "/api/v1/categories",
            csrfToken,
            new { Name = name, IsActive = true });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<CategoryResponse>(JsonOptions))!.CategoryId;
    }

    private static async Task<int> CreateProductAsync(HttpClient client, string csrfToken, object product)
    {
        using var response = await SendWithCsrfAsync(client, HttpMethod.Post, "/api/v1/products", csrfToken, product);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<ProductResponse>(JsonOptions))!.ProductId;
    }

    private static object NewProduct(string sku, string name, int categoryId, int minimumStock = 0) => new
    {
        SKU = sku,
        Name = name,
        Description = "Catalog integration test",
        CategoryId = categoryId,
        CostPrice = 10.25m,
        SalePrice = 15.75m,
        MinimumStock = minimumStock,
        IsActive = true
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

    private sealed record CsrfResponse(string RequestToken);
    private sealed record CategoryResponse(int CategoryId, string Name, string? Description, bool IsActive);
    private sealed record ProductResponse(int ProductId, string SKU, string Name, int CategoryId, string CategoryName, decimal CostPrice, decimal SalePrice, int CurrentStock, int MinimumStock, bool IsActive);
    private sealed record SupplierResponse(int SupplierId, string Name, string? TaxId, string? Phone, string? Email, string? Address, bool IsActive);
    private sealed record MovementResponse(long StockMovementId, int ProductId, string SKU, string ProductName, int Type, int QuantityDelta, int StockBefore, int StockAfter, string? Notes, string CreatedByUserId, DateTime CreatedAtUtc);
    private sealed record AdjustmentResponse(ProductResponse Product, MovementResponse Movement);
    private sealed record PageResponse<T>(T[] Items, int Page, int PageSize, int TotalCount, int TotalPages);
}
