#!/bin/bash
# ==============================================================================
# PolyConecta - Solution Build, Test & Multi-Layer Launch Script
# ==============================================================================
# Usage:
#   ./run.sh                  # Inicia SPA (9000) + API Gateway (9020)
#   ./run.sh --with-bridge    # Inicia SPA (9000) + API Gateway (9020) + CONTPAQi Bridge (5005)
# ==============================================================================

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
cd "$REPO_ROOT"

PRESENTATION_PORT="9000"
API_PORT="9020"
BRIDGE_PORT="5005"
WITH_BRIDGE=false

for arg in "$@"; do
    case $arg in
        --with-bridge|--bridge)
            WITH_BRIDGE=true
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
echo "PolyConecta Web Presentation Port    : ${PRESENTATION_PORT}"
echo "Swagger & REST API (PolyConecta.Api) : ${API_PORT}"
if [ "$WITH_BRIDGE" = true ]; then
    echo "CONTPAQi Bridge Port                : ${BRIDGE_PORT} (Activo)"
else
    echo "CONTPAQi Bridge Mode                : Desactivado (Usa --with-bridge para arrancar)"
fi
echo "================================================================="

# Step 1: Locate a .NET installation that can actually build & run net8.0.
echo "🔍 Step 1: Checking .NET SDK environment..."

# El criterio es el RUNTIME de ASP.NET Core 8.x, no la version del SDK: un SDK
# mayor (9/10) compila net8.0 sin problema, pero solo si esa instalacion trae los
# assets de ASP.NET Core 8. Si no los trae, _framework/blazor.web.js se emite
# vacio -> 404 -> el circuito interactivo de Blazor nunca arranca -> UI inoperable.
# (El SDK debe ser 9+ de todas formas: el formato .slnx no existe en el SDK 8.)
has_aspnet8_runtime() {
    "$1" --list-runtimes 2>/dev/null | grep -q '^Microsoft\.AspNetCore\.App 8\.'
}

DOTNET_BIN=""
for candidate in "$(command -v dotnet 2>/dev/null)" \
                 "/usr/local/share/dotnet/dotnet" \
                 "/usr/share/dotnet/dotnet" \
                 "$HOME/.dotnet/dotnet"; do
    if [ -n "$candidate" ] && [ -x "$candidate" ] && has_aspnet8_runtime "$candidate"; then
        DOTNET_BIN="$candidate"
        break
    fi
done

if [ -z "$DOTNET_BIN" ]; then
    echo "❌ Error: No se encontro una instalacion de .NET con el runtime ASP.NET Core 8.x."
    if command -v dotnet &> /dev/null; then
        echo "   'dotnet' en PATH: $(command -v dotnet)"
        echo "   Runtimes disponibles ahi:"
        dotnet --list-runtimes 2>/dev/null | sed 's/^/     /'
    fi
    echo "   Instala el runtime 8 (macOS: brew install --cask dotnet-sdk@8)."
    exit 1
fi

# Antepone la instalacion elegida al PATH para que los procesos hijos
# (testhost, dotnet run) usen esta misma instalacion de forma consistente.
DOTNET_ROOT="$(dirname "$DOTNET_BIN")"
export DOTNET_ROOT
export PATH="$DOTNET_ROOT:$PATH"

echo "   Using .NET installation: ${DOTNET_ROOT}"
echo "   Found .NET SDK version: $("$DOTNET_BIN" --version)"

# Step 2: Build complete solution
echo "🛠️  Step 2: Building full solution (Polyconecta.slnx)..."
"$DOTNET_BIN" build Polyconecta.slnx -c Debug

# Step 3: Run test suites
echo "🧪 Step 3: Running domain unit & integration test suites..."
"$DOTNET_BIN" test Polyconecta.slnx --no-build --verbosity quiet

echo "================================================================="
echo "✅ Build & Tests Succeeded! Launching PolyConecta Solution Layers..."
echo "================================================================="
echo "💻 PolyConecta Web Presentation   : http://localhost:${PRESENTATION_PORT}"
echo "📚 Swagger API (PolyConecta.Api)  : http://localhost:${API_PORT}/swagger"
echo "⚙️  REST API Endpoints Base       : http://localhost:${API_PORT}/api/v1"

PIDS=()
cleanup() {
    echo ""
    echo "🛑 Shutting down PolyConecta processes..."
    for pid in "${PIDS[@]}"; do
        kill "$pid" 2>/dev/null || true
    done
}
trap cleanup EXIT INT TERM

if [ "$WITH_BRIDGE" = true ]; then
    echo "🌉 CONTPAQi Bridge Worker         : http://localhost:${BRIDGE_PORT}"
    echo "-----------------------------------------------------------------"
    echo "Iniciando servicio CONTPAQi Bridge en segundo plano (Puerto ${BRIDGE_PORT})..."
    "$DOTNET_BIN" run --project PolyConecta.Contpaq/PolyConecta.Contpaq.csproj --no-build --urls "http://localhost:${BRIDGE_PORT}" &
    PIDS+=($!)
fi

echo "-----------------------------------------------------------------"
echo "Iniciando PolyConecta Web Presentation en segundo plano (Puerto ${PRESENTATION_PORT})..."
"$DOTNET_BIN" run --project PolyConecta.Presentation/PolyConecta.Presentation.csproj --no-build --urls "http://localhost:${PRESENTATION_PORT}" &
PIDS+=($!)

echo "================================================================="
echo "Press Ctrl+C to stop the servers."
echo "================================================================="

# Step 4: Run PolyConecta.Api server in foreground
"$DOTNET_BIN" run --project PolyConecta.Api/PolyConecta.Api.csproj --no-build --urls "http://localhost:${API_PORT}"
