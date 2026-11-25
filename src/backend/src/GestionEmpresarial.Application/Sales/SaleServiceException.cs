using System.Net;
using GestionEmpresarial.Application.Common;

namespace GestionEmpresarial.Application.Sales;

public sealed class SaleServiceException(HttpStatusCode statusCode, string code, string message)
    : Exception(message), IApplicationServiceException
{
    public HttpStatusCode StatusCode { get; } = statusCode;
    public string Code { get; } = code;

    public static SaleServiceException BadRequest(string code, string message) =>
        new(HttpStatusCode.BadRequest, code, message);

    public static SaleServiceException Conflict(string code, string message) =>
        new(HttpStatusCode.Conflict, code, message);
}
