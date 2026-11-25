# Backend

La solución ya está creada. No vuelvas a generar los proyectos; sigue la arquitectura descrita en `../../docs/04-arquitectura-y-diagramas.md`:

- `GestionEmpresarial.Domain`: entidades, enums y reglas de dominio; no depende de otros proyectos.
- `GestionEmpresarial.Application`: casos de uso, contratos, DTOs, interfaces y validación; depende de Domain.
- `GestionEmpresarial.Infrastructure`: EF Core, SQL Server, Identity, repositorios, auditoría y servicios; depende de Domain/Application.
- `GestionEmpresarial.Api`: controladores, autenticación/autorización, middleware, OpenAPI y composición de dependencias.
- `tests/GestionEmpresarial.UnitTests` y `tests/GestionEmpresarial.IntegrationTests`.

## Ejecución local

- API HTTPS: `https://localhost:7024` (perfil `https` de `Properties/launchSettings.json`).
- API HTTP: `http://localhost:5146` (perfil `http`).
- Health check: `https://localhost:7024/health`.
- Frontend Vite: `http://localhost:5173`; las llamadas `/api/...` se reenvían al API HTTPS mediante `src/frontend/vite.config.ts`.

Desde la raíz del repositorio, ejecuta el API con `dotnet run --project src/backend/src/GestionEmpresarial.Api --launch-profile https` y el frontend con `npm.cmd run dev` desde `src/frontend`.

El API lee `ConnectionStrings:DefaultConnection` desde configuración. En desarrollo, .NET carga User Secrets; en otros entornos puede usarse `ConnectionStrings__DefaultConnection`. No agregues la cadena a `appsettings.json` ni a otro archivo versionado. User Secrets ya está inicializado para el proyecto.
