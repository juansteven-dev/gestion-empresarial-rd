# 11. Plan de calidad del software

## 14.1 Objetivo

Evaluar la calidad del prototipo con evidencias observables, no solo con apreciaciones. Los umbrales son objetivos académicos de aceptación y deben ajustarse si el equipo documenta una limitación del entorno.

## 14.2 Atributos y métricas

| Atributo | Qué se evalúa | Métrica/criterio de aceptación | Evidencia |
|---|---|---|---|
| Funcionalidad | Correspondencia entre requisitos y funciones | 100% de RF Alta implementados o declarados como limitación; cada RF Alta tiene prueba | Matriz de trazabilidad y Postman |
| Usabilidad | Facilidad para completar tareas comunes | Usuario de prueba completa login, venta y recepción de compra sin asistencia; formularios con etiquetas y errores claros | Observación y capturas |
| Rendimiento | Tiempo de respuesta | 95% de consultas comunes < 2 segundos en dataset y carga definidos en RNF-02 | Reporte de k6/JMeter o medición documentada |
| Seguridad | Protección de rutas, datos y operaciones | Todas las pruebas de roles pasan; no hay secretos en repo; venta no acepta total manipulado | Pruebas 401/403/CSRF y revisión |
| Mantenibilidad | Separación de responsabilidades y legibilidad | API en capas; frontend por features; no duplicar reglas de negocio críticas | Revisión de código/estructura |
| Escalabilidad | Capacidad de crecer sin reescribir módulos | Paginación, índices, API versionada y módulos desacoplados; no implica demostrar escala masiva | Diseño y prueba de paginación |
| Confiabilidad | Integridad y recuperación | 0 stock negativo; transacciones completas; compra no se recibe dos veces; backup restaurado | QA-07 a QA-10/QA-16 y acta de restore |
| Compatibilidad | Funcionamiento en navegadores definidos | Flujos principales probados en Chrome y Edge disponibles | Lista de navegador/versión y capturas |

## 14.3 Revisión de calidad por módulo

- Auth: login, logout, roles, expiración y respuestas de error.
- Catalog: restricciones SKU/precios/categoría, paginación y desactivación.
- Purchases: doble recepción, datos históricos y atomicidad.
- Sales: stock insuficiente, precios recalculados, total y concurrencia.
- Inventory: ajustes con motivo, movimientos y stock bajo.
- Reports: filtros de fecha, agregaciones y consistencia con tablas transaccionales.
- Frontend: responsive, loading/empty/error, navegación por rol y validaciones.

## 14.4 Criterios de salida

- Build del frontend y backend sin errores.
- Todas las pruebas de prioridad Alta ejecutadas y con resultado registrado.
- Ningún defecto crítico de seguridad o integridad abierto.
- Defectos medios/bajos documentados con impacto.
- Documentación coincide con la versión entregada.
- Se dispone de backup y evidencia de restauración.

## 14.5 Limitaciones a declarar

El sistema es un prototipo académico para un comercio simulado. No se debe afirmar que está certificado para facturación fiscal, que soporta alta disponibilidad productiva o que escala a miles de comercios. Las capacidades no probadas deben declararse como trabajo futuro.
