using System.Security.Claims;
using GestionEmpresarial.Api.Authorization;
using GestionEmpresarial.Application.Sales;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionEmpresarial.Api.Controllers;

[ApiController]
[Authorize(Roles = RoleNames.Administrator + "," + RoleNames.Employee)]
[Route("api/v1/sales")]
public sealed class SalesController(ISaleService saleService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<SalePageResult<SaleSummaryDto>>> GetSales(
        [FromQuery] SaleQuery query,
        CancellationToken cancellationToken) =>
        Ok(await saleService.GetSalesAsync(
            query,
            CurrentUserId,
            User.IsInRole(RoleNames.Administrator),
            cancellationToken));

    [HttpGet("{id:long}")]
    public async Task<ActionResult<SaleDetailDto>> GetSale(long id, CancellationToken cancellationToken)
    {
        var sale = await saleService.GetSaleAsync(
            id,
            CurrentUserId,
            User.IsInRole(RoleNames.Administrator),
            cancellationToken);
        return sale is null ? NotFound() : Ok(sale);
    }

    [HttpPost]
    public async Task<ActionResult<SaleDetailDto>> CreateSale(
        SaleWriteRequest request,
        CancellationToken cancellationToken)
    {
        var sale = await saleService.CreateSaleAsync(
            request,
            CurrentUserId,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            cancellationToken);
        return CreatedAtAction(nameof(GetSale), new { id = sale.SaleId }, sale);
    }

    private string CurrentUserId =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new InvalidOperationException("The authenticated user has no identifier claim.");
}
