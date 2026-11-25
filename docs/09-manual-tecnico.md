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
