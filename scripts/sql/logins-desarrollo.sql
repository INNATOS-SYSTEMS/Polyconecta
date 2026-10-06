-- Base y logins de PolyConecta para desarrollo local y CI (CT-30).
-- Las contraseñas llegan como variables de sqlcmd; nunca se escriben aquí (CT-29):
--   sqlcmd -S localhost -U sa -P <sa> -C -i scripts/sql/logins-desarrollo.sql \
--     -v Base=PolyConecta AppPassword=<app> MigracionesPassword=<migraciones>
--
-- polyconecta_app          lectura, escritura y EXECUTE; sin DDL. Es el login de la API.
-- polyconecta_migraciones  db_owner de la base. Solo para `dotnet ef database update`.

IF DB_ID(N'$(Base)') IS NULL
    EXEC(N'CREATE DATABASE [$(Base)]');
GO

IF SUSER_ID(N'polyconecta_app') IS NULL
    CREATE LOGIN polyconecta_app WITH PASSWORD = N'$(AppPassword)', CHECK_POLICY = OFF;
IF SUSER_ID(N'polyconecta_migraciones') IS NULL
    CREATE LOGIN polyconecta_migraciones WITH PASSWORD = N'$(MigracionesPassword)', CHECK_POLICY = OFF;
GO

USE [$(Base)];
GO

IF USER_ID(N'polyconecta_app') IS NULL
    CREATE USER polyconecta_app FOR LOGIN polyconecta_app;
ALTER ROLE db_datareader ADD MEMBER polyconecta_app;
ALTER ROLE db_datawriter ADD MEMBER polyconecta_app;
GRANT EXECUTE TO polyconecta_app;

IF USER_ID(N'polyconecta_migraciones') IS NULL
    CREATE USER polyconecta_migraciones FOR LOGIN polyconecta_migraciones;
ALTER ROLE db_owner ADD MEMBER polyconecta_migraciones;
GO
