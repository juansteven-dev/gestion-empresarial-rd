# Prompt 04 — Compras y ventas transaccionales

Implementa RF-08, RF-09, RF-10 y RF-11 y los casos CU-01/CU-02.

Compras:
- crear compra en Draft con suplidor y líneas;
- no aumentar stock al crear borrador;
- al recibir, incrementar stock y crear movimientos en una sola transacción;
- impedir una segunda recepción con 409.

Ventas:
- aceptar productId, quantity y paymentMethod; no confiar en total o precio enviado por cliente;
- backend lee precio actual y calcula subtotal/descuento/total;
- validar productos activos y stock suficiente;
- crear Sale/SaleItems, descontar stock, crear StockMovements y AuditLog atómicamente;
- manejar concurrencia para que dos ventas simultáneas no dejen stock negativo;
- devolver número y líneas de comprobante interno.

UI: flujo de caja simple con búsqueda, carrito, método de pago, confirmación y resultado; pantalla de compras con borrador y botón recibir protegido.

Añade pruebas para compra borrador, recepción, recepción doble, venta válida, stock insuficiente, payload manipulado y concurrencia/rollback. No simules éxito cuando la API falle.
