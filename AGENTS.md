# Instrucciones permanentes para agentes de código

Actúa como ingeniero de software senior y tutor técnico. Este proyecto es un sistema académico cliente-servidor. No inventes que una función existe, que una prueba pasó o que una integración está conectada si no se ha verificado.

## Objetivo y arquitectura

- Nombre: Gestión Empresarial RD.
- Problema: pequeños comercios dominicanos llevan inventario, ventas y compras en cuadernos o múltiples hojas de cálculo, con errores, poca trazabilidad y reportes tardíos.
- Cliente: React + TypeScript + Vite.
- Servidor: ASP.NET Core Web API en .NET 10 LTS.
- Persistencia: SQL Server mediante EF Core 10.
- Arquitectura: monolito modular cliente-servidor con separación de capas; no microservicios.
- Comunicación: HTTPS, API REST, JSON, rutas bajo `/api/v1`.

## Reglas de diseño

1. No acoplar React a Entity Framework ni a modelos internos del backend. El cliente consume DTOs.
2. El controlador HTTP solo coordina entrada/salida; las reglas de negocio viven en servicios/casos de uso.
3. Domain no depende de Application, Infrastructure ni API.
4. Application define casos de uso, DTOs, interfaces y validaciones de aplicación.
5. Infrastructure implementa persistencia, identidad y servicios externos.
6. API configura DI, middleware, autenticación, autorización, CORS/proxy, OpenAPI y controladores.
7. Usar nombres y código en inglés de forma consistente; documentación del curso en español.
8. Dinero: `decimal(18,2)` y moneda DOP. No usar `float`/`double`.
9. Fechas en UTC (`DateTimeOffset` recomendado).
10. No introducir microservicios, colas, Redis, Kubernetes o CQRS completo sin justificarlo en una decisión arquitectónica.

## Reglas de negocio críticas

- `Administrador`: acceso completo y gestión de usuarios.
- `Empleado`: consultar productos y registrar ventas; no administra usuarios ni suplidores.
- `EncargadoAlmacen`: gestionar productos, categorías, suplidores, compras y ajustes de inventario; no administra usuarios.
- No vender cantidades mayores al stock disponible.
- Venta: crear encabezado, líneas, descontar stock, generar movimientos y auditoría en una única transacción.
- Recibir compra: cambiar estado a `Received`, aumentar stock, generar movimientos y auditoría en una única transacción.
- No recibir una compra dos veces.
- No modificar una venta/compras recibida sin una operación explícita y auditada.
- Los cambios concurrentes de stock deben evitar stock negativo; usar transacción y concurrencia optimista (`rowversion`) o bloqueo apropiado.
- Producto inactivo no se puede vender ni comprar.
- Los totales se calculan en el servidor a partir de cantidades y precios validados; nunca confiar en el total enviado por el cliente.

## Seguridad obligatoria

- ASP.NET Core Identity para contraseñas, usuarios y roles.
- Cookie de autenticación `HttpOnly`, `Secure` en HTTPS y `SameSite` adecuado; si se usa autenticación por cookies, implementar protección antifalsificación (CSRF) para operaciones de escritura.
- En desarrollo, configurar Vite proxy `/api` para reducir problemas de cookies/CORS. En producción, servir cliente y API bajo el mismo origen o documentar CORS y credenciales explícitamente.
- Exigir autorización por rol en backend. Ocultar botones en frontend no es seguridad.
- Validar DTOs y reglas del dominio en servidor.
- Usar EF Core parametrizado; nunca concatenar input de usuario a SQL.
- No guardar contraseñas, tokens, connection strings o claves en el repositorio.
- Configurar secretos con .NET User Secrets / variables de entorno.
- Registrar errores sin exponer stack traces ni datos sensibles en producción.
- Añadir auditoría para login relevante, creación/edición de productos, ajustes de stock, recepción de compra, venta y administración de usuarios.

## Calidad y estilo

- Escribir código legible, pequeño y con responsabilidades claras.
- Manejar `CancellationToken` en operaciones I/O cuando sea práctico.
- Paginación para listados; filtros y ordenamiento con límites.
- Respuestas HTTP coherentes: 200/201/204, 400, 401, 403, 404, 409 y 500.
- Usar Problem Details para errores de API.
- Frontend: componentes reutilizables, rutas protegidas, manejo centralizado de API, formularios con validación y estados loading/empty/error.
- No crear archivos gigantes ni mezclar acceso a datos y JSX.
- No agregar dependencias sin explicar por qué son necesarias.

## Flujo de trabajo de la IA

Antes de cambiar archivos:
1. Resume qué vas a modificar y por qué.
2. Identifica requisitos e historias que cubre.
3. Comprueba estructura y convenciones existentes; no recrees proyectos ya creados.
4. Haz un cambio pequeño y coherente.
5. Ejecuta o indica los comandos de build/test aplicables.
6. Informa archivos modificados, pruebas ejecutadas, resultados reales y tareas pendientes.

No hagas un scaffold nuevo sobre archivos existentes sin revisar primero. No inventes versiones de paquetes: usa versiones estables compatibles y comprueba documentación oficial cuando una decisión dependa de versión actual.
