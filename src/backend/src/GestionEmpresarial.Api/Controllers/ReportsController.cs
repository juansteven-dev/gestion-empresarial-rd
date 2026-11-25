using GestionEmpresarial.Api.Authorization;
using GestionEmpresarial.Application.Management;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionEmpresarial.Api.Controllers;

[ApiController]
[Authorize(Roles = RoleNames.Administrator)]
[Route("api/v1/reports")]
public sealed class ReportsController(IManagementQueryService queryService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ReportsSummaryDto>> Get(
        [FromQuery] PeriodQuery query,
        CancellationToken cancellationToken) =>
        Ok(await queryService.GetReportsAsync(query, cancellationToken));
}
