# Manual de usuario (plantilla para completar con capturas reales)

## 1. Acceso

Describir la URL local utilizada, pantalla de login, mensajes de validación y cierre de sesión. Incluir captura real.

## 2. Roles

- Administrador: administra usuarios y tiene acceso global.
- Empleado: consulta productos y registra ventas.
- Encargado de almacén: mantiene productos, suplidores, compras e inventario.

## 3. Dashboard

Explicar tarjetas, filtros de período, alertas y significado de cada indicador.

## 4. Productos y categorías

Explicar búsqueda, alta, edición, desactivación, SKU, costo, precio, stock y stock mínimo.

## 5. Suplidores

Explicar cómo registrar, editar y desactivar un suplidor.

## 6. Compras

Explicar cómo crear borrador, agregar líneas, revisar total y recibir compra. Advertir que recibir cambia stock y no se puede repetir.

## 7. Ventas

Administrador y Empleado pueden buscar productos activos por nombre o SKU, agregarlos al carrito, indicar cantidades y elegir efectivo, tarjeta o transferencia. El sistema comprueba de nuevo el stock y el precio al confirmar; si las existencias cambiaron, muestra el conflicto para que se actualice el carrito. Al completar la operación se muestra el comprobante interno con número, líneas, subtotal, descuento, total y método de pago. Puede imprimirse desde el navegador; no es un comprobante fiscal.

## 8. Inventario

Explicar alertas de bajo stock, movimientos, ajustes con motivo e historial.

## 9. Reportes

Explicar los filtros, período, métricas y limitaciones.

## 10. Mensajes frecuentes

Documentar errores reales: credenciales incorrectas, permisos insuficientes, SKU duplicado, stock insuficiente o pérdida de conexión.

## 11. Catalogos e inventario disponibles

El menu de sesion muestra Productos para todos los roles autenticados. Categorias, Suplidores e Inventario aparecen para Administrador y EncargadoAlmacen; el backend vuelve a comprobar ese permiso. Empleado tambien puede consultar nombres de categorias desde el filtro de Productos.

En Productos, escribe parte del nombre o SKU, filtra por categoria o estado y usa la paginacion. Administrador y EncargadoAlmacen pueden crear y editar SKU, nombre, categoria activa, costo, precio, minimo y estado. El stock actual es de solo consulta en esta pantalla. Desactivar conserva el producto y su historial; los productos inactivos se consultan con el filtro de estado.

En Categorias, crea y edita nombre/descripcion. Si una categoria todavia tiene productos activos, primero desactiva esos productos; asi no queda un producto activo en una categoria inactiva. En Suplidores, se pueden mantener identificacion, telefono, correo, direccion y estado; desactivar conserva el registro.

En Inventario, consulta el conteo y la lista de productos cuyo stock esta en el minimo o por debajo. Para ajustar, selecciona el producto, indica una cantidad positiva para sumar o negativa para restar, y escribe el motivo. El sistema rechaza una operacion que deje stock negativo. Los movimientos muestran variacion, balance antes/despues, motivo y fecha; los ajustes aceptados tambien quedan auditados.

## 12. Compras disponibles

Administrador y EncargadoAlmacen pueden abrir Compras. Usa Nueva compra, selecciona un suplidor activo y agrega una o mas lineas con producto, cantidad y costo unitario. Guardar borrador genera el numero de compra y muestra el documento en la lista; el stock no cambia en ese momento.

Abre Detalle para revisar sus lineas. Cuando la mercancia haya llegado, usa Recibir. La operacion cambia el estado a Recibida, incrementa existencias y queda en el historial de movimientos y auditoria. El boton solo aparece para borradores; si una compra ya fue recibida, otra solicitud se rechaza.

## 13. Ventas disponibles

En Ventas, escribe al menos dos caracteres para buscar un producto activo y agrégalo al carrito. Define cantidad y método de pago, y confirma. El navegador envía solo identificador de producto, cantidad y método de pago; el servidor obtiene el precio válido, comprueba stock y calcula importes. Una venta concurrente que consuma las últimas unidades puede responder con un conflicto; revisa el carrito y vuelve a consultar el catálogo. El historial se filtra por número y fechas; Administrador también puede filtrar por ID de empleado. El detalle permite volver a abrir o imprimir el comprobante. Empleado ve sus propias ventas; Administrador puede consultar el historial global. EncargadoAlmacen no tiene acceso a este módulo. La fecha del comprobante se muestra según la zona horaria local del navegador y se almacena en UTC.
