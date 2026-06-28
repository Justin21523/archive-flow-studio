#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"

cd "$REPO_ROOT"

"$SCRIPT_DIR/ensure-wasm-tools.sh"
dotnet restore ArchiveFlow.sln
dotnet build ArchiveFlow.sln --configuration Release --no-restore
dotnet test ArchiveFlow.sln --configuration Release --no-build
