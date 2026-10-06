/* ==============================================================================
   New-BridgeReadOnlyLogin.sql  ·  spec 002, L1-T001 (tarea 0.1, US-8)

   Crea el login de SOLO LECTURA con el que el bridge lee las tablas adm* de
   CONTPAQi (catálogos, existencias y capas) y le da db_datareader en cada base
   de la lista. Sustituye a `sa` en BridgeConfig__SqlConnectionString (CT-29,
   CT-30). Las escrituras del bridge van por el SDK, nunca por este login
   (Principio II).

   Variables de sqlcmd (nunca se escriben en este archivo):
     Login     Nombre del login, por ejemplo polyconecta_bridge_ro.
     Password  Contraseña del login. Cumple la política de Windows (CHECK_POLICY).
     Bases     Bases separadas por coma. Hoy el bridge solo lee tablas adm* de la
               base de la empresa (adPOLYEMPAQUES): agrega CompacWAdmin u otra
               base solo si una lectura la necesita. Es obligatoria: un :setvar
               aquí le ganaría a -v, así que el script no trae valor por omisión.

   Uso (PowerShell en el VPS, como administrador de SQL Server):

     $s = Read-Host 'Contraseña del login' -AsSecureString
     $env:Password = [Runtime.InteropServices.Marshal]::PtrToStringBSTR(
                       [Runtime.InteropServices.Marshal]::SecureStringToBSTR($s))
     sqlcmd -S localhost\COMPAC -E -b -i New-BridgeReadOnlyLogin.sql `
            -v Login="polyconecta_bridge_ro" Bases="adPOLYEMPAQUES"
     Remove-Item Env:Password

   sqlcmd toma $(Password) de la variable de entorno, así que la contraseña no
   queda en el historial ni en la lista de procesos.

   Es idempotente: si el login ya existe, le cambia la contraseña (sirve para
   rotarla) y en cada base solo agrega lo que falte.
   ============================================================================== */

:on error exit
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @login sysname       = N'$(Login)';
DECLARE @password nvarchar(128) = N'$(Password)';
DECLARE @bases nvarchar(max) = N'$(Bases)';
DECLARE @sql nvarchar(max);
DECLARE @q nvarchar(258) = QUOTENAME(@login);

-- ------------------------------------------------------------------ validaciones

IF @login = N'' OR @password = N''
    THROW 50001, 'Faltan las variables Login o Password de sqlcmd.', 1;

IF @login = N'sa' OR IS_SRVROLEMEMBER('sysadmin', @login) = 1
    THROW 50002, 'El login indicado es sa o pertenece a sysadmin: este script no lo modifica.', 1;

IF EXISTS (SELECT 1 FROM sys.server_principals WHERE name = @login AND type <> 'S')
    THROW 50003, 'Ya existe un principal con ese nombre que no es un login de SQL Server.', 1;

-- Todas las bases se comprueban antes de crear nada: un error a la mitad dejaría el login a medias.
DECLARE @resto nvarchar(max) = @bases + N',';
DECLARE @base sysname;
DECLARE @primera sysname = NULL;
WHILE LEN(@resto) > 0
BEGIN
    SET @base = LTRIM(RTRIM(LEFT(@resto, CHARINDEX(N',', @resto) - 1)));
    SET @resto = SUBSTRING(@resto, CHARINDEX(N',', @resto) + 1, LEN(@resto));
    IF @base = N'' CONTINUE;
    IF DB_ID(@base) IS NULL
        THROW 50004, 'Una de las bases de la lista no existe en este servidor. Revisa la variable Bases.', 1;
    SET @primera = COALESCE(@primera, @base);
END;
IF @primera IS NULL
    THROW 50005, 'La variable Bases no trae ninguna base.', 1;

-- ------------------------------------------------------------------ login

IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = @login)
BEGIN
    SET @sql = N'CREATE LOGIN ' + QUOTENAME(@login)
             + N' WITH PASSWORD = N''' + REPLACE(@password, N'''', N'''''') + N''''
             + N', CHECK_POLICY = ON, CHECK_EXPIRATION = OFF;';
    EXEC (@sql);
    PRINT N'Login creado: ' + @login;
END
ELSE
BEGIN
    SET @sql = N'ALTER LOGIN ' + QUOTENAME(@login)
             + N' WITH PASSWORD = N''' + REPLACE(@password, N'''', N'''''') + N''';';
    EXEC (@sql);
    PRINT N'El login ya existía: se actualizó su contraseña. ' + @login;
END;

-- ------------------------------------------------------------------ usuario y db_datareader por base

SET @resto = @bases + N',';
WHILE LEN(@resto) > 0
BEGIN
    SET @base = LTRIM(RTRIM(LEFT(@resto, CHARINDEX(N',', @resto) - 1)));
    SET @resto = SUBSTRING(@resto, CHARINDEX(N',', @resto) + 1, LEN(@resto));
    IF @base = N'' CONTINUE;

    SET @sql = N'USE ' + QUOTENAME(@base) + N';
        IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = @login)
            EXEC (N''CREATE USER '' + @q + N'' FOR LOGIN '' + @q + N'';'');
        IF IS_ROLEMEMBER(''db_datareader'', @login) <> 1
            EXEC (N''ALTER ROLE db_datareader ADD MEMBER '' + @q + N'';'');';
    EXEC sp_executesql @sql, N'@login sysname, @q nvarchar(258)', @login = @login, @q = @q;
    PRINT N'Lectura en ' + @base + N': db_datareader.';
END;

-- La base por omisión del login es la primera de la lista: la cadena de conexión del bridge la nombra igual.
SET @sql = N'ALTER LOGIN ' + QUOTENAME(@login) + N' WITH DEFAULT_DATABASE = ' + QUOTENAME(@primera) + N';';
EXEC (@sql);

-- ------------------------------------------------------------------ verificación (va a la evidencia 0.1.md, sin secretos)

SELECT sp.name AS login, sp.is_disabled, sp.default_database_name,
       IS_SRVROLEMEMBER('sysadmin', sp.name) AS es_sysadmin
FROM sys.server_principals sp
WHERE sp.name = @login;

SET @resto = @bases + N',';
WHILE LEN(@resto) > 0
BEGIN
    SET @base = LTRIM(RTRIM(LEFT(@resto, CHARINDEX(N',', @resto) - 1)));
    SET @resto = SUBSTRING(@resto, CHARINDEX(N',', @resto) + 1, LEN(@resto));
    IF @base = N'' CONTINUE;
    SET @sql = N'USE ' + QUOTENAME(@base) + N';
        SELECT DB_NAME() AS base, u.name AS usuario, r.name AS rol
        FROM sys.database_principals u
        JOIN sys.database_role_members m ON m.member_principal_id = u.principal_id
        JOIN sys.database_principals r ON r.principal_id = m.role_principal_id
        WHERE u.name = @login;';
    EXEC sp_executesql @sql, N'@login sysname', @login = @login;
END;
