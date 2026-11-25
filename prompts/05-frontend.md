# Prompt 05 — Aplicación frontend

Implementa el frontend siguiendo el diseño funcional y la API existente. No crear datos mock como si fueran datos reales en la entrega final; si se usan mocks durante desarrollo, mantenerlos aislados y etiquetados.

Diseño requerido:
- Login limpio.
- Layout con sidebar y barra superior con usuario/rol.
- Dashboard con tarjetas de métricas y alertas de stock.
- Productos: tabla paginada, búsqueda, filtros, alta/edición.
- Suplidores: listado y formularios.
- Compras: crear borrador, detalle, recibir.
- Ventas: punto de venta simple y comprobante interno.
- Inventario: movimientos, ajustes y stock bajo.
- Reportes: filtros por fecha y gráficos con datos API.

Requisitos de calidad:
- responsive 360px+;
- componentes reutilizables;
- React Router y rutas privadas por rol;
- formularios con validación;
- centralizar cliente API y errores;
- loading, empty, success y error states;
- accesibilidad básica: labels, foco y contraste;
- no poner reglas de negocio críticas solo en cliente.

Verifica `npm run lint` si está configurado y `npm run build`. Lista pantallas aún no conectadas.
