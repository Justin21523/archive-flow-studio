#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"

cd "$REPO_ROOT"
"$SCRIPT_DIR/ensure-wasm-tools.sh"
dotnet run --project src/ArchiveFlow.Browser/ArchiveFlow.Browser.csproj
