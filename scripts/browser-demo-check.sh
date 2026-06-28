#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"

cd "$REPO_ROOT"
"$SCRIPT_DIR/verify.sh"
"$SCRIPT_DIR/publish-browser.sh"
"$SCRIPT_DIR/verify-browser-static.sh"
