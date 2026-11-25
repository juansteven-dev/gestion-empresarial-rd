# Base de datos

La base de datos se llama `GestionEmpresarialRD` y usa SQL Server. El esquema de identidad (usuarios, roles, claims, logins y tokens) debe crearse mediante las migraciones de ASP.NET Core Identity/EF Core, no mediante contraseñas SQL propias.

El modelo lógico de negocio está documentado en `docs/05-modelo-de-datos.md`. El archivo `scripts/01-create-database.sql` crea la base y las tablas de negocio para revisión académica. Para la ejecución real del backend, se recomienda que EF Core sea el único propietario de las migraciones del esquema, para evitar divergencias entre SQL manual y el modelo C#.

## Opciones locales

- SQL Server Developer/Express instalado en Windows: crear la base de datos y configurar la cadena de conexión.
- Docker: crear un `.env` en la raíz con `MSSQL_SA_PASSWORD=UnaClaveLarga_DeDesarrollo_123!` y ejecutar `docker compose up -d`.

La contraseña de ejemplo es solo ilustrativa. Utiliza una clave distinta y nunca la publiques.
