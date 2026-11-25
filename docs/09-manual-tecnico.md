# Manual técnico (plantilla para completar al finalizar)

> Completar con las versiones, rutas y configuraciones reales del repositorio. No copiar valores secretos.

## 1. Descripción

Gestión Empresarial RD es una aplicación cliente-servidor. El cliente React/TypeScript consume una API ASP.NET Core; la API aplica reglas de negocio y persiste en SQL Server con EF Core.

## 2. Requisitos de instalación

- Windows 11/macOS/Linux compatible con SDK .NET 10.
- .NET SDK 10.
- Node.js LTS acordado por el equipo.
- SQL Server 2022+ o Docker Desktop.
- Git y Visual Studio Code.

## 3. Estructura

Documentar la estructura real del backend y frontend, especialmente los módulos, capas, configuración y tests.

## 4. Configuración

Registrar nombres de variables, no sus valores:

| Variable/clave | Propósito | Secreto |
|---|---|---|
| `ConnectionStrings:DefaultConnection` | Conexión SQL Server | Sí |
| `ASPNETCORE_ENVIRONMENT` | Entorno API | No |
| `MSSQL_SA_PASSWORD` | Password SQL en Docker | Sí |
| `VITE_API_BASE_URL` | URL base si no se usa proxy | No |

## 5. Instalación y ejecución

Seguir `docs/00-inicio-vscode.md` y adaptar los comandos a la estructura final. Documentar migraciones, seed de roles/usuario administrador y comandos exactos comprobados.

## 6. Arquitectura y API

Incluir el diagrama de arquitectura, OpenAPI/Swagger disponible, lista de endpoints, DTOs principales, roles y códigos HTTP.

## 7. Base de datos

Incluir diagrama ER, descripción de tablas, migraciones y cómo crear/restaurar backup.

## 8. Pruebas

Comandos realmente ejecutados para `dotnet test`, `dotnet build`, `npm run build` y la colección Postman.

## 9. Despliegue y mantenimiento

Documentar configuración de producción, HTTPS, secretos, backup, logs, actualización de dependencias y rollback de versión. Si no se despliega en producción, declararlo expresamente.

## 10. API de catalogos e inventario (implementada)

Los controladores estan en src/backend/src/GestionEmpresarial.Api/Controllers; sus contratos estan en Application y CatalogService implementa casos de uso y consultas en Infrastructure. Los controladores no acceden directamente a EF Core.

| Ruta | Acceso | Funcion |
|---|---|---|
| GET /api/v1/products y GET /{id} | Cualquier usuario autenticado | Busqueda por nombre/SKU, filtro por categoria/estado/stock bajo y paginacion. |
| POST, PUT /{id}, DELETE /{id} en /api/v1/products | Administrador, EncargadoAlmacen | Alta/edicion y desactivacion logica. El stock no se escribe desde el formulario de producto. |
| GET de /api/v1/categories | Cualquier usuario autenticado | Consulta de nombres para filtrar productos. |
| Escrituras en /api/v1/categories y CRUD en /api/v1/suppliers | Administrador, EncargadoAlmacen | CRUD y desactivacion logica. |
| GET /api/v1/inventory/stock, /low-stock, /movements | Administrador, EncargadoAlmacen | Existencias, alertas y movimientos paginados. |
| POST /api/v1/inventory/adjustments | Administrador, EncargadoAlmacen | Ajuste con motivo, validacion del balance, movimiento y auditoria transaccionales. |

Los listados aceptan page y pageSize (1 a 100). Los errores de validacion responden 400 y los conflictos de SKU, categoria o stock responden 409 con Problem Details. Las escrituras requieren antiforgery. La migracion inicial ya contiene el esquema de catalogos e inventario; esta fase no cambio el esquema y no creo otra migracion.

Las pruebas de integracion actuales reemplazan SQL Server por EF Core InMemory. Para validar indices unicos, RowVersion y transacciones relacionales en el entorno local, ejecutar ademas una prueba funcional contra una base SQL Server de desarrollo.

## 11. Endpoints de compras (Fase C)

El controlador PurchasesController aplica autorizacion en servidor a Administrador y EncargadoAlmacen. GET /api/v1/purchases lista compras paginadas y admite search, supplierId, status, page y pageSize. GET /{id} devuelve el detalle. POST /api/v1/purchases crea un borrador; POST /{id}/receive confirma la recepcion. Las escrituras pasan por el filtro antifalsificacion global.

La recepcion abre transaccion, reclama la fila con una actualizacion condicionada a Status Draft, incrementa cada producto, crea StockMovement por linea, marca Received y agrega AuditLog antes de confirmar. La condicion de estado evita que dos solicitudes concurrentes reciban el mismo documento; RowVersion de producto detecta conflictos de stock. Cualquier excepcion antes de confirmar revierte la transaccion en SQL Server. No hubo cambios de esquema porque las tablas y restricciones ya existen en la migracion inicial.

## 12. Endpoints de ventas (Fase D)

`SalesController` aplica autorizacion de servidor a Administrador y Empleado; GET y POST tambien pasan por el filtro antifalsificacion global en las escrituras. `GET /api/v1/sales` admite `search` por numero, `fromDate`, `toDate`, `page` y `pageSize`. El rol Administrador puede filtrar por `createdByUserId`; Empleado siempre queda limitado al usuario autenticado. `GET /api/v1/sales/{id}` aplica el mismo alcance. `POST /api/v1/sales` recibe solo `paymentMethod` y `items[{productId, quantity}]`.

`SaleService` valida lineas y productos activos, obtiene `SalePrice` desde la base, calcula subtotal/total y descuenta stock. Guarda el encabezado y las lineas para obtener el ID; luego agrega un movimiento `Sale` por producto y `AuditLog`, y confirma todo en la misma transaccion SQL Server. `Products.RowVersion` evita que dos solicitudes concurrentes vendan las mismas unidades; el conflicto responde 409. No se agregaron tablas ni columnas porque la migracion inicial ya define Sales, SaleItems y StockMovements.

`SalesEndpointTests` ejecuta la concurrencia con EF Core InMemory y un interceptor de pruebas que coordina las dos lecturas y simula el conflicto de RowVersion. Este proveedor no demuestra el rollback relacional de SQL Server; una validacion completa de transacciones debe ejecutarse contra una base de pruebas SQL Server.

## 13. Dashboard, reportes, usuarios y auditoría (Fase E)

`GET /api/v1/dashboard` recibe `fromDate` y `toDate` opcionales y devuelve agregados desde Sales, SaleItems y Products. El alcance cambia por rol: Administrador ve ventas globales e inventario, Empleado sus propias ventas, y EncargadoAlmacen inventario. `GET /api/v1/reports` requiere Administrador y devuelve ventas por día, productos más vendidos y movimientos agregados.

`GET /api/v1/users` requiere Administrador y admite `search`, `page` y `pageSize`; `POST /api/v1/users` crea cuentas con Identity. La vista presenta los roles y si Identity considera la cuenta bloqueada. `GET /api/v1/audit` requiere Administrador y admite `search`, `userId`, `fromDate`, `toDate`, `page` y `pageSize`. Estas consultas usan entidades existentes y no requieren migración.

Las integraciones frontend están en `src/frontend/src/api`; Dashboard, Reportes, Usuarios y Auditoría consumen esos endpoints. El bundle de Reportes carga de forma diferida para mantener los gráficos fuera del paquete inicial.
