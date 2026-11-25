# 2. Requisitos del sistema

Los requisitos usan identificadores estables. Cada requisito debe vincularse a una historia, endpoint, pantalla y prueba. Los umbrales de rendimiento son objetivos de aceptación para el entorno académico, no garantías para producción.

## 2.1 Requisitos funcionales

| ID | Requisito | Prioridad | Criterio verificable |
|---|---|---|---|
| RF-01 | Iniciar y cerrar sesión | Alta | Credenciales válidas crean sesión; inválidas devuelven error genérico. |
| RF-02 | Consultar perfil y rol | Alta | El usuario autenticado puede consultar su perfil, sin ver datos sensibles. |
| RF-03 | Administrar usuarios | Alta | Solo Administrador crea, edita, desactiva y asigna roles. |
| RF-04 | Administrar categorías | Media | Crear, listar, editar y desactivar categorías. |
| RF-05 | Administrar productos | Alta | Crear, consultar, editar y desactivar productos con SKU único. |
| RF-06 | Consultar inventario | Alta | Buscar y filtrar productos por nombre, SKU, categoría y stock. |
| RF-07 | Administrar suplidores | Media | Crear, consultar, editar y desactivar suplidores. |
| RF-08 | Crear compra | Alta | Crear borrador con suplidor, líneas, cantidades y costo unitario. |
| RF-09 | Recibir compra | Alta | Al recibir, el sistema incrementa stock y crea movimientos en una sola transacción. |
| RF-10 | Registrar venta | Alta | Calcular totales en servidor, comprobar stock y descontarlo de forma atómica. |
| RF-11 | Consultar ventas | Media | Listar ventas por rango de fechas, número y empleado. |
| RF-12 | Consultar movimientos | Alta | Filtrar historial por producto, tipo y fecha. |
| RF-13 | Ajustar stock | Media | Solo EncargadoAlmacen/Admin ajusta y debe indicar motivo. |
| RF-14 | Alertar stock bajo | Media | Mostrar productos activos con stock <= mínimo. |
| RF-15 | Dashboard | Media | Mostrar ventas del período, cantidad de ventas, alertas y productos destacados. |
| RF-16 | Reportes | Media | Consultar ventas por día, productos más vendidos y movimientos. |
| RF-17 | Auditoría | Alta | Registrar usuario, acción, entidad, fecha y referencia de operaciones sensibles. |
| RF-18 | Comprobante interno | Baja | Mostrar/permitir imprimir resumen de venta no fiscal. |

## 2.2 Requisitos no funcionales

| ID | Categoría | Requisito / métrica |
|---|---|---|
| RNF-01 | Arquitectura | Cliente React independiente que consume API REST versionada bajo `/api/v1`. |
| RNF-02 | Rendimiento | En el entorno de prueba acordado, 95% de solicitudes de consulta comunes deben responder en menos de 2 segundos con 50 usuarios virtuales y dataset de 5,000 productos. Registrar herramienta y configuración. |
| RNF-03 | Rendimiento | Listados paginados; tamaño predeterminado 20 y máximo 100 registros por página. |
| RNF-04 | Disponibilidad | En la demostración local, el servicio debe recuperarse tras reiniciar API/base de datos sin pérdida de datos ya confirmados. |
| RNF-05 | Seguridad | Toda operación protegida requiere identidad válida y rol autorizado comprobado en servidor. |
| RNF-06 | Seguridad | Contraseñas gestionadas por ASP.NET Core Identity; nunca texto plano. |
| RNF-07 | Seguridad | El sistema no expone connection strings, contraseñas, stack traces ni datos sensibles al cliente. |
| RNF-08 | Usabilidad | Interfaz adaptable a anchuras de 360 px o superiores; formularios con etiquetas y mensajes claros. |
| RNF-09 | Mantenibilidad | Separar Domain, Application, Infrastructure y API; frontend por módulos funcionales. |
| RNF-10 | Integridad | Las operaciones de venta y recepción de compra son transaccionales; el stock nunca puede ser negativo. |
| RNF-11 | Datos | Importes en DOP con precisión decimal de dos posiciones; fechas almacenadas en UTC. |
| RNF-12 | Compatibilidad | Probar en versiones actuales de Chrome y Edge disponibles en el laboratorio. |
| RNF-13 | Observabilidad | Errores técnicos quedan en logs del servidor con correlación, sin registrar contraseñas ni tokens. |
| RNF-14 | Recuperación | Documentar y demostrar backup/restore de la base de datos de demostración. |

## 2.3 Requisitos de seguridad específicos

- RS-01: contraseñas con política mínima definida y bloqueo temporal tras intentos fallidos configurados.
- RS-02: roles del backend `Administrador`, `Empleado`, `EncargadoAlmacen`.
- RS-03: protección CSRF si se usa cookie de autenticación.
- RS-04: HTTPS fuera del loopback local.
- RS-05: validación de entrada en servidor y consultas parametrizadas.
- RS-06: usuario de base de datos con permisos mínimos para la aplicación.
- RS-07: secretos fuera del repositorio.
- RS-08: auditoría de ajustes de stock, compra recibida, venta y gestión de usuarios.

## 2.4 Reglas de negocio

- RN-01: SKU es único y obligatorio.
- RN-02: cantidad de venta/compra debe ser un entero mayor que cero.
- RN-03: costo y precio no pueden ser negativos.
- RN-04: no se vende producto inactivo ni sin stock suficiente.
- RN-05: el total se calcula en backend: suma de líneas menos descuento permitido.
- RN-06: la compra solo aumenta stock al cambiar a `Received`.
- RN-07: una compra solo puede recibirse una vez.
- RN-08: ventas y compras recibidas no se eliminan físicamente; cualquier reverso futuro debe ser una operación separada y auditada.
- RN-09: los ajustes de inventario requieren motivo.
- RN-10: el producto conserva el precio y nombre aplicado en la venta a través de sus líneas históricas.

## 2.5 Matriz de trazabilidad mínima

| Requisito | Módulo | Prueba principal |
|---|---|---|
| RF-01, RF-03 | Auth/Users | QA-01, QA-02, QA-03 |
| RF-05, RF-06 | Catalog/Products | QA-04, QA-05 |
| RF-08, RF-09 | Purchases | QA-06, QA-07 |
| RF-10 | Sales | QA-08, QA-09, QA-10 |
| RF-12, RF-13, RF-14 | Inventory | QA-11, QA-12 |
| RF-15, RF-16 | Reports | QA-13 |
| RF-17 | Audit | QA-14 |
