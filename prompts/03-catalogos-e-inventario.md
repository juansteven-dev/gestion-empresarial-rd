# Prompt 03 — Catálogos e inventario

Implementa RF-04, RF-05, RF-06, RF-07, RF-12, RF-13 y RF-14.

Construye entidades, mappings, migración, servicios, endpoints, DTOs, validaciones y UI para categorías, productos, suplidores, consulta de existencias, movimientos, ajustes y stock bajo.

Reglas:
- SKU único; precios/stock/mínimo no negativos;
- paginación page/pageSize con máximo 100;
- productos y suplidores se desactivan con IsActive en lugar de eliminar si tienen historial;
- ajustes requieren motivo y permiso Admin/EncargadoAlmacen;
- cada ajuste genera StockMovement y AuditLog en una transacción;
- no exponer entidades EF directamente.

Incluye pruebas de SKU duplicado, categoría inexistente, ajuste sin motivo, ajuste que daría stock negativo, alerta de mínimo y paginación. Genera UI con búsqueda, filtros, formularios y estados loading/empty/error.
