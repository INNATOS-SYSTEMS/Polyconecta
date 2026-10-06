#!/bin/bash
# ==============================================================================
# PolyConecta - Solution Build, Test & Multi-Layer Launch Script
# ==============================================================================
# Usage:
#   ./run.sh                  # Inicia PolyConecta.Web (9000) + API (9020)
#   ./run.sh --with-bridge    # Además levanta el bridge de CONTPAQi (9030)
#   ./run.sh --solo-web       # Solo PolyConecta.Web (9000): sin .NET, SQL Server, pruebas ni API
#
# El prototipo Blazor (PolyConecta.Presentation) ya no se levanta: es solo la referencia de las
# pruebas de paridad de PolyConecta.Web, que lo arrancan por su cuenta en :9010.
# ==============================================================================

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
cd "$REPO_ROOT"

WEB_PORT="9000"
API_PORT="9020"
BRIDGE_PORT="9030"
WITH_BRIDGE=false
SOLO_WEB=false

for arg in "$@"; do
    case $arg in
        --with-bridge|--bridge)
            WITH_BRIDGE=true
            ;;
        --solo-web|--web)
            SOLO_WEB=true
            ;;
        --with-angular|--angular)
            # PolyConecta.Web ya se levanta siempre; se acepta para no romper a quien lo usaba.
            ;;
        [0-9]*)
            API_PORT="$arg"
            ;;
    esac
done

echo "================================================================="
echo "🚀  PolyConecta Operational Suite - Build, Test & Run"
echo "================================================================="
echo "Repository Root: ${REPO_ROOT}"
echo "PolyConecta.Web (Angular)            : ${WEB_PORT}"
if [ "$SOLO_WEB" = true ]; then
    echo "Modo                                : solo la capa web (--solo-web)"
else
echo "Swagger & REST API (PolyConecta.Api) : ${API_PORT}"
if [ "$WITH_BRIDGE" = true ]; then
    echo "CONTPAQi Bridge Port                : ${BRIDGE_PORT} (Activo)"
else
    echo "CONTPAQi Bridge Mode                : Desactivado (Usa --with-bridge para arrancar)"
fi
fi
echo "================================================================="

# Step 0: PolyConecta.Web necesita la versión de Node fijada en .nvmrc (spec 001, L2-T067).
echo "🔍 Step 0: Checking Node for PolyConecta.Web..."
NODE_REQUERIDO="$(tr -d 'v[:space:]' < PolyConecta.Web/.nvmrc)"
if ! command -v node > /dev/null 2>&1; then
    echo "❌ Error: PolyConecta.Web requiere Node ${NODE_REQUERIDO} (PolyConecta.Web/.nvmrc) y no hay 'node' en PATH."
    exit 1
fi
NODE_ACTUAL="$(node --version | tr -d 'v')"
if [ "$NODE_ACTUAL" != "$NODE_REQUERIDO" ]; then
    echo "❌ Error: PolyConecta.Web requiere Node ${NODE_REQUERIDO} (PolyConecta.Web/.nvmrc) y está ${NODE_ACTUAL}. Usa 'nvm use'."
    exit 1
fi
if [ ! -d PolyConecta.Web/node_modules ]; then
    echo "   Instalando dependencias de PolyConecta.Web (npm ci)..."
    (cd PolyConecta.Web && npm ci)
fi
echo "   Node ${NODE_ACTUAL}"

# --solo-web: la réplica no necesita la API (el chatter queda "Sin conexión en vivo" y agrega en local).
if [ "$SOLO_WEB" = true ]; then
    echo "================================================================="
    echo "💻 PolyConecta.Web (Angular) : http://localhost:${WEB_PORT}"
    echo "Press Ctrl+C to stop the server."
    echo "================================================================="
    cd PolyConecta.Web
    exec npm start -- --port "${WEB_PORT}"
fi

# Step 1: Locate a .NET installation that can build and run the solution.
echo "🔍 Step 1: Checking .NET SDK environment..."

# La solución corre en .NET 10 (CT-04). El prototipo Blazor sigue en net8.0 (D-60), pero run.sh ya
# no lo levanta: el runtime 8 solo hace falta para las pruebas de paridad de PolyConecta.Web.
has_runtimes() {
    "$1" --list-runtimes 2>/dev/null | grep -q '^Microsoft\.AspNetCore\.App 10\.'
}

