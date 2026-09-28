#!/bin/bash
# Empaqueta la estación de laboratorio (desde el Mac). Salida: tools/sdk-lab/dist/sdklab-workstation.zip
set -euo pipefail
cd "$(dirname "$0")/.."
OUT=dist/pkg
rm -rf dist && mkdir -p "$OUT/scripts" "$OUT/specs" "$OUT/matrix" "$OUT/evidence"

dotnet publish src/SdkLab.csproj -c Release -r win-x86 --self-contained true \
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o "$OUT"

cp AGENTS.md CLAUDE.md lab.config.example.json README.md "$OUT/"
cp scripts/*.ps1 "$OUT/scripts/"
cp specs/*.json "$OUT/specs/"
cp ../../docs/contpaq/MATRIZ_PRUEBAS_SDK_WIP_LOTES.md "$OUT/matrix/"
# Referencias del SDK/BD para consulta del agente (solo lectura)
cp ../../docs/contpaq/Referencia_SDK_CONTPAQi.md ../../docs/contpaq/Referencia_BD_CONTPAQi.md "$OUT/matrix/"
: > "$OUT/evidence/.gitkeep"
rm -f "$OUT"/*.pdb

# Sanidad: el paquete no debe llevar secretos
if grep -R -I -n -E 'Password=|User Id=sa|INNATOS' "$OUT" --include='*.json' --include='*.ps1' --include='*.md' | grep -v 'Password=\$(\$Cred' ; then
  echo "ABORTA: posible secreto en el paquete" >&2; exit 1
fi
(cd "$OUT" && zip -r -q ../sdklab-workstation.zip .)
echo "OK: $(du -h dist/sdklab-workstation.zip | cut -f1)  dist/sdklab-workstation.zip"
