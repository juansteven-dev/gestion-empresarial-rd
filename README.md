# Gestión Empresarial RD

**Sistema web cliente-servidor para administrar inventario, compras, ventas y suplidores de pequeños comercios.**

Este repositorio es el punto de partida académico y técnico para construir un prototipo funcional mediante desarrollo asistido por IA (vibecoding) en Visual Studio Code. Incluye alcance, requisitos verificables, arquitectura, diagramas Mermaid, modelo de datos, seguridad, prototipos de interfaz, plan de calidad, pruebas, gestión del proyecto, plantillas de documentación y prompts para trabajar por etapas.

> **Importante:** este paquete es una base de planificación y estructura, no una aplicación terminada. Los proyectos ejecutables de .NET y React se generan siguiendo `docs/00-inicio-vscode.md`. No se deben presentar como implementadas funcionalidades que todavía no existan y no hayan sido probadas.

## 1. Stack tecnológico recomendado

| Capa | Tecnología | Uso |
|---|---|---|
| Cliente | React 19.3 + TypeScript | Interfaz web adaptable |
| Herramienta frontend | Vite 8 | Servidor de desarrollo y compilación |
| Runtime JS | Node.js 26 LTS | Ejecutar herramientas del frontend |
| Servidor | ASP.NET Core Web API sobre .NET 10 LTS | API REST, reglas y seguridad |
| ORM | Entity Framework Core 10 | Persistencia y migraciones |
| Base de datos | SQL Server 2022 o superior | Datos relacionales |
| Identidad | ASP.NET Core Identity | Usuarios, contraseñas y roles |
| Pruebas API | Postman | Pruebas funcionales y de integración |
| Pruebas .NET | xUnit | Pruebas unitarias |
| Control de versiones | Git + GitHub | Colaboración e historial |
| Diagramas | Mermaid / draw.io | UML y arquitectura |

La elección de .NET 10 prioriza la línea LTS. React y Vite se usan como cliente independiente; ASP.NET Core expone una API HTTP/JSON y no renderiza las páginas React.

## 2. Qué debe hacer el sistema

- Autenticar a los usuarios y aplicar los roles `Administrador`, `Empleado` y `EncargadoAlmacen`.
- Administrar categorías, productos, suplidores y sus datos.
- Registrar compras y recibirlas para aumentar existencias.
- Registrar ventas para reducir existencias de manera atómica.
- Mantener historial de movimientos de inventario.
- Alertar sobre productos con existencias iguales o inferiores al mínimo.
- Mostrar indicadores y reportes básicos.
- Mantener auditoría de operaciones importantes.

## 3. Estructura del repositorio

```text
GestionEmpresarialRD/
├── .github/
│   └── copilot-instructions.md
├── .vscode/
│   ├── extensions.json
│   └── tasks.json
├── database/
│   ├── README.md
│   └── scripts/
│       └── 01-create-database.sql
├── docs/
│   ├── 00-inicio-vscode.md
│   ├── 01-alcance-y-problema.md
│   ├── 02-requisitos.md
│   ├── 03-historias-y-casos-de-uso.md
│   ├── 04-arquitectura-y-diagramas.md
│   ├── 05-modelo-de-datos.md
│   ├── 06-seguridad.md
│   ├── 07-plan-de-pruebas.md
│   ├── 08-gestion-del-proyecto.md
│   ├── 09-manual-tecnico.md
│   ├── 10-manual-de-usuario.md
│   ├── 11-estructura-documento-final.md
│   └── 12-presentacion-final.md
├── prompts/
│   ├── 00-prompt-maestro.md
│   ├── 01-crear-esqueleto.md
│   ├── 02-autenticacion-y-roles.md
│   ├── 03-catalogos-e-inventario.md
│   ├── 04-compras-y-ventas.md
│   ├── 05-frontend.md
│   ├── 06-reportes-y-auditoria.md
│   └── 07-revision-integral.md
├── src/
│   ├── backend/       # Solución .NET; se genera con las instrucciones
│   └── frontend/      # Aplicación React + TypeScript; se genera con Vite
├── tests/
│   ├── api/           # Colección Postman y escenarios
│   └── evidence/      # Capturas y resultados reales de pruebas
├── AGENTS.md
├── .editorconfig
├── .gitignore
└── docker-compose.yml
```

## 4. Requisitos previos

Instalar:

1. Visual Studio Code.
2. .NET SDK 10.
3. Node.js 26 LTS y npm.
4. SQL Server 2022 Developer/Express o Docker Desktop.
5. SQL Server Management Studio (SSMS) o Azure Data Studio si está disponible en el entorno.
6. Git.

Extensiones sugeridas: C# Dev Kit, C#, ESLint, Prettier, SQL Server (mssql), GitLens y Markdown Mermaid.

## 5. Inicio rápido

Seguir exactamente `docs/00-inicio-vscode.md`. Resumen:

```bash
# Desde la carpeta raíz del repositorio
# 1) Generar la solución backend en src/backend.
# 2) Generar React en src/frontend.
# 3) Configurar cadena de conexión y variables de entorno.
# 4) Ejecutar migraciones de EF Core.
# 5) Iniciar API y cliente en terminales separadas.
```

No copiar secretos a Git. Usar User Secrets en .NET y variables de entorno para el contenedor SQL Server.

## 6. Reglas del proyecto

- Cliente y servidor son aplicaciones separadas. El cliente solo consume endpoints `/api/v1/...`.
- El servidor es la autoridad: validar y autorizar todas las operaciones en backend.
- Nunca permitir que el frontend cambie directamente el stock.
- Venta y recepción de compra deben actualizar existencias, movimientos y documento dentro de una transacción de base de datos.
- Usar `decimal` para dinero; nunca `float` ni `double` en cálculos monetarios.
- Guardar fechas en UTC y mostrar en zona local cuando corresponda.
- No almacenar contraseñas en texto plano ni tokens sensibles en `localStorage`.
- No usar datos personales o credenciales reales en las pruebas.
- No marcar pruebas como aprobadas sin ejecutarlas y guardar evidencia.
- Un cambio de alcance debe actualizar requisitos, historias, pruebas y documentación.

## 7. Definición de terminado (Definition of Done)

Una funcionalidad se considera terminada solo cuando:

1. Tiene requisito/historia asociado.
2. Tiene validación del lado del servidor.
3. Respeta autenticación y rol correspondiente.
4. Tiene estados de carga, éxito y error en UI cuando aplique.
5. Tiene al menos una prueba relevante.
6. No rompe `dotnet build`, pruebas .NET ni `npm run build`.
7. Está integrada mediante Pull Request revisado.
8. Su documentación está actualizada.

## 8. Documentos principales

Empieza por `docs/00-inicio-vscode.md`, después lee `AGENTS.md` y utiliza `prompts/00-prompt-maestro.md` como contexto para tu asistente de IA.
