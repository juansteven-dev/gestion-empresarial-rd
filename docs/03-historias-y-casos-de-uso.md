# 3. Historias de usuario y casos de uso

## 3.1 Actores

- **Administrador:** propietario o encargado general; gestiona usuarios y puede acceder a todos los módulos.
- **Empleado:** registra ventas y consulta productos.
- **Encargado de almacén:** administra catálogo, suplidores, compras y ajustes de inventario.

## 3.2 Historias de usuario

| ID | Historia | Criterios de aceptación |
|---|---|---|
| HU-01 | Como usuario, quiero iniciar sesión para acceder a las funciones autorizadas. | Credenciales válidas, sesión creada, menú acorde al rol. |
| HU-02 | Como administrador, quiero crear usuarios y asignar roles para controlar el acceso. | Solo Admin puede hacerlo; rol asignado aparece en perfil. |
| HU-03 | Como encargado, quiero registrar productos para mantener el catálogo actualizado. | SKU único; categoría válida; precios/stock no negativos. |
| HU-04 | Como encargado, quiero consultar productos con poco stock para planificar reposición. | Se muestran productos con `CurrentStock <= MinimumStock`. |
| HU-05 | Como encargado, quiero registrar suplidores para relacionarlos con compras. | Datos validados; proveedor puede desactivarse sin borrar historial. |
| HU-06 | Como encargado, quiero crear una compra para documentar mercancía adquirida. | Borrador guarda suplidor, líneas, cantidades y costos. |
| HU-07 | Como encargado, quiero recibir una compra para aumentar existencias. | Se procesa una sola vez; stock y movimientos se actualizan juntos. |
| HU-08 | Como empleado, quiero registrar una venta para cobrar productos y actualizar inventario. | Stock suficiente; total calculado en servidor; movimiento creado. |
| HU-09 | Como empleado, quiero ver un comprobante interno para confirmar la operación. | Muestra número, fecha, líneas, subtotal, descuento y total. |
| HU-10 | Como administrador, quiero revisar reportes para conocer el desempeño del negocio. | Filtros por período y totales consistentes con ventas guardadas. |
| HU-11 | Como administrador, quiero consultar auditoría para saber quién ejecutó cambios importantes. | Se muestra usuario, acción, entidad y fecha. |
| HU-12 | Como encargado, quiero ajustar inventario con un motivo para corregir diferencias justificadas. | Motivo obligatorio; cantidad resultante >= 0; movimiento y auditoría. |

## 3.3 Casos de uso

### CU-01 Registrar venta

- Actor principal: Empleado.
- Precondiciones: sesión válida; rol Empleado o Administrador; productos activos y disponibles.
- Flujo principal:
  1. El empleado agrega uno o más productos.
  2. El sistema consulta precios actuales y calcula una vista previa.
  3. El empleado selecciona método de pago y confirma.
  4. El backend vuelve a validar productos, precios, permisos y existencias.
  5. En una transacción, guarda la venta y sus líneas, descuenta stock, registra movimientos y auditoría.
  6. Devuelve número y resumen de venta.
- Alternos: stock insuficiente -> HTTP 409; producto inactivo -> HTTP 400/409; error DB -> rollback completo.
- Postcondición: venta confirmada y existencias consistentes.

### CU-02 Recibir compra

- Actor principal: EncargadoAlmacen.
- Precondiciones: sesión válida; compra en estado Draft.
- Flujo principal: abrir compra, revisar líneas, confirmar recepción; servidor verifica estado; en transacción cambia estado a Received, incrementa stock, crea movimientos y auditoría.
- Alternos: compra ya recibida -> HTTP 409; producto inactivo o detalle inválido -> no se procesa; error -> rollback.
- Postcondición: compra recibida una vez y stock incrementado.

### CU-03 Crear producto

- Actor: EncargadoAlmacen o Administrador.
- Precondiciones: autenticación y rol.
- Flujo: completar SKU, nombre, categoría, costo, precio y mínimo; backend valida unicidad y valores; guarda producto.
- Alternos: SKU duplicado -> HTTP 409; categoría inexistente -> HTTP 400.

### CU-04 Administrar usuarios

- Actor: Administrador.
- Precondición: sesión Admin.
- Flujo: crea cuenta, asigna rol permitido y estado; Identity aplica políticas de contraseña.
- Alternos: rol no permitido o email repetido -> validación; acceso desde otros roles -> HTTP 403.

## 3.4 Reglas para estimación

Cada historia debe convertirse en tareas frontend, backend, persistencia y pruebas. Evitar historias “hacer módulo completo”; dividirlas por comportamiento verificable.

## 3.5 Aceptacion de catalogos e inventario (Fase B)

- HU-03 Productos: crear o editar requiere categoria activa, SKU unico, precios no negativos y existencia minima no negativa. La existencia actual no se edita en el formulario de producto.
- HU-04 Alertas: el usuario de almacen consulta productos activos cuando CurrentStock <= MinimumStock.
- HU-05 Suplidores: Administrador o EncargadoAlmacen puede buscar, filtrar por estado y desactivar un suplidor conservando sus datos.
- HU-12 Ajustes: Administrador o EncargadoAlmacen indica producto, variacion y motivo. Una variacion que deje stock negativo se rechaza; una aceptada crea movimiento y auditoria.
- Los listados de categorias, productos, suplidores, existencias y movimientos usan paginacion del servidor. Las escrituras requieren cookie autenticada y token antifalsificacion.

## 3.6 Ciclo de compras implementado (Fase C)

- HU-06: el encargado crea un borrador con suplidor y lineas. El servidor valida suplidor/productos activos, cantidad y costo; genera el numero y total.
- HU-07: el borrador conserva stock. Al recibir, el servidor reclama condicionalmente el estado Draft, actualiza existencias, agrega movimientos y auditoria y confirma todo en una transaccion.
- La recepcion repetida o el cambio concurrente de compra/stock devuelve conflicto sin duplicar inventario.
