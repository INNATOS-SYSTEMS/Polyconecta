# Quickstart: verificar F0

Cómo comprobar los criterios de éxito de la spec. Cada sección dice quién la corre.

## Requisitos (local)

- SDK de .NET 10.0.300 (`global.json`).
- Docker en marcha, para SQL Server 2022 con Testcontainers.
- Node 24.16.0 (`.nvmrc`).
- Variables en `user-secrets` o en el entorno:
  - `ConnectionStrings__PolyConecta` y `ConnectionStrings__PolyConectaMigraciones`;
  - `Erp__BridgeUrl`, `Erp__CallbackBaseUrl` y `Erp__CallbackSecret`;
  - en el bridge: `BridgeConfig__Mode=Simulated` y `BridgeConfig__CallbackSecret`, con el mismo secreto.

## 1. Contrato firmado (US-1, SC-001) · los dos líderes

```bash
npx @redocly/cli@2.58.1 lint docs/contratos/bridge-v1.openapi.yaml
```

**Esperado**:
- El OpenAPI valida sin errores.
- `bridge-v1.md` tiene las dos firmas y ningún ❓.
- `docs/contratos/ejemplos/` tiene una carga válida y una inválida por comando.

## 2. .NET 10 y SQL Server (US-2, SC-003) · L2

```bash
dotnet build Polyconecta.slnx
dotnet test Polyconecta.slnx
docker run -d --name pc-sql -e ACCEPT_EULA=Y -e MSSQL_SA_PASSWORD=<local> -p 1433:1433 mcr.microsoft.com/mssql/server:2022-latest
sqlcmd -S localhost -U sa -P <local> -i scripts/sql/logins-desarrollo.sql
dotnet ef database update --project PolyConecta.Infrastructure --startup-project PolyConecta.Api --connection "$ConnectionStrings__PolyConectaMigraciones"
dotnet run --project PolyConecta.Api
```

**Esperado**:
- Build y pruebas en verde, sin advertencias de versión. `grep -r 'Version=' --include=*.csproj` no devuelve nada, porque las versiones están en `Directory.Packages.props`.
- La base tiene el esquema `plt`.
- La API arranca con `polyconecta_app`, que no tiene DDL.

## 3. Bridge simulado (US-3, SC-002) · L1

```bash
BridgeConfig__Mode=Simulated dotnet run --project PolyConecta.Contpaq
curl -s localhost:5005/health
BRIDGE_URL=http://localhost:5005 dotnet test tests/PolyConecta.Contract.Tests
```

**Esperado**:
- `/health` dice `"mode": "Simulated"`.
- La suite pasa al 100 %: válidas, inválidas, idempotencia, versión y fallos simulados.

## 4. Ciclo completo (US-4, SC-004) · L2

```bash
dotnet test tests/PolyConecta.Application.Tests --filter "FullyQualifiedName~CicloCompleto"
```

**Esperado**: el `DocumentoDePrueba` sigue este ciclo:

1. Se crea con folio de su secuencia.
2. Su transición queda en `StateTransitionLog`.
3. Su mensaje queda en el outbox.
4. El despachador lo envía al simulador.
5. El callback guarda el folio y el id ERP, y su `SyncState` termina en `Confirmado`.

**Con el fallo forzado:** termina en `Error`, y `ReintentarSincronizacion` lo lleva a `Confirmado`.

## 5. Bridge en .NET 10 (US-5, SC-005) · L1, en el VPS

Seguir `tools/sdk-lab/EJECUCION_EN_VPS.md` con el paquete .NET 10 y correr los bloques F y G.

**Esperado**: el mismo resultado que la evidencia del 30 de septiembre en `tools/sdk-lab/evidence/`.

## 6. Aplicación web (US-6) · L2

```bash
cd PolyConecta.Web && npm ci && npm run build && npm test && npm start
```

**Esperado**: en `http://localhost:4200` se ve la barra superior y una página vacía por módulo. Los 12 componentes compartidos se ven igual que en el prototipo (`:9000`).

## 7. CI (US-7, SC-007) · L2

Abrir un PR con una prueba rota a propósito.

**Esperado**: los checks `dotnet`, `contrato` y `web` corren, el PR queda en rojo y no se puede integrar. Al corregirla, queda en verde.

## 8. Servidor del conector (US-8, SC-006) · L1, en el VPS

1. Intentar entrar con la contraseña anterior de `sa`: debe fallar.
2. Reiniciar el VPS sin iniciar sesión a mano.
3. En menos de 5 minutos, `GET /health` responde y `GET /api/v1/catalogs/warehouses` lee con el login de solo lectura.
4. El usuario `polyconecta-bridge` ya no existe.
