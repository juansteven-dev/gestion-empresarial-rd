# 1. Identificación del problema y alcance

## 1.1 Contexto

El proyecto plantea un pequeño comercio minorista dominicano (colmado o tienda de conveniencia) que necesita administrar productos, existencias, suplidores, compras y ventas desde un único sistema. Se trata de un escenario empresarial simulado para fines académicos; no se afirma que una empresa real haya contratado la solución.

## 1.2 Problema

La gestión manual o fragmentada del inventario y las operaciones comerciales puede provocar diferencias entre existencias reales y registradas, ventas de productos agotados, compras duplicadas, pérdida de información y dificultad para conocer el desempeño del negocio.

## 1.3 Causas

- Registro en cuadernos o archivos separados.
- Ausencia de actualización automática del inventario al vender o recibir productos.
- Falta de permisos diferenciados para empleados y administradores.
- Poca trazabilidad de quién realizó cambios.
- Reportes elaborados manualmente y con retraso.

## 1.4 Consecuencias

- Pérdidas por desabastecimiento o diferencias de inventario.
- Tiempo administrativo dedicado a reconciliar datos.
- Dificultad para identificar productos de alta rotación.
- Menor control de compras, ventas y empleados.

## 1.5 Usuarios afectados

Propietario/administrador, cajero o empleado, encargado de almacén y personal que consulta reportes.

## 1.6 Solución propuesta

Aplicación web cliente-servidor. React presenta la interfaz; ASP.NET Core Web API valida permisos y reglas; SQL Server conserva los datos. Las operaciones críticas se ejecutan en el servidor dentro de transacciones.

## 1.7 Objetivo general

Diseñar e implementar un prototipo web cliente-servidor para gestionar inventario, compras, ventas y usuarios de un pequeño comercio, aplicando arquitectura por capas, seguridad, modelado, pruebas y documentación.

## 1.8 Objetivos específicos

1. Levantar requisitos funcionales y no funcionales.
2. Modelar actores, casos de uso, clases, secuencias, componentes y arquitectura.
3. Diseñar una base de datos relacional normalizada.
4. Implementar autenticación y autorización por roles.
5. Implementar operaciones esenciales de catálogo, compras, ventas y stock.
6. Ejecutar pruebas unitarias, integración y aceptación.
7. Evaluar seguridad, usabilidad, rendimiento y mantenibilidad.
8. Entregar manuales, repositorio y evidencias.

## 1.9 Alcance incluido

- Login/logout y perfil de usuario.
- Gestión de usuarios por administrador y roles fijos.
- Categorías y productos.
- Suplidores.
- Compras en borrador y recepción de compra.
- Ventas y comprobante interno.
- Ajustes manuales de inventario con motivo y auditoría.
- Movimientos e historial por producto.
- Alertas de stock bajo.
- Dashboard y reportes básicos.

## 1.10 Fuera de alcance de la primera versión

- Integración real con DGII/e-CF o facturación fiscal certificada.
- Procesamiento real de tarjetas o conexión bancaria.
- Aplicación móvil nativa.
- Multiempresa/multisucursal.
- Contabilidad formal, nómina o impuestos automáticos.
- Microservicios y despliegue cloud de alta disponibilidad.
- Recomendaciones con IA.

## 1.11 Supuestos y restricciones

- Moneda de trabajo: peso dominicano (DOP).
- Un solo comercio y una sucursal en la primera versión.
- Los usuarios son creados por un administrador; no hay registro público.
- Las operaciones de compra/venta utilizan productos ya registrados.
- No se almacenan datos de tarjetas ni credenciales bancarias.
- El prototipo se evalúa en un entorno local de demostración.

## 1.12 Criterios de aceptación del proyecto

- Los tres roles pueden iniciar sesión y reciben permisos diferentes.
- El administrador puede mantener catálogos y usuarios.
- La recepción de una compra incrementa stock una única vez.
- La venta disminuye stock y rechaza una cantidad superior a la disponible.
- Todo movimiento de stock queda registrado con usuario, fecha, tipo y referencia.
- Los reportes reflejan datos persistidos en la base de datos.
- Las pruebas críticas tienen evidencia real.
