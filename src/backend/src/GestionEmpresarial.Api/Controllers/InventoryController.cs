using System.Security.Claims;
using GestionEmpresarial.Api.Authorization;
using GestionEmpresarial.Application.Catalogs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionEmpresarial.Api.Controllers;

[ApiController]
[Authorize(Roles = RoleNames.Administrator + "," + RoleNames.WarehouseManager)]
[Route("api/v1/inventory")]
public sealed class InventoryController(ICatalogService catalogService) : ControllerBase
{
    [HttpGet("stock")]
    public async Task<ActionResult<PageResult<ProductDto>>> GetStock(
        [FromQuery] ProductQuery query,
        CancellationToken cancellationToken) =>
        Ok(await catalogService.GetStockAsync(query, cancellationToken));

    [HttpGet("low-stock")]
    public async Task<ActionResult<PageResult<ProductDto>>> GetLowStock(
        [FromQuery] ProductQuery query,
        CancellationToken cancellationToken) =>
        Ok(await catalogService.GetLowStockAsync(query, cancellationToken));

    [HttpGet("movements")]
    public async Task<ActionResult<PageResult<StockMovementDto>>> GetMovements(
        [FromQuery] StockMovementQuery query,
        CancellationToken cancellationToken) =>
        Ok(await catalogService.GetMovementsAsync(query, cancellationToken));

    [HttpPost("adjustments")]
    public async Task<ActionResult<StockAdjustmentResult>> AdjustStock(
        StockAdjustmentRequest request,
        CancellationToken cancellationToken)
    {
        var result = await catalogService.AdjustStockAsync(
            request,
            CurrentUserId,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            cancellationToken);
        return Ok(result);
    }

    private string CurrentUserId =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new InvalidOperationException("The authenticated user has no identifier claim.");
}
