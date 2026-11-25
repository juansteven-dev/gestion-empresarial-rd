# 12. Gestión del proyecto

## 12.1 Metodología

Scrum ligero con iteraciones de una semana. Reunión breve para acordar objetivos y bloqueos; tablero Kanban con columnas Pendiente, En progreso, Revisión, Pruebas y Terminado. Pull Requests para integrar trabajo; la persona autora no debe aprobar su propio cambio crítico.

## 12.2 División entre cinco integrantes

| Integrante | Área principal | Entregables propios | Responsabilidades compartidas |
|---|---|---|---|
| 1. Análisis | Problema, requisitos, historias y aceptación | docs 01-03; backlog; validación de alcance | Coordina cronograma y edición final del informe |
| 2. Arquitectura | UML, capas, API y convenciones | docs 04; diagramas; estructura backend | Revisión técnica de PRs e integración |
| 3. Datos | Modelo ER, entidades y persistencia | docs 05; DbContext/migrations; SQL | Ayuda a validar compras/ventas y backup |
| 4. Frontend | UX, prototipos y React | prototipos; páginas; componentes y API client | Manual de usuario y capturas |
| 5. Seguridad/QA | Identidad, permisos, pruebas y calidad | docs 06-07; casos de prueba; evidencias | Checklist de seguridad y demo |

La implementación es de todos: integrante 1 valida reglas y aceptación; 2 API/capas; 3 persistencia; 4 interfaz; 5 auth/QA. Cada módulo tiene un responsable principal, pero revisión cruzada.

## 12.3 Cronograma sugerido de 6 semanas

| Semana | Objetivo | Entregables |
|---|---|---|
| 1 | Selección del escenario, requisitos, historias, arquitectura y prototipos | Alcance aprobado, requisitos, UML inicial, wireframes |
| 2 | Bootstrap repo, BD, Identity y navegación básica | Build cliente/servidor, migración inicial, login y roles |
| 3 | Catálogos, productos, categorías, suplidores e inventario | CRUD + validaciones + pruebas |
| 4 | Compras y ventas transaccionales | Flujo de recepción, venta, movimientos y auditoría |
| 5 | Dashboard, reportes, calidad, seguridad y manuales | Reportes, pruebas, evidencia, documentación |
| 6 | Correcciones, ensayo de demo y entrega | PDF 15-25 páginas, manuales, repo, PPT y prototipo |

Ajustar a la fecha real de entrega. No esperar hasta el final para integrar módulos.

## 12.4 Git y control de cambios

- `main`: rama estable para entregas.
- `develop`: integración del equipo (opcional si el grupo puede mantenerla).
- Ramas: `feature/products-crud`, `feature/sales-create`, `fix/stock-validation`, `docs/uml-model`.
- Commits: `feat: add product CRUD`, `fix: prevent negative stock`, `test: cover duplicate purchase receipt`, `docs: update user manual`.
- PR debe incluir: objetivo, requisitos cubiertos, capturas cuando aplique, pruebas ejecutadas y riesgos.
- No subir `node_modules`, `bin`, `obj`, `.env`, secretos ni bases de datos con información sensible.

## 12.5 Gestión de incidencias

Cada issue debe incluir título, descripción, pasos para reproducir, resultado esperado/obtenido, prioridad, responsable y criterio de cierre. Prioridad: crítica (datos/seguridad), alta (flujo principal), media (función secundaria), baja (cosmético).

## 12.6 Herramientas

VS Code, GitHub, GitHub Projects/Issues, Figma o draw.io, Postman, SQL Server/SSMS, xUnit y herramienta de videollamada para reuniones.

## 12.7 Riesgos

| Riesgo | Mitigación |
|---|---|
| IA crea código no integrado | Prompts cortos; revisar diff; build por módulo |
| Un integrante concentra todo | PRs pequeños; dueños claros; revisión cruzada |
| Cambios rompen stock | Transacciones y pruebas de regresión |
| Alcance excesivo | Mantener exclusiones de docs 01 |
| Tiempo de integración insuficiente | Integrar desde semana 2 y congelar funciones antes de demo |
| Evidencias incompletas | Guardar capturas/resultados al ejecutar cada QA |
