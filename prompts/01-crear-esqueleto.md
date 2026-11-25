# Prompt 01 — Estructura inicial

Lee `AGENTS.md` y `docs/00-inicio-vscode.md`. Inspecciona el repositorio antes de modificar.

Crea o completa la solución backend siguiendo `docs/04-arquitectura-y-diagramas.md`:
- proyectos Domain, Application, Infrastructure y Api;
- referencias de proyectos en dirección correcta;
- DI en Program.cs;
- OpenAPI/ProblemDetails;
- endpoint de health check;
- ApplicationDbContext con Identity y entidades base si ya están definidas;
- configuración desde User Secrets/environment variables;
- CORS o proxy de desarrollo explicado.

Prepara frontend React/TypeScript con layout base, React Router, cliente HTTP centralizado, manejo de errores y rutas placeholder para login, dashboard, productos, compras y ventas. No inventes endpoints; usa `docs/04-arquitectura-y-diagramas.md`.

No implementes todavía toda la lógica comercial. Asegura `dotnet build` y `npm run build`. Resume los comandos ejecutados y resultados.
