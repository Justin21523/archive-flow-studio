#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
PUBLISH_DIR="${PUBLISH_DIR:-$REPO_ROOT/src/ArchiveFlow.Browser/bin/Release/net10.0-browser/publish/wwwroot}"

if [[ -n "${BASE_PATH:-}" ]]; then
  BASE_HREF="$BASE_PATH"
elif [[ -n "${GITHUB_REPOSITORY:-}" ]]; then
  REPO_NAME="${GITHUB_REPOSITORY#*/}"
  BASE_HREF="/$REPO_NAME/"
else
  BASE_HREF="/archive-flow-studio/"
fi

if [[ "$BASE_HREF" != */ ]]; then
  BASE_HREF="$BASE_HREF/"
fi

test -f "$PUBLISH_DIR/index.html"
test -d "$PUBLISH_DIR/_framework"

sed -i "s#<base href=\"[^\"]*\" />#<base href=\"$BASE_HREF\" />#" "$PUBLISH_DIR/index.html"
touch "$PUBLISH_DIR/.nojekyll"

echo "Prepared GitHub Pages artifact at $PUBLISH_DIR"
echo "Base href: $BASE_HREF"
