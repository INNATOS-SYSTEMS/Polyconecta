#!/bin/bash
# Empaqueta, sube y descomprime la estación sdk-lab en el VPS.
#   ./scripts/deploy-to-vps.sh                 # build completo + subir (~30 MB)
#   ./scripts/deploy-to-vps.sh --scripts-only  # solo scripts/specs/AGENTS (rápido, sin recompilar)
# No pisa lab.config.json, evidence/ ni los respaldos: el zip no los contiene.
set -euo pipefail

SSH_TARGET="${SSH_TARGET:-vps-innatos}"
REMOTE_DIR_WIN='C:\sdklab'
REMOTE_DIR_SCP='C:/sdklab'

cd "$(dirname "$0")/.."
SSH=(/usr/bin/ssh -o LogLevel=ERROR)      # LogLevel=ERROR calla el aviso informativo de post-cuántico
SCP=(/usr/bin/scp -o LogLevel=ERROR)

echo "Destino: $SSH_TARGET  ($REMOTE_DIR_WIN)"
"${SSH[@]}" "$SSH_TARGET" "New-Item -ItemType Directory -Force $REMOTE_DIR_WIN | Out-Null"

if [[ "${1:-}" == "--scripts-only" ]]; then
  "${SSH[@]}" "$SSH_TARGET" "New-Item -ItemType Directory -Force $REMOTE_DIR_WIN\\scripts, $REMOTE_DIR_WIN\\specs | Out-Null"
  "${SCP[@]}" scripts/*.ps1 "$SSH_TARGET:$REMOTE_DIR_SCP/scripts/"
  "${SCP[@]}" specs/*.json "$SSH_TARGET:$REMOTE_DIR_SCP/specs/"
  "${SCP[@]}" AGENTS.md CLAUDE.md README.md lab.config.example.json "$SSH_TARGET:$REMOTE_DIR_SCP/"
  "${SSH[@]}" "$SSH_TARGET" "Get-ChildItem $REMOTE_DIR_WIN -Recurse -File | Unblock-File"
  echo "OK (scripts-only)"; exit 0
fi

echo "1/4 Compilando y empaquetando..."
./scripts/Build-Package.sh

echo "2/4 Subiendo zip..."
"${SCP[@]}" dist/sdklab-workstation.zip "$SSH_TARGET:$REMOTE_DIR_SCP/sdklab-workstation.zip"

echo "3/4 Descomprimiendo en el VPS..."
"${SSH[@]}" "$SSH_TARGET" "Expand-Archive -Path $REMOTE_DIR_WIN\\sdklab-workstation.zip -DestinationPath $REMOTE_DIR_WIN -Force; Remove-Item $REMOTE_DIR_WIN\\sdklab-workstation.zip -Force; Get-ChildItem $REMOTE_DIR_WIN -Recurse -File | Unblock-File"

echo "4/4 Verificando..."
"${SSH[@]}" "$SSH_TARGET" "Get-ChildItem $REMOTE_DIR_WIN | Select-Object Name, Length; & '$REMOTE_DIR_WIN\\sdklab.exe' --help | Select-Object -First 3"
echo "OK. Siguiente: Install-Workstation.ps1 (una vez, sesión interactiva) o 'sdklab env'."
