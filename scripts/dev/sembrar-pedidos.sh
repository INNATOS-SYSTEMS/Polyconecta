#!/bin/bash
# Crea N pedidos por la API en estados variados (quickstart §5, L2-T033): 40 % en Borrador, 30 % Confirmado,
# 20 % Autorizado (las dos firmas) y 10 % Cancelado. Los captura ac1; firman comercial1 y cobranza1.
# Requiere la API arriba (./run.sh) con los usuarios de R1 sembrados y los catálogos sincronizados.
#
#   scripts/dev/sembrar-pedidos.sh 500
#   API=http://localhost:9020 R1_PASSWORD=... scripts/dev/sembrar-pedidos.sh 50
set -euo pipefail

N="${1:-20}"
API="${API:-http://localhost:9020}/api/v1"
REPO="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
if [ -z "${R1_PASSWORD:-}" ] && [ -f "$REPO/.env.local" ]; then
    R1_PASSWORD="$(grep '^LOCAL_R1_PASSWORD=' "$REPO/.env.local" | cut -d= -f2-)"
fi
[ -n "${R1_PASSWORD:-}" ] || { echo "Falta R1_PASSWORD (o LOCAL_R1_PASSWORD en .env.local)." >&2; exit 1; }
command -v jq > /dev/null || { echo "Hace falta jq." >&2; exit 1; }

TMP="$(mktemp -d)"
trap 'rm -rf "$TMP"' EXIT

# pedir <usuario> <método> <ruta> [cuerpo]: con la cookie de ese usuario; falla si la API no responde 2xx.
pedir() {
    local usuario="$1" metodo="$2" ruta="$3" cuerpo="${4:-}"
    local args=(-sS -X "$metodo" -b "$TMP/$usuario" -c "$TMP/$usuario" -H 'Content-Type: application/json' -H 'X-Requested-With: PolyConecta' -w '\n%{http_code}')
    [ -n "$cuerpo" ] && args+=(-d "$cuerpo")
    local salida codigo
    salida="$(curl "${args[@]}" "$API$ruta")"
    codigo="${salida##*$'\n'}"
    salida="${salida%$'\n'*}"
    if [[ "$codigo" != 2* ]]; then
        echo "✘ $usuario $metodo $ruta → $codigo: $salida" >&2
        return 1
    fi
    printf '%s' "$salida"
}

for u in ac1 comercial1 cobranza1; do
    pedir "$u" POST /plataforma/sesion "$(jq -nc --arg u "$u" --arg c "$R1_PASSWORD" '{usuario: $u, contrasena: $c}')" > /dev/null
done

pedir ac1 GET "/ventas/clientes/buscar?texto=" > "$TMP/clientes.json"
pedir ac1 GET "/inventario/productos/buscar?texto=" > "$TMP/productos.json"
CLIENTES="$(jq length "$TMP/clientes.json")"
PRODUCTOS="$(jq length "$TMP/productos.json")"
[ "$CLIENTES" -gt 0 ] && [ "$PRODUCTOS" -gt 0 ] || { echo "Sin clientes o productos: sincroniza los catálogos primero." >&2; exit 1; }

borrador=0; confirmado=0; autorizado=0; cancelado=0
for i in $(seq 1 "$N"); do
    cliente="$(jq -c ".[$(( (i - 1) % CLIENTES ))]" "$TMP/clientes.json")"
    moneda="$(jq -r '.moneda // "MXN"' <<< "$cliente")"
    lineas="$(jq -c --argjson i "$i" --argjson n "$PRODUCTOS" '
        [range(0; 1 + ($i % 3)) as $k | .[(($i + $k) % $n)] | {productoId: .id, cantidad: (10 * ($i % 7 + 1)), precioUnitario: (1.5 + ($k * 0.75))}]
        | unique_by(.productoId)' "$TMP/productos.json")"
    cuerpo="$(jq -nc --argjson c "$cliente" --arg m "$moneda" --argjson l "$lineas" \
        '{clienteId: $c.id, moneda: $m, tipoCambio: (if $m == "MXN" then 1 else 18.5 end), lineas: $l}')"
    pedido="$(pedir ac1 POST /ventas/pedidos "$cuerpo")"
    id="$(jq -r .id <<< "$pedido")"
    rv="$(jq -r .rowVersion <<< "$pedido")"

    resto=$(( i % 10 ))
    if [ "$resto" -lt 4 ]; then
        borrador=$((borrador + 1))
    elif [ "$resto" -eq 9 ]; then
        pedir ac1 POST "/ventas/pedidos/$id/cancelar" "$(jq -nc --arg r "$rv" '{rowVersion: $r, motivo: "Datos de prueba"}')" > /dev/null
        cancelado=$((cancelado + 1))
    else
        pedido="$(pedir ac1 POST "/ventas/pedidos/$id/confirmar" "$(jq -nc --arg r "$rv" '{rowVersion: $r}')")"
        if [ "$resto" -lt 7 ]; then
            confirmado=$((confirmado + 1))
        else
            for firmante in comercial1 cobranza1; do
                rv="$(jq -r .rowVersion <<< "$pedido")"
                pedido="$(pedir "$firmante" POST "/ventas/pedidos/$id/autorizar" "$(jq -nc --arg r "$rv" '{rowVersion: $r}')")"
            done
            autorizado=$((autorizado + 1))
        fi
    fi
    if (( i % 50 == 0 )); then echo "  $i de $N"; fi
done

echo "✓ $N pedidos: $borrador Borrador, $confirmado Confirmado, $autorizado Autorizado, $cancelado Cancelado."
