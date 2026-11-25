using System.Security.Claims;
using GestionEmpresarial.Api.Authorization;
using GestionEmpresarial.Application.Catalogs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionEmpresarial.Api.Controllers;

[ApiController]
[Authorize(Roles = RoleNames.Administrator + "," + RoleNames.WarehouseManager)]
[Route("api/v1/suppliers")]
public sealed class SuppliersController(ICatalogService catalogService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PageResult<SupplierDto>>> GetSuppliers(
        [FromQuery] SupplierQuery query,
        CancellationToken cancellationToken) =>
        Ok(await catalogService.GetSuppliersAsync(query, cancellationToken));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<SupplierDto>> GetSupplier(int id, CancellationToken cancellationToken)
    {
        var supplier = await catalogService.GetSupplierAsync(id, cancellationToken);
        return supplier is null ? NotFound() : Ok(supplier);
    }

    [HttpPost]
    public async Task<ActionResult<SupplierDto>> CreateSupplier(
        SupplierWriteRequest request,
        CancellationToken cancellationToken)
    {
        var supplier = await catalogService.CreateSupplierAsync(
            request,
            CurrentUserId,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            cancellationToken);
        return CreatedAtAction(nameof(GetSupplier), new { id = supplier.SupplierId }, supplier);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<SupplierDto>> UpdateSupplier(
        int id,
        SupplierWriteRequest request,
        CancellationToken cancellationToken)
    {
        var supplier = await catalogService.UpdateSupplierAsync(
            id,
            request,
            CurrentUserId,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            cancellationToken);
        return supplier is null ? NotFound() : Ok(supplier);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeactivateSupplier(int id, CancellationToken cancellationToken)
    {
        var deactivated = await catalogService.DeactivateSupplierAsync(
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
