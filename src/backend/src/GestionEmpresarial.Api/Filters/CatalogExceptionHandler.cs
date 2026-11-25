using GestionEmpresarial.Application.Common;
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
        if (exception is not IApplicationServiceException serviceException)
        {
            return false;
        }

        var statusCode = (int)serviceException.StatusCode;
        logger.LogInformation("Application request rejected with code {ServiceErrorCode} and status {StatusCode}.",
            serviceException.Code,
            statusCode);

        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/problem+json";
        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = serviceException.StatusCode == System.Net.HttpStatusCode.Conflict
                ? "Conflicto con los datos"
                : "Solicitud inválida",
            Detail = serviceException.Message
        };
        problem.Extensions["code"] = serviceException.Code;
        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
        return true;
    }
}
