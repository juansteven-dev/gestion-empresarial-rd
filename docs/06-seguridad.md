# 8. Seguridad del sistema

## 8.1 Modelo de amenazas básico

Activos: credenciales, datos de productos, precios, historial de ventas, existencias, identidad de empleados y registros de auditoría.

Amenazas: acceso sin autorización, fuerza bruta, modificación de precios/stock desde el cliente, inyección SQL, CSRF/XSS, exposición de secretos, robo de sesión, errores de concurrencia y pérdida de datos.

## 8.2 Controles

| Riesgo | Control propuesto | Evidencia |
|---|---|---|
| Contraseñas expuestas | ASP.NET Core Identity y su hash de contraseña; política fuerte y lockout | Configuración y prueba login |
| Usuario entra a módulo ajeno | Autorización por rol en endpoint y servicio | Prueba 403 por cada rol |
| Cliente manipula total/precio | Backend vuelve a leer catálogo y calcula importe | Prueba alterando payload |
| Venta de más unidades | Validación de stock y transacción/concurrencia | Prueba de stock insuficiente y concurrencia |
| CSRF con cookies | Antiforgery token en cambios de estado | Prueba sin token |
| SQL injection | EF Core y parámetros; no concatenar SQL | Revisión de código y caso de prueba |
| XSS | Renderizado seguro React; no usar dangerouslySetInnerHTML con datos de usuario | Revisión y prueba de entrada |
| Secretos filtrados | User Secrets/variables de entorno; `.gitignore` | Revisión de repositorio |
| Fuga de datos por errores | Problem Details sin stack trace en producción | Prueba de error |
| Acción no trazable | AuditLogs append-only para operaciones sensibles | Prueba de auditoría |
| Pérdida de información | Backup de base de datos y prueba de restauración | Archivo y acta de restore |

## 8.3 Autenticación y sesión

Recomendación para este prototipo: ASP.NET Core Identity con cookie de autenticación `HttpOnly`; marcar `Secure` en HTTPS y definir `SameSite` según despliegue. Utilizar antiforgery token en endpoints que cambian estado. Para simplificar cookies en desarrollo, usar proxy Vite `/api` y mismo origen desde el navegador. No guardar contraseña ni token de sesión en `localStorage`.

Si el equipo decide utilizar JWT en vez de cookies, documentar expiración corta, almacenamiento del access token en memoria, refresh token protegido en cookie HttpOnly, revocación/rotación y protección CSRF del refresh endpoint. No mezclar ambos diseños sin justificación.

## 8.4 Roles y permisos

| Función | Administrador | Empleado | EncargadoAlmacen |
|---|:---:|:---:|:---:|
| Iniciar/cerrar sesión | Sí | Sí | Sí |
| Gestionar usuarios | Sí | No | No |
| Consultar productos | Sí | Sí | Sí |
| Crear/editar productos | Sí | No | Sí |
| Gestionar categorías | Sí | No | Sí |
| Gestionar suplidores | Sí | No | Sí |
| Crear/recibir compras | Sí | No | Sí |
| Registrar ventas | Sí | Sí | Opcional (definir política final) |
| Ajustar inventario | Sí | No | Sí |
| Dashboard por rol | Sí | Sí (ventas propias) | Sí (inventario) |
| Reportes globales | Sí | No | No |
| Consultar auditoría | Sí | No | No |

*El alcance mínimo del proyecto limita reportes globales al Administrador; se puede decidir permitir reportes de inventario al Encargado.

El dashboard aplica alcance de datos en servidor: Administrador ve métricas globales, Empleado solo sus ventas y EncargadoAlmacen alertas de inventario. `/api/v1/reports`, `/api/v1/users` y `/api/v1/audit` exigen el rol Administrador.

## 8.5 Registro de auditoría

Registrar al menos: creación/edición/desactivación de productos y suplidores; recepción de compras; ventas; ajustes de stock; creación/desactivación de usuarios y cambios de roles. Campos: UserId, Action, EntityName, EntityId, DetailsJson filtrado, IpAddress si procede, CreatedAtUtc. Nunca guardar credenciales, cookies, tokens o números de tarjetas.

## 8.6 Copias de seguridad

Documentar una copia manual de SQL Server:

```sql
BACKUP DATABASE GestionEmpresarialRD
TO DISK = 'C:\\Backups\\GestionEmpresarialRD.bak'
WITH INIT, CHECKSUM;
```

Restauración de demostración en otra base o instancia siguiendo las restricciones del entorno. Verificar con `RESTORE VERIFYONLY` y realizar al menos una restauración real para evidenciar recuperación. No guardar copias con datos reales en el repositorio.

## 8.7 Checklist antes de demo

- [ ] No existen contraseñas ni connection strings en Git.
- [ ] Usuarios de demo tienen datos ficticios.
- [ ] Endpoints verifican identidad y rol en backend.
- [ ] Operaciones críticas usan transacciones.
- [ ] Errores no revelan stack traces.
- [ ] Auditoría registra las acciones críticas.
- [ ] Backup restaurado al menos una vez.
## 8.8 Configuración implementada en la fase A

La API implementa login, logout y consulta de sesión en `/api/v1/auth`; `GET /api/v1/auth/csrf` entrega el token que el cliente envía como `X-CSRF-TOKEN` en operaciones de escritura. La creación de cuentas está protegida por el rol `Administrador` en el servidor y no existe registro público. Los únicos roles válidos son `Administrador`, `Empleado` y `EncargadoAlmacen`. Las instrucciones locales para crear el primer administrador y retirar esos secretos después están en `src/backend/README.md`.

## 8.9 Ventas implementadas en la fase D

`POST /api/v1/sales` requiere sesión, rol `Administrador` o `Empleado` y token CSRF. El contrato solo contiene método de pago y líneas con `ProductId`/`Quantity`; cualquier importe calculado en el cliente se ignora. El servicio vuelve a leer producto activo, precio y stock, y guarda la operación dentro de una transacción SQL Server. `RowVersion` de Products convierte una modificación concurrente del stock en conflicto 409. El historial y el detalle de un Empleado solo muestran sus propias ventas; Administrador puede consultar todas. EncargadoAlmacen no puede registrar ni consultar ventas.
