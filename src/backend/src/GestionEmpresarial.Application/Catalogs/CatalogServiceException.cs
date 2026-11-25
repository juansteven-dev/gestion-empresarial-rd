using System.Net;

namespace GestionEmpresarial.Application.Catalogs;

public sealed class CatalogServiceException(HttpStatusCode statusCode, string code, string message)
    : Exception(message)
{
    public HttpStatusCode StatusCode { get; } = statusCode;
    public string Code { get; } = code;

    public static CatalogServiceException BadRequest(string code, string message) =>
        new(HttpStatusCode.BadRequest, code, message);

    public static CatalogServiceException Conflict(string code, string message) =>
        new(HttpStatusCode.Conflict, code, message);
}
