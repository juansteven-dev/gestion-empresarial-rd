# 10. Estrategia y plan de pruebas

## 10.1 Tipos de prueba

- Unitarias: reglas de cálculo, validaciones, permisos de servicios y lógica de stock aislada.
- Integración: API + EF Core + SQL Server en entorno de pruebas.
- Funcionales: interacción completa desde UI y API.
- Aceptación: escenarios de negocio basados en historias de usuario.
- Seguridad básica: autenticación, autorización, validación, CSRF y manipulación de payload.
- Rendimiento básico: consultas y operaciones comunes con dataset documentado.
- Regresión: repetir casos críticos luego de cambios.

## 10.2 Entorno y datos de prueba

- Base de datos exclusiva de pruebas, no producción.
- Dataset ficticio: categoría Bebidas; SKU BEB-001, Agua 500ml, costo DOP 10.00, venta DOP 20.00, stock 20, mínimo 5; SKU SNK-001, Galletas, costo DOP 15.00, venta DOP 25.00, stock 2, mínimo 5.
- Suplidor ficticio: Distribuidora Demo.
- Usuarios de prueba creados localmente con los tres roles. Credenciales no se guardan en repositorio ni en documento público.
- Vaciar/restaurar la base antes de pruebas que alteren stock.

## 10.3 Casos de prueba

| ID | Caso | Datos/acción | Resultado esperado | Resultado obtenido |
|---|---|---|---|---|
| QA-01 | Login correcto | Usuario activo + contraseña correcta | 200; cookie/sesión válida; perfil cargado | Completar al ejecutar |
| QA-02 | Login incorrecto | Contraseña incorrecta | 401/400 genérico; no iniciar sesión | Completar al ejecutar |
| QA-03 | Permiso denegado | Empleado llama POST /users | 403 y no se crea usuario | Completar al ejecutar |
| QA-04 | SKU duplicado | Crear producto con SKU BEB-001 | 409/validación; no duplicar | Completar al ejecutar |
| QA-05 | Producto válido | Crear producto con categoría existente | 201 y persistido | Completar al ejecutar |
| QA-06 | Crear compra | Compra con 5 unidades de SKU SNK-001 | Compra Draft; stock no cambia | Completar al ejecutar |
| QA-07 | Recibir compra | Recibir compra anterior | Estado Received; stock 2->7; un movimiento | Completar al ejecutar |
| QA-08 | Venta válida | Vender 3 aguas a DOP 20 | Total DOP 60; stock 20->17 | Completar al ejecutar |
| QA-09 | Venta sin stock | Vender 4 galletas con stock 2 | 409; no venta ni movimiento; stock 2 | Completar al ejecutar |
| QA-10 | Doble recepción | Repetir POST receive misma compra | 409; no duplicar existencias | Completar al ejecutar |
| QA-11 | Ajuste sin motivo | Ajustar inventario sin notes | 400; no cambiar stock | Completar al ejecutar |
| QA-12 | Stock bajo | Consultar low-stock con Galletas | Aparece porque 2 <= 5 | Completar al ejecutar |
| QA-13 | Dashboard | Consultar reporte tras venta | Totales coinciden con base de datos | Completar al ejecutar |
| QA-14 | Auditoría | Registrar venta y recibir compra | Se guarda actor/acción/fecha/referencia | Completar al ejecutar |
| QA-15 | Payload manipulado | Enviar total/precio menor desde cliente | Backend ignora total enviado y recalcula | Completar al ejecutar |
| QA-16 | Concurrencia | Dos ventas simultáneas consumen stock restante | No stock negativo; conflicto controlado | Completar al ejecutar |

## 10.4 Plantilla de ejecución real

Para cada caso registrar:

- ID y versión/commit evaluado.
- Fecha/hora, responsable y entorno.
- Precondiciones y pasos ejecutados.
- Datos exactos de entrada (sin contraseñas).
- Resultado esperado y resultado obtenido.
- Estado: Aprobado / Fallido / Bloqueado / No ejecutado.
- Captura, respuesta Postman, log o reporte adjunto.
- Incidencia relacionada y corrección aplicada.

No cambiar `Completar al ejecutar` por `Aprobado` hasta que la prueba se ejecute y se archive evidencia.

## 10.5 Herramientas

- xUnit para unit tests .NET.
- WebApplicationFactory / TestServer para integración API cuando se implemente.
- Postman para solicitudes HTTP y colección de regresión.
- Browser DevTools para frontend.
- k6 o Apache JMeter para una prueba de carga sencilla si el tiempo lo permite.
- GitHub Issues para defectos y trazabilidad.

## 10.6 Criterios de entrada y salida

Entrada: build de cliente/servidor sin errores; base de pruebas preparada; usuario y datos ficticios; requisitos disponibles.

Salida: todos los QA Alta ejecutados; 0 defectos críticos abiertos; fallos conocidos documentados; evidencia guardada; `dotnet test` y `npm run build` ejecutados.
