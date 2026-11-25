# Backend

Generar la solución según `../../docs/00-inicio-vscode.md`. La solución debe contener:

- `GestionEmpresarial.Domain`: entidades, enums y reglas de dominio; no depende de otros proyectos.
- `GestionEmpresarial.Application`: casos de uso, contratos, DTOs, interfaces y validación; depende de Domain.
- `GestionEmpresarial.Infrastructure`: EF Core, SQL Server, Identity, repositorios, auditoría y servicios; depende de Domain/Application.
- `GestionEmpresarial.Api`: controladores, autenticación/autorización, middleware, OpenAPI y composición de dependencias.
- `tests/GestionEmpresarial.UnitTests` y `tests/GestionEmpresarial.IntegrationTests`.

El proyecto no se genera automáticamente al descomprimir este paquete; sigue los comandos documentados para evitar guardar archivos de build o una versión de SDK incorrecta.
