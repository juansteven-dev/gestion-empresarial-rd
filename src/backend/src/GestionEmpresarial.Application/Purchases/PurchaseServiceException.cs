using System.Net;
using GestionEmpresarial.Application.Common;

namespace GestionEmpresarial.Application.Purchases;

public sealed class PurchaseServiceException : Exception, IApplicationServiceException
{
    public PurchaseServiceException(HttpStatusCode statusCode, string code, string message)
        : base(message)
    {
        StatusCode = statusCode;
        Code = code;
    }

    public HttpStatusCode StatusCode { get; }
    public string Code { get; }

    public static PurchaseServiceException BadRequest(string code, string message) =>
        new(HttpStatusCode.BadRequest, code, message);

    public static PurchaseServiceException Conflict(string code, string message) =>
        new(HttpStatusCode.Conflict, code, message);
}
