using GestionEmpresarial.Api.Authorization;
using GestionEmpresarial.Application.Management;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionEmpresarial.Api.Controllers;

[ApiController]
[Authorize(Roles = RoleNames.Administrator)]
[Route("api/v1/audit")]
public sealed class AuditController(IManagementQueryService queryService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ManagementPageResult<AuditLogDto>>> Get(
        [FromQuery] AuditLogQuery query,
        CancellationToken cancellationToken) =>
        Ok(await queryService.GetAuditLogsAsync(query, cancellationToken));
}