DOTNET_BIN=""
for candidate in "$(command -v dotnet 2>/dev/null)" \
                 "/usr/local/share/dotnet/dotnet" \
                 "/usr/share/dotnet/dotnet" \
                 "$HOME/.dotnet/dotnet"; do
    if [ -n "$candidate" ] && [ -x "$candidate" ] && has_runtimes "$candidate"; then
        DOTNET_BIN="$candidate"
        break
    fi
done

if [ -z "$DOTNET_BIN" ]; then
    echo "❌ Error: No se encontró una instalación de .NET con el runtime de ASP.NET Core 10.x."
    if command -v dotnet &> /dev/null; then
        echo "   'dotnet' en PATH: $(command -v dotnet)"
        echo "   Runtimes disponibles ahí:"
        dotnet --list-runtimes 2>/dev/null | sed 's/^/     /'
    fi
    echo "   Instala el SDK 10 (global.json)."
    exit 1
fi

# Antepone la instalacion elegida al PATH para que los procesos hijos
# (testhost, dotnet run) usen esta misma instalacion de forma consistente.
DOTNET_ROOT="$(dirname "$DOTNET_BIN")"
export DOTNET_ROOT
export PATH="$DOTNET_ROOT:$PATH"

echo "   Using .NET installation: ${DOTNET_ROOT}"
echo "   Found .NET SDK version: $("$DOTNET_BIN" --version)"

# Step 1b: SQL Server de PolyConecta (CT-05). Si no hay ConnectionStrings__PolyConecta en el
# entorno, se levanta (o reutiliza) un SQL Server 2022 local en Docker con los logins de CT-30.
# Las contraseñas locales se generan la primera vez en .env.local, que git ignora (CT-29).
SQL_CONTAINER="polyconecta-sql"
SQL_PORT="14333"
if [ -z "${ConnectionStrings__PolyConecta:-}" ]; then
    echo "🗄️  Step 1b: SQL Server local en Docker (${SQL_CONTAINER}, puerto ${SQL_PORT})..."
    if ! docker info > /dev/null 2>&1; then
        echo "❌ Error: Docker no está en marcha y no hay ConnectionStrings__PolyConecta en el entorno."
        exit 1
    fi
    if [ ! -f .env.local ]; then
        gen() { LC_ALL=C tr -dc 'A-Za-z0-9' < /dev/urandom | head -c 20; }
        {
            echo "LOCAL_SA_PASSWORD=Sa_$(gen)1!"
            echo "LOCAL_APP_PASSWORD=App_$(gen)1!"
            echo "LOCAL_MIGRACIONES_PASSWORD=Mig_$(gen)1!"
        } > .env.local
    fi
    # shellcheck disable=SC1091
    . ./.env.local
    if ! docker ps --format '{{.Names}}' | grep -q "^${SQL_CONTAINER}$"; then
        if docker ps -a --format '{{.Names}}' | grep -q "^${SQL_CONTAINER}$"; then
            docker start "$SQL_CONTAINER" > /dev/null
        else
            docker run -d --name "$SQL_CONTAINER" -e ACCEPT_EULA=Y -e "MSSQL_SA_PASSWORD=${LOCAL_SA_PASSWORD}" \
                -p "${SQL_PORT}:1433" mcr.microsoft.com/mssql/server:2022-latest > /dev/null
        fi
    fi
    SQLCMD=(docker exec "$SQL_CONTAINER" /opt/mssql-tools18/bin/sqlcmd -C -S localhost -U sa -P "$LOCAL_SA_PASSWORD")
    for _ in $(seq 1 60); do "${SQLCMD[@]}" -Q "SELECT 1" > /dev/null 2>&1 && break; sleep 2; done
    docker cp scripts/sql/logins-desarrollo.sql "$SQL_CONTAINER:/tmp/logins-desarrollo.sql"
    "${SQLCMD[@]}" -b -i /tmp/logins-desarrollo.sql -v Base=PolyConecta \
        "AppPassword=${LOCAL_APP_PASSWORD}" "MigracionesPassword=${LOCAL_MIGRACIONES_PASSWORD}" > /dev/null
    export ConnectionStrings__PolyConecta="Server=localhost,${SQL_PORT};Database=PolyConecta;User Id=polyconecta_app;Password=${LOCAL_APP_PASSWORD};TrustServerCertificate=true"
    export ConnectionStrings__PolyConectaMigraciones="Server=localhost,${SQL_PORT};Database=PolyConecta;User Id=polyconecta_migraciones;Password=${LOCAL_MIGRACIONES_PASSWORD};TrustServerCertificate=true"
