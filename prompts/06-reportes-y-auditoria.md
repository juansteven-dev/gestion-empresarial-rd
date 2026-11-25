# Prompt 06 — Reportes, dashboard y auditoría

Implementa RF-15, RF-16 y RF-17.

Dashboard mínimo:
- ventas del día y período seleccionado;
- número de ventas;
- productos con stock bajo;
- productos más vendidos por unidades.

Reportes:
- ventas por rango de fecha, filtros y paginación;
- top productos vendidos;
- movimientos de inventario por producto y fecha.

Auditoría:
- lista paginada para Administrador;
- actor, acción, entidad, referencia, fecha y detalles permitidos;
- nunca incluir contraseña, cookies, tokens ni información sensible.

Los reportes deben consultar SQL Server y coincidir con los registros transaccionales. Agrega pruebas de rango de fechas, permisos y consistencia de totales.
