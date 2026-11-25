using System.Security.Claims;
using GestionEmpresarial.Api.Authorization;
using GestionEmpresarial.Application.Management;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionEmpresarial.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/dashboard")]
public sealed class DashboardController(IManagementQueryService queryService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<DashboardSummaryDto>> Get(
        [FromQuery] PeriodQuery query,
        CancellationToken cancellationToken)
    {
        var isAdministrator = User.IsInRole(RoleNames.Administrator);
        var canViewInventory = isAdministrator || User.IsInRole(RoleNames.WarehouseManager);
        var canViewSales = isAdministrator || User.IsInRole(RoleNames.Employee);
        return Ok(await queryService.GetDashboardAsync(
            query,
            User.FindFirstValue(ClaimTypes.NameIdentifier)!,
            canViewSales,
            isAdministrator,
            canViewInventory,
            cancellationToken));
    }
}
