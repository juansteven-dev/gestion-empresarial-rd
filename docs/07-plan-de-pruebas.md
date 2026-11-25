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
| QA-08 | Venta válida | Vender 2 productos a DOP 9.25 con stock 4 | Total DOP 18.50; stock 4->2 | Automatizado en InMemory: aprobado |
| QA-09 | Venta sin stock | Intentar vender 2 unidades con stock 1 | 409; no venta ni movimiento; stock 1 | Automatizado en InMemory: aprobado |
| QA-10 | Doble recepción | Repetir POST receive misma compra | 409; no duplicar existencias | Completar al ejecutar |
| QA-11 | Ajuste sin motivo | Ajustar inventario sin notes | 400; no cambiar stock | Completar al ejecutar |
| QA-12 | Stock bajo | Consultar low-stock con Galletas | Aparece porque 2 <= 5 | Completar al ejecutar |
| QA-13 | Dashboard | Consultar reporte tras venta | Totales coinciden con base de datos | Completar al ejecutar |
| QA-14 | Auditoría | Registrar venta y recibir compra | Se guarda actor/acción/fecha/referencia | Venta automatizada: actor, acción, número e importe comprobados |
| QA-15 | Payload manipulado | Enviar total/precio menor desde cliente | Backend ignora total enviado y recalcula | Automatizado: DOP 0.01 enviado; DOP 18.50 calculado |
| QA-16 | Concurrencia | Dos ventas simultáneas consumen stock restante | No stock negativo; conflicto controlado | InMemory con interceptor RowVersion: 1 respuesta 201, 1 respuesta 409, stock 0; SQL Server pendiente |

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

## 10.7 Automatizacion agregada para la Fase B

CatalogEndpointTests usa WebApplicationFactory y cubre paginacion de categorias; creacion y edicion; SKU duplicado; categoria inexistente y precio negativo; rechazo de ajuste sin motivo y de stock negativo; ajuste valido con movimiento y auditoria; alerta CurrentStock <= MinimumStock; desactivacion logica de producto/suplidor; y acceso por rol. La ejecucion real y el resultado final de esta suite deben quedar registrados por commit en la evidencia del equipo. EF Core InMemory no valida caracteristicas relacionales propias de SQL Server.
## 10.8 Verificacion realizada (2026-11-25)

- dotnet build src/backend/GestionEmpresarial.slnx --no-restore: compilacion correcta; 0 advertencias y 0 errores.
- dotnet test src/backend/tests/GestionEmpresarial.UnitTests/GestionEmpresarial.UnitTests.csproj --no-restore --logger "console;verbosity=minimal": 19 pruebas aprobadas, 0 fallidas y 0 omitidas. VSTest informo que VSTEST_ARTIFACTS_DIRECTORY no esta definida y uso un directorio temporal.
- npm.cmd run build (desde src/frontend): compilacion TypeScript y Vite correctas; 139 modulos transformados.
- La suite de API usa EF Core InMemory. No se ejecuto una prueba contra SQL Server real en esta fase.

## 10.9 Verificacion de la Fase C (2026-11-25)

- dotnet build src/backend/GestionEmpresarial.slnx --no-restore: compilacion correcta; 0 advertencias y 0 errores.
- dotnet test src/backend/tests/GestionEmpresarial.UnitTests/GestionEmpresarial.UnitTests.csproj --no-restore --logger "console;verbosity=minimal": 24 aprobadas, 0 fallidas y 0 omitidas. VSTest uso una carpeta temporal porque VSTEST_ARTIFACTS_DIRECTORY no esta definida.
- npm.cmd run build desde src/frontend: compilacion TypeScript y Vite correctas; 141 modulos transformados.
- Las pruebas de integracion usan EF Core InMemory y comprueban que un error en una linea posterior no deje cambios parciales persistidos. No ejercitan el rollback relacional de SQL Server; esa comprobacion queda pendiente de ejecutarse contra la base local.

## 10.10 Verificación de la Fase D (2026-11-25)

- `dotnet build src/backend/GestionEmpresarial.slnx --no-restore`: compilación correcta; 0 advertencias y 0 errores.
- `dotnet test src/backend/tests/GestionEmpresarial.UnitTests/GestionEmpresarial.UnitTests.csproj --no-restore --logger "console;verbosity=minimal"`: 29 aprobadas, 0 fallidas y 0 omitidas. VSTest avisó que `VSTEST_ARTIFACTS_DIRECTORY` no está definida y usó una carpeta temporal.
- `npm.cmd run build` desde `src/frontend`: TypeScript y Vite correctos; 143 módulos transformados.
- Las pruebas cubren precio calculado por servidor pese a importes falsos, productos inactivos y stock insuficiente, venta de varias líneas sin cambios parciales cuando falla la validación, auditoría, roles, alcance del historial, filtros y carrera por la última unidad.
- La carrera se coordinó en EF Core InMemory con un interceptor de pruebas que simula el control RowVersion; no equivale a concurrencia ni rollback en SQL Server. No se ejecutó contra SQL Server real, así que la reversión transaccional relacional queda pendiente de verificación en una base de pruebas.
- No se cambió el modelo de datos ni se generó una migración; Sales, SaleItems y StockMovements ya están en InitialCreate.

## 10.11 Verificación de la Fase E (2026-11-25)

- `dotnet build src/backend/GestionEmpresarial.slnx --no-restore`: correcto; 0 advertencias y 0 errores.
- `dotnet test src/backend/tests/GestionEmpresarial.UnitTests/GestionEmpresarial.UnitTests.csproj --no-restore --logger "console;verbosity=minimal"`: 31 aprobadas, 0 fallidas y 0 omitidas. VSTest usó una carpeta temporal porque `VSTEST_ARTIFACTS_DIRECTORY` no está definida.
- `npm.cmd run lint` desde `src/frontend`: código de salida 0; 0 errores y 1 advertencia `react(only-export-components)` en `src/auth/AuthContext.tsx`.
- `npm.cmd run build` desde `src/frontend`: correcto; TypeScript compiló y Vite transformó 749 módulos. Reportes se genera como chunk separado.
- Las pruebas nuevas ejercitan dashboard, reportes, usuarios y auditoría con roles. La suite utiliza EF Core InMemory; no se ejecutó una prueba contra SQL Server real.
- Las vistas de inicio de sesión, dashboard, productos/categorías, suplidores, compras/recepción, punto de venta, inventario/movimientos, reportes, usuarios y auditoría consumen API. No quedan pantallas con mocks ni rutas de placeholder.
