# Prompt 07 — Revisión integral antes de entrega

Actúa como revisor externo. No hagas refactor masivo de inmediato. Inspecciona el repositorio contra:
- `docs/02-requisitos.md` (RF/RNF);
- `docs/04-arquitectura-y-diagramas.md`;
- `docs/05-modelo-de-datos.md`;
- `docs/06-seguridad.md`;
- `docs/07-plan-de-pruebas.md`.

Entrega una matriz requisito -> archivo/endpoint/pantalla -> prueba -> estado real (Implementado, Parcial, No implementado, No verificado). Identifica:
1. fallos de seguridad/autorización;
2. riesgos de stock/doble recepción/concurrencia;
3. errores de integridad/validación;
4. endpoints sin UI o UI sin API;
5. falta de logs/auditoría;
6. pruebas ausentes;
7. discrepancias entre documentación y código.

Después corrige solo defectos priorizados con cambios pequeños y pruebas. Ejecuta `dotnet build`, `dotnet test`, `npm run build` y lint si existe. Reporta los resultados literales de los comandos. No marques nada como completo si no se verificó.
