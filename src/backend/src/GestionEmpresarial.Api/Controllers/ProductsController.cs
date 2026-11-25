using System.Security.Claims;
using GestionEmpresarial.Api.Authorization;
using GestionEmpresarial.Application.Catalogs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionEmpresarial.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/products")]
public sealed class ProductsController(ICatalogService catalogService) : ControllerBase
{
    private const string CatalogManagerRoles = RoleNames.Administrator + "," + RoleNames.WarehouseManager;

    [HttpGet]
    public async Task<ActionResult<PageResult<ProductDto>>> GetProducts(
        [FromQuery] ProductQuery query,
        CancellationToken cancellationToken) =>
        Ok(await catalogService.GetProductsAsync(query, cancellationToken));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProductDto>> GetProduct(int id, CancellationToken cancellationToken)
    {
        var product = await catalogService.GetProductAsync(id, cancellationToken);
        return product is null ? NotFound() : Ok(product);
    }

    [HttpPost]
    [Authorize(Roles = CatalogManagerRoles)]
    public async Task<ActionResult<ProductDto>> CreateProduct(
        ProductWriteRequest request,
        CancellationToken cancellationToken)
    {
        var product = await catalogService.CreateProductAsync(
            request,
            CurrentUserId,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            cancellationToken);
        return CreatedAtAction(nameof(GetProduct), new { id = product.ProductId }, product);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = CatalogManagerRoles)]
    public async Task<ActionResult<ProductDto>> UpdateProduct(
        int id,
        ProductWriteRequest request,
        CancellationToken cancellationToken)
    {
        var product = await catalogService.UpdateProductAsync(
            id,
            request,
            CurrentUserId,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            cancellationToken);
        return product is null ? NotFound() : Ok(product);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = CatalogManagerRoles)]
    public async Task<IActionResult> DeactivateProduct(int id, CancellationToken cancellationToken)
    {
        var deactivated = await catalogService.DeactivateProductAsync(
            id,
            CurrentUserId,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            cancellationToken);
        return deactivated ? NoContent() : NotFound();
    }

    private string CurrentUserId =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new InvalidOperationException("The authenticated user has no identifier claim.");
}
