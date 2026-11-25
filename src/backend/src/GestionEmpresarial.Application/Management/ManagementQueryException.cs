using System.Net;
using GestionEmpresarial.Application.Common;

namespace GestionEmpresarial.Application.Management;

public sealed class ManagementQueryException(HttpStatusCode statusCode, string code, string message)
    : Exception(message), IApplicationServiceException
{
    public HttpStatusCode StatusCode { get; } = statusCode;
    public string Code { get; } = code;

    public static ManagementQueryException InvalidPeriod() =>
        new(HttpStatusCode.BadRequest, "invalid_date_range", "La fecha inicial no puede ser posterior a la fecha final.");
}
