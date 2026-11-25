# Inicio del proyecto en Visual Studio Code

Esta guía prepara el repositorio para comenzar el desarrollo asistido por IA. Ejecuta los comandos desde la carpeta raíz `GestionEmpresarialRD`.

## A. Verificar herramientas

```bash
 dotnet --version
 node --version
 npm --version
 git --version
```

Usar .NET SDK 10 LTS y Node.js 26 LTS. Aunque existen versiones más recientes, se fija .NET 10 para priorizar la línea LTS y reducir sorpresas de compatibilidad en el proyecto académico.

## B. Generar la solución .NET

En PowerShell o terminal integrada:

```powershell
cd src/backend

dotnet new sln -n GestionEmpresarial

dotnet new classlib -n GestionEmpresarial.Domain -o src/GestionEmpresarial.Domain -f net10.0
dotnet new classlib -n GestionEmpresarial.Application -o src/GestionEmpresarial.Application -f net10.0
dotnet new classlib -n GestionEmpresarial.Infrastructure -o src/GestionEmpresarial.Infrastructure -f net10.0
dotnet new webapi -n GestionEmpresarial.Api -o src/GestionEmpresarial.Api -f net10.0 --use-controllers
dotnet new xunit -n GestionEmpresarial.UnitTests -o tests/GestionEmpresarial.UnitTests -f net10.0

dotnet sln GestionEmpresarial.slnx add src/GestionEmpresarial.Domain/GestionEmpresarial.Domain.csproj
dotnet sln GestionEmpresarial.slnx add src/GestionEmpresarial.Application/GestionEmpresarial.Application.csproj
dotnet sln GestionEmpresarial.slnx add src/GestionEmpresarial.Infrastructure/GestionEmpresarial.Infrastructure.csproj
dotnet sln GestionEmpresarial.slnx add src/GestionEmpresarial.Api/GestionEmpresarial.Api.csproj
dotnet sln GestionEmpresarial.slnx add tests/GestionEmpresarial.UnitTests/GestionEmpresarial.UnitTests.csproj

dotnet add src/GestionEmpresarial.Application reference src/GestionEmpresarial.Domain
dotnet add src/GestionEmpresarial.Infrastructure reference src/GestionEmpresarial.Domain src/GestionEmpresarial.Application
dotnet add src/GestionEmpresarial.Api reference src/GestionEmpresarial.Application src/GestionEmpresarial.Infrastructure
dotnet add tests/GestionEmpresarial.UnitTests reference src/GestionEmpresarial.Domain src/GestionEmpresarial.Application
```

Si `dotnet new webapi` no reconoce `--use-controllers`, crear el template Web API y seleccionar controladores en la opción disponible del SDK; después mantener el diseño definido en los documentos.

Añadir paquetes EF Core/Identity compatibles con EF Core 10 (mantener la misma versión para todos los paquetes Microsoft.EntityFrameworkCore):

```powershell
dotnet add src/GestionEmpresarial.Infrastructure package Microsoft.EntityFrameworkCore.SqlServer --version 10.0.15
dotnet add src/GestionEmpresarial.Infrastructure package Microsoft.EntityFrameworkCore.Design --version 10.0.15
dotnet add src/GestionEmpresarial.Infrastructure package Microsoft.AspNetCore.Identity.EntityFrameworkCore --version 10.0.15
dotnet add src/GestionEmpresarial.Api package Microsoft.AspNetCore.OpenApi --version 10.0.15
```

Luego:

```powershell
dotnet restore
dotnet build
```

Si al momento de instalar existe un parche 10.0.x posterior, actualizar todos los paquetes Microsoft EF/Identity al mismo parche estable compatible. No mezclar versiones principales de EF Core.

## C. Generar el cliente React

Desde la raíz del repositorio:

```bash
npm create vite@latest src/frontend -- --template react-ts
cd src/frontend
npm install
npm install react-router-dom @tanstack/react-query axios react-hook-form zod @hookform/resolvers lucide-react recharts
npm install -D eslint prettier
```

Vite es el servidor de desarrollo del cliente. Configurar un proxy `/api` hacia `https://localhost:<puerto-api>` o `http://localhost:<puerto-api>` según el perfil de la API. En producción, preferir que frontend y API compartan origen mediante hosting/reverse proxy.

## D. Preparar SQL Server

### Opción 1: Docker

Crear `.env` en la raíz del repositorio (no subirlo a Git):

```env
MSSQL_SA_PASSWORD=UnaClaveLarga_Y_Unica_123!
```

Arrancar:

```bash
docker compose up -d
```

### Opción 2: SQL Server instalado

Crear la base `GestionEmpresarialRD` desde SSMS y usar una cuenta de desarrollo con permisos mínimos suficientes. Evitar `sa` para la conexión habitual de la API.

## E. Configurar conexión y secretos

Usar .NET User Secrets desde `src/backend/src/GestionEmpresarial.Api`:

```bash
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost,1433;Database=GestionEmpresarialRD;User Id=sa;Password=TU_CLAVE;TrustServerCertificate=True;Encrypt=True"
```

En entorno local con SQL Server instalado, reemplazar el servidor y el método de autenticación. Nunca guardar esta cadena en `appsettings.json` versionado.

## F. Migraciones y ejecución

Cuando el asistente haya creado `ApplicationDbContext` en Infrastructure y la configuración DI:

```bash
# Desde src/backend
 dotnet tool install --global dotnet-ef --version 10.0.15
 dotnet ef migrations add InitialCreate --project src/GestionEmpresarial.Infrastructure --startup-project src/GestionEmpresarial.Api
 dotnet ef database update --project src/GestionEmpresarial.Infrastructure --startup-project src/GestionEmpresarial.Api
 dotnet run --project src/GestionEmpresarial.Api
```

En otra terminal:

```bash
cd src/frontend
npm run dev
```

## G. Flujo de trabajo con IA (vibecoding)

1. Abrir la carpeta raíz en VS Code, no solo `src/frontend`.
2. Comprobar que el agente leyó `AGENTS.md` y `.github/copilot-instructions.md`.
3. Pegar `prompts/00-prompt-maestro.md` una vez como contexto.
4. Trabajar un prompt por módulo, en el orden sugerido del README.
5. Revisar el diff antes de aceptar cambios.
6. Ejecutar build y tests después de cada módulo.
7. Hacer commits pequeños y descriptivos.

**No pedir “haz el sistema completo” en un solo prompt.** Eso suele producir código inconsistente, reglas de negocio duplicadas y pantallas desconectadas de la API.