fi

# Step 2: Build complete solution
echo "🛠️  Step 2: Building full solution (Polyconecta.slnx)..."
"$DOTNET_BIN" build Polyconecta.slnx -c Debug

# Step 2b: Aplicar migraciones con el login de migraciones (CT-06, CT-30)
if [ -n "${ConnectionStrings__PolyConectaMigraciones:-}" ]; then
    echo "📐 Step 2b: Applying EF Core migrations..."
    "$DOTNET_BIN" tool restore > /dev/null
    "$DOTNET_BIN" ef database update --project PolyConecta.Infrastructure --startup-project PolyConecta.Api --no-build
fi

# Step 3: Run test suites
echo "🧪 Step 3: Running domain unit & integration test suites..."
"$DOTNET_BIN" test --solution Polyconecta.slnx --no-build

echo "================================================================="
echo "✅ Build & Tests Succeeded! Launching PolyConecta Solution Layers..."
echo "================================================================="
echo "💻 PolyConecta.Web (Angular)      : http://localhost:${WEB_PORT}"
echo "📚 Swagger API (PolyConecta.Api)  : http://localhost:${API_PORT}/swagger"
echo "⚙️  REST API Endpoints Base       : http://localhost:${API_PORT}/api/v1"

PIDS=()
cleanup() {
    echo ""
    echo "🛑 Shutting down PolyConecta processes..."
    for pid in "${PIDS[@]}"; do
        # npm start deja a ng serve como proceso hijo: se apagan los hijos primero.
        pkill -P "$pid" 2>/dev/null || true
        kill "$pid" 2>/dev/null || true
    done
}
trap cleanup EXIT INT TERM

if [ "$WITH_BRIDGE" = true ]; then
    # Fuera de Windows el bridge arranca en modo simulado (D-122). La API le envía su outbox y recibe
    # los callbacks firmados con un secreto local de .env.local (CT-29).
    if [ -f .env.local ] && ! grep -q '^LOCAL_CALLBACK_SECRET=' .env.local; then
        echo "LOCAL_CALLBACK_SECRET=$(LC_ALL=C tr -dc 'A-Za-z0-9' < /dev/urandom | head -c 32)" >> .env.local
    fi
    # shellcheck disable=SC1091
    [ -f .env.local ] && . ./.env.local
    export BridgeConfig__CallbackSecret="${LOCAL_CALLBACK_SECRET:-secreto-local}"
    export BridgeConfig__DashboardPort="${BRIDGE_PORT}"
    export Erp__BridgeUrl="http://localhost:${BRIDGE_PORT}"
    export Erp__CallbackBaseUrl="http://localhost:${API_PORT}"
    export Erp__CallbackSecret="${BridgeConfig__CallbackSecret}"
    echo "🌉 CONTPAQi Bridge Worker         : http://localhost:${BRIDGE_PORT}"
    echo "-----------------------------------------------------------------"
    echo "Iniciando servicio CONTPAQi Bridge en segundo plano (Puerto ${BRIDGE_PORT})..."
    "$DOTNET_BIN" run --project PolyConecta.Contpaq/PolyConecta.Contpaq.csproj --no-build &
    PIDS+=($!)
fi

echo "-----------------------------------------------------------------"
echo "Iniciando PolyConecta.Web en segundo plano (Puerto ${WEB_PORT})..."
(cd PolyConecta.Web && exec npm start -- --port "${WEB_PORT}") &
PIDS+=($!)

echo "================================================================="
echo "Press Ctrl+C to stop the servers."
echo "================================================================="

# Step 4: Run PolyConecta.Api server in foreground
"$DOTNET_BIN" run --project PolyConecta.Api/PolyConecta.Api.csproj --no-build --urls "http://localhost:${API_PORT}"
