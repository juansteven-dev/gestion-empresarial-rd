using GestionEmpresarial.Application.Catalogs;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace GestionEmpresarial.Api.Filters;

public sealed class CatalogExceptionHandler(ILogger<CatalogExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not CatalogServiceException catalogException)
        {
            return false;
        }

        var statusCode = (int)catalogException.StatusCode;
        logger.LogInformation("Catalog request rejected with code {CatalogErrorCode} and status {StatusCode}.",
            catalogException.Code,
            statusCode);

        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/problem+json";
        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = catalogException.StatusCode == System.Net.HttpStatusCode.Conflict
                ? "Conflicto con los datos"
                : "Solicitud inválida",
            Detail = catalogException.Message
        };
        problem.Extensions["code"] = catalogException.Code;
        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
        return true;
    }
}
