# Prompt 02 — Identidad y autorización

Implementa RF-01, RF-02 y RF-03 de `docs/02-requisitos.md` usando ASP.NET Core Identity.

Requisitos:
- roles exactos: Administrador, Empleado, EncargadoAlmacen;
- no existe registro público; solo Admin crea usuarios;
- login/logout/me;
- política de contraseña y lockout;
- cookie HttpOnly y Secure en HTTPS; antiforgery para cambios de estado;
- seed de roles seguro e idempotente; no incluir credenciales fijas en código;
- proteger endpoints por rol y mostrar 401/403 adecuados;
- frontend con formulario login, contexto de sesión, rutas protegidas y logout;
- no guardar password/token en localStorage.

Añade pruebas: login correcto/incorrecto, empleado no crea usuario y usuario no autenticado no accede a ruta protegida. Revisa CSRF y documenta cómo se inicializa el primer administrador mediante variable de entorno/User Secrets o comando seguro.
