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

Explicar cómo buscar producto, seleccionar cantidad, método de pago, confirmar, visualizar comprobante interno y qué sucede si no hay stock.

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
