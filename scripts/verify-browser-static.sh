#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
PUBLISH_DIR="$REPO_ROOT/src/ArchiveFlow.Browser/bin/Release/net10.0-browser/publish/wwwroot"
PORT="${PORT:-5174}"

cd "$REPO_ROOT"
if [[ ! -f "$PUBLISH_DIR/index.html" ]]; then
  "$SCRIPT_DIR/publish-browser.sh"
fi

python3 -m http.server "$PORT" --directory "$PUBLISH_DIR" >/tmp/archiveflow-browser-static.log 2>&1 &
SERVER_PID=$!
trap 'kill "$SERVER_PID" >/dev/null 2>&1 || true' EXIT

for _ in {1..20}; do
  if curl -fsI "http://localhost:$PORT/" >/dev/null; then
    break
  fi
  sleep 0.25
done

curl -fsI "http://localhost:$PORT/" >/dev/null
curl -fsI "http://localhost:$PORT/main.js" >/dev/null
curl -fsI "http://localhost:$PORT/_framework/dotnet.js" >/dev/null

echo "Static browser artifact verified at http://localhost:$PORT/"
