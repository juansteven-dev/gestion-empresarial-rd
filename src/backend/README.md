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
## Autenticación y primer administrador

El API usa ASP.NET Core Identity y cookies de sesión `HttpOnly`, `Secure` y `SameSite=Lax` con expiración de ocho horas. El navegador solicita `GET /api/v1/auth/csrf` y envía el token recibido en `X-CSRF-TOKEN` para cada operación de escritura. Los endpoints son `POST /api/v1/auth/login`, `POST /api/v1/auth/logout` y `GET /api/v1/auth/me`. No hay registro público. `POST /api/v1/users` crea cuentas y exige el rol `Administrador`; solo acepta `Administrador`, `Empleado` o `EncargadoAlmacen`.

Para crear el primer administrador local, configura los secretos desde la raíz del repositorio. Sustituye los valores de ejemplo; no los guardes en archivos versionados:

```powershell
dotnet user-secrets set --project src/backend/src/GestionEmpresarial.Api "BootstrapAdmin:UserName" "TU_USUARIO_LOCAL"
dotnet user-secrets set --project src/backend/src/GestionEmpresarial.Api "BootstrapAdmin:Email" "TU_CORREO_LOCAL"
dotnet user-secrets set --project src/backend/src/GestionEmpresarial.Api "BootstrapAdmin:Password" "TU_CLAVE_SEGURA"
dotnet run --project src/backend/src/GestionEmpresarial.Api --launch-profile https -- --bootstrap-admin
dotnet user-secrets remove --project src/backend/src/GestionEmpresarial.Api "BootstrapAdmin:Password"
dotnet user-secrets remove --project src/backend/src/GestionEmpresarial.Api "BootstrapAdmin:UserName"
dotnet user-secrets remove --project src/backend/src/GestionEmpresarial.Api "BootstrapAdmin:Email"
```

El arranque normal crea los tres roles de forma idempotente. El comando `--bootstrap-admin` solo crea una cuenta si todavía no existe un administrador; no restablece contraseñas. Identity aplica una clave de al menos 12 caracteres y bloquea la cuenta durante 15 minutos después de cinco intentos fallidos.

Las pruebas de autenticación se ejecutan con `dotnet test src/backend/GestionEmpresarial.slnx`.

## Catalogos e inventario (Fase B)

La API expone DTOs paginados bajo /api/v1. Las categorias y suplidores se administran en /categories y /suppliers; la consulta de productos esta disponible para cualquier usuario autenticado en /products. Productos, categorias, suplidores y operaciones de inventario se pueden modificar por Administrador y EncargadoAlmacen. El servidor aplica los mismos permisos aunque se invoque la API directamente.

- GET /api/v1/products: busca por search (nombre o SKU), filtra con categoryId, isActive y lowStockOnly, y pagina con page/pageSize (maximo 100). Admite GET /{id}, POST, PUT /{id} y DELETE /{id}; DELETE marca el producto inactivo.
- /api/v1/categories: cualquier usuario autenticado puede consultar con GET y GET /{id}; POST, PUT /{id} y DELETE /{id} requieren Administrador o EncargadoAlmacen. DELETE desactiva; no borra fisicamente.
- /api/v1/suppliers: GET, GET /{id}, POST, PUT /{id} y DELETE /{id} requieren Administrador o EncargadoAlmacen. DELETE desactiva; no borra fisicamente.
- GET /api/v1/inventory/stock, /low-stock y /movements: existencia paginada, alertas con stock <= minimo e historial filtrable por producto/tipo/fecha.
- POST /api/v1/inventory/adjustments: acepta productId, quantityDelta distinto de cero y reason obligatorio. El ajuste que produzca stock negativo devuelve 409. Un ajuste valido persiste producto, movimiento y auditoria en una transaccion.

Toda escritura requiere el token de GET /api/v1/auth/csrf en X-CSRF-TOKEN. dotnet test src/backend/tests/GestionEmpresarial.UnitTests/GestionEmpresarial.UnitTests.csproj ejecuta las pruebas de autenticacion y catalogos con WebApplicationFactory y EF Core InMemory; estas pruebas no sustituyen una validacion contra SQL Server.
