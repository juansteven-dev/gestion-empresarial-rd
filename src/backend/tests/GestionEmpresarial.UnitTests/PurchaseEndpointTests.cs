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
public sealed class PurchaseEndpointTests(ApiFactory factory)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task CreatePurchase_RejectsEmptyLinesAndUnknownSupplier()
    {
        using var client = await CreateAuthenticatedClientAsync();
        var csrfToken = await GetCsrfTokenAsync(client);
        var supplierId = await CreateSupplierAsync(client, csrfToken);
        var categoryId = await CreateCategoryAsync(client, csrfToken);
        var productId = await CreateProductAsync(client, csrfToken, categoryId);

        using var emptyResponse = await SendWithCsrfAsync(
            client,
            HttpMethod.Post,
            "/api/v1/purchases",
            csrfToken,
            new { SupplierId = supplierId, Items = Array.Empty<object>() });
        Assert.Equal(HttpStatusCode.BadRequest, emptyResponse.StatusCode);

        using var invalidSupplierResponse = await SendWithCsrfAsync(
            client,
            HttpMethod.Post,
            "/api/v1/purchases",
            csrfToken,
            NewPurchase(int.MaxValue, productId));
        Assert.Equal(HttpStatusCode.BadRequest, invalidSupplierResponse.StatusCode);

        using var invalidLineResponse = await SendWithCsrfAsync(
            client,
            HttpMethod.Post,
            "/api/v1/purchases",
            csrfToken,
            NewPurchase(supplierId, productId, quantity: 0));
        Assert.Equal(HttpStatusCode.BadRequest, invalidLineResponse.StatusCode);
    }

    [Fact]
    public async Task CreateDraft_DoesNotIncreaseStockOrCreatePurchaseMovements()
    {
        using var client = await CreateAuthenticatedClientAsync();
        var csrfToken = await GetCsrfTokenAsync(client);
        var supplierId = await CreateSupplierAsync(client, csrfToken);
        var categoryId = await CreateCategoryAsync(client, csrfToken);
        var productId = await CreateProductAsync(client, csrfToken, categoryId);

        using var response = await SendWithCsrfAsync(
            client,
            HttpMethod.Post,
            "/api/v1/purchases",
            csrfToken,
            NewPurchase(supplierId, productId, quantity: 5, unitCost: 12.50m));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var purchase = await response.Content.ReadFromJsonAsync<PurchaseDetailResponse>(JsonOptions);
        Assert.NotNull(purchase);
        Assert.Equal(0, purchase.Status);
        Assert.Equal(62.50m, purchase.Total);
        Assert.Single(purchase.Items);

        using var listResponse = await client.GetAsync("/api/v1/purchases?status=Draft&page=1&pageSize=1");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        var purchasePage = await listResponse.Content.ReadFromJsonAsync<PurchasePageResponse>(JsonOptions);
        Assert.NotNull(purchasePage);
        Assert.Equal(1, purchasePage.PageSize);
        Assert.Contains(purchasePage.Items, item => item.PurchaseId == purchase.PurchaseId);

        var product = await GetProductAsync(client, productId);
        Assert.Equal(0, product.CurrentStock);
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Empty(await dbContext.StockMovements.AsNoTracking()
            .Where(item => item.ReferenceType == StockReferenceType.Purchase && item.ReferenceId == purchase.PurchaseId)
            .ToListAsync());
    }

    [Fact]
    public async Task ReceivePurchase_IncreasesStockAuditsAndRejectsASecondReceipt()
    {
        using var client = await CreateAuthenticatedClientAsync();
        var csrfToken = await GetCsrfTokenAsync(client);
        var supplierId = await CreateSupplierAsync(client, csrfToken);
        var categoryId = await CreateCategoryAsync(client, csrfToken);
        var productId = await CreateProductAsync(client, csrfToken, categoryId);
        var draft = await CreateDraftAsync(client, csrfToken, supplierId, productId, quantity: 4);

        using var receiveResponse = await SendWithCsrfAsync(
            client,
            HttpMethod.Post,
            $"/api/v1/purchases/{draft.PurchaseId}/receive",
            csrfToken);
        Assert.Equal(HttpStatusCode.OK, receiveResponse.StatusCode);
        var received = await receiveResponse.Content.ReadFromJsonAsync<PurchaseDetailResponse>(JsonOptions);
        Assert.NotNull(received);
        Assert.Equal(1, received.Status);
        Assert.NotNull(received.ReceivedAtUtc);

        var product = await GetProductAsync(client, productId);
        Assert.Equal(4, product.CurrentStock);

        using var secondReceiveResponse = await SendWithCsrfAsync(
            client,
            HttpMethod.Post,
            $"/api/v1/purchases/{draft.PurchaseId}/receive",
            csrfToken);
        Assert.Equal(HttpStatusCode.Conflict, secondReceiveResponse.StatusCode);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var movement = await dbContext.StockMovements.AsNoTracking().SingleAsync(item =>
            item.ReferenceType == StockReferenceType.Purchase && item.ReferenceId == draft.PurchaseId);
        Assert.Equal(MovementType.Purchase, movement.MovementType);
        Assert.Equal(4, movement.QuantityDelta);
        Assert.Equal(0, movement.StockBefore);
        Assert.Equal(4, movement.StockAfter);

        var audit = await dbContext.AuditLogs.AsNoTracking().SingleAsync(item =>
            item.Action == "PurchaseReceived" && item.EntityId == draft.PurchaseId.ToString());
        Assert.Contains(draft.PurchaseNumber, audit.DetailsJson, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ReceivePurchase_WhenALaterLineIsInvalid_LeavesNoPartialChanges()
    {
        using var client = await CreateAuthenticatedClientAsync();
        var csrfToken = await GetCsrfTokenAsync(client);
        var supplierId = await CreateSupplierAsync(client, csrfToken);
        var categoryId = await CreateCategoryAsync(client, csrfToken);
        var firstProductId = await CreateProductAsync(client, csrfToken, categoryId);
        var secondProductId = await CreateProductAsync(client, csrfToken, categoryId);
        await AdjustStockAsync(client, csrfToken, firstProductId, 7);

        var draft = await CreateDraftAsync(
            client,
            csrfToken,
            supplierId,
            [
                new PurchaseLineRequest(firstProductId, 3, 5m),
                new PurchaseLineRequest(secondProductId, 2, 8m)
            ]);

        using var deactivateResponse = await SendWithCsrfAsync(
            client,
            HttpMethod.Delete,
            $"/api/v1/products/{secondProductId}",
            csrfToken);
        Assert.Equal(HttpStatusCode.NoContent, deactivateResponse.StatusCode);

        using var receiveResponse = await SendWithCsrfAsync(
            client,
            HttpMethod.Post,
            $"/api/v1/purchases/{draft.PurchaseId}/receive",
            csrfToken);
        Assert.Equal(HttpStatusCode.Conflict, receiveResponse.StatusCode);

        var firstProduct = await GetProductAsync(client, firstProductId);
        Assert.Equal(7, firstProduct.CurrentStock);
        using var purchaseResponse = await client.GetAsync($"/api/v1/purchases/{draft.PurchaseId}");
        var stillDraft = await purchaseResponse.Content.ReadFromJsonAsync<PurchaseDetailResponse>(JsonOptions);
        Assert.NotNull(stillDraft);
        Assert.Equal(0, stillDraft.Status);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Empty(await dbContext.StockMovements.AsNoTracking()
            .Where(item => item.ReferenceType == StockReferenceType.Purchase && item.ReferenceId == draft.PurchaseId)
            .ToListAsync());
        Assert.False(await dbContext.AuditLogs.AsNoTracking().AnyAsync(item =>
            item.Action == "PurchaseReceived" && item.EntityId == draft.PurchaseId.ToString()));
    }

    [Fact]
    public async Task Employee_CannotAccessPurchaseEndpoints()
    {
        using var client = await CreateAuthenticatedClientAsync(RoleNames.Employee);

        using var response = await client.GetAsync("/api/v1/purchases");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private async Task<HttpClient> CreateAuthenticatedClientAsync(string role = RoleNames.WarehouseManager)
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
        return client;
    }

    private static async Task<int> CreateSupplierAsync(HttpClient client, string csrfToken)
    {
        using var response = await SendWithCsrfAsync(
            client,
            HttpMethod.Post,
            "/api/v1/suppliers",
            csrfToken,
            new { Name = $"purchase-supplier-{Guid.NewGuid():N}", IsActive = true });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var supplier = await response.Content.ReadFromJsonAsync<SupplierResponse>(JsonOptions);
        Assert.NotNull(supplier);
        return supplier.SupplierId;
    }

    private static async Task<int> CreateCategoryAsync(HttpClient client, string csrfToken)
    {
        using var response = await SendWithCsrfAsync(
            client,
            HttpMethod.Post,
            "/api/v1/categories",
            csrfToken,
            new { Name = $"purchase-category-{Guid.NewGuid():N}", IsActive = true });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var category = await response.Content.ReadFromJsonAsync<CategoryResponse>(JsonOptions);
        Assert.NotNull(category);
        return category.CategoryId;
    }

    private static async Task<int> CreateProductAsync(HttpClient client, string csrfToken, int categoryId)
    {
        using var response = await SendWithCsrfAsync(
            client,
            HttpMethod.Post,
            "/api/v1/products",
            csrfToken,
            new
            {
                SKU = $"PUR-{Guid.NewGuid():N}"[..20],
                Name = $"Purchase product {Guid.NewGuid():N}"[..27],
                CategoryId = categoryId,
                CostPrice = 5m,
                SalePrice = 9m,
                MinimumStock = 0,
                IsActive = true
            });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var product = await response.Content.ReadFromJsonAsync<ProductResponse>(JsonOptions);
        Assert.NotNull(product);
        return product.ProductId;
    }

    private static async Task AdjustStockAsync(HttpClient client, string csrfToken, int productId, int delta)
    {
        using var response = await SendWithCsrfAsync(
            client,
            HttpMethod.Post,
            "/api/v1/inventory/adjustments",
            csrfToken,
            new { ProductId = productId, QuantityDelta = delta, Reason = "Prepare purchase rollback test" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private static async Task<ProductResponse> GetProductAsync(HttpClient client, int productId)
    {
        using var response = await client.GetAsync($"/api/v1/products/{productId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<ProductResponse>(JsonOptions))!;
    }

    private static Task<PurchaseDetailResponse> CreateDraftAsync(
        HttpClient client,
        string csrfToken,
        int supplierId,
        int productId,
        int quantity = 3) =>
        CreateDraftAsync(client, csrfToken, supplierId, [new PurchaseLineRequest(productId, quantity, 10m)]);

    private static async Task<PurchaseDetailResponse> CreateDraftAsync(
        HttpClient client,
        string csrfToken,
        int supplierId,
        PurchaseLineRequest[] items)
    {
        using var response = await SendWithCsrfAsync(
            client,
            HttpMethod.Post,
            "/api/v1/purchases",
            csrfToken,
            new { SupplierId = supplierId, Items = items });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<PurchaseDetailResponse>(JsonOptions))!;
    }

    private static object NewPurchase(int supplierId, int productId, int quantity = 1, decimal unitCost = 1m) => new
    {
        SupplierId = supplierId,
        Items = new[] { new { ProductId = productId, Quantity = quantity, UnitCost = unitCost } }
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
    private sealed record SupplierResponse(int SupplierId);
    private sealed record CategoryResponse(int CategoryId);
    private sealed record ProductResponse(int ProductId, int CurrentStock);
    private sealed record PurchaseLineRequest(int ProductId, int Quantity, decimal UnitCost);
    private sealed record PurchaseLineResponse(long PurchaseItemId, int ProductId, int Quantity, decimal UnitCost, decimal LineTotal);
    private sealed record PurchaseDetailResponse(long PurchaseId, string PurchaseNumber, int Status, decimal Total, DateTime? ReceivedAtUtc, PurchaseLineResponse[] Items);
    private sealed record PurchaseSummaryResponse(long PurchaseId, string PurchaseNumber, int Status, decimal Total);
    private sealed record PurchasePageResponse(PurchaseSummaryResponse[] Items, int Page, int PageSize, int TotalCount, int TotalPages);
}
