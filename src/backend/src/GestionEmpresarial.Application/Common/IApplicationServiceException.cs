using System.Net;

namespace GestionEmpresarial.Application.Common;

public interface IApplicationServiceException
{
    HttpStatusCode StatusCode { get; }
    string Code { get; }
    string Message { get; }
}
