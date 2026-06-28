#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"

cd "$REPO_ROOT"
"$SCRIPT_DIR/ensure-wasm-tools.sh"
dotnet publish src/ArchiveFlow.Browser/ArchiveFlow.Browser.csproj --configuration Release

echo "Browser publish output:"
echo "$REPO_ROOT/src/ArchiveFlow.Browser/bin/Release/net10.0-browser/publish/wwwroot"
