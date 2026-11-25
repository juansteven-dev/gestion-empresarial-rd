using System.Security.Claims;
using GestionEmpresarial.Api.Authorization;
using GestionEmpresarial.Application.Purchases;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionEmpresarial.Api.Controllers;

[ApiController]
[Authorize(Roles = RoleNames.Administrator + "," + RoleNames.WarehouseManager)]
[Route("api/v1/purchases")]
public sealed class PurchasesController(IPurchaseService purchaseService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PurchasePageResult<PurchaseSummaryDto>>> GetPurchases(
        [FromQuery] PurchaseQuery query,
        CancellationToken cancellationToken) =>
        Ok(await purchaseService.GetPurchasesAsync(query, cancellationToken));

    [HttpGet("{id:long}")]
    public async Task<ActionResult<PurchaseDetailDto>> GetPurchase(long id, CancellationToken cancellationToken)
    {
        var purchase = await purchaseService.GetPurchaseAsync(id, cancellationToken);
        return purchase is null ? NotFound() : Ok(purchase);
    }

    [HttpPost]
    public async Task<ActionResult<PurchaseDetailDto>> CreateDraft(
        PurchaseWriteRequest request,
        CancellationToken cancellationToken)
    {
        var purchase = await purchaseService.CreateDraftAsync(
            request,
            CurrentUserId,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            cancellationToken);
        return CreatedAtAction(nameof(GetPurchase), new { id = purchase.PurchaseId }, purchase);
    }

    [HttpPost("{id:long}/receive")]
    public async Task<ActionResult<PurchaseDetailDto>> Receive(long id, CancellationToken cancellationToken)
    {
        var purchase = await purchaseService.ReceivePurchaseAsync(
            id,
            CurrentUserId,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            cancellationToken);
        return purchase is null ? NotFound() : Ok(purchase);
    }

    private string CurrentUserId =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new InvalidOperationException("The authenticated user has no identifier claim.");
}
