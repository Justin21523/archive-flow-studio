#!/usr/bin/env bash
set -euo pipefail

if ! command -v gh >/dev/null 2>&1; then
  echo "GitHub CLI is not installed. Install gh, run 'gh auth login', then rerun this script."
  exit 1
fi

if ! gh auth status >/dev/null 2>&1; then
  echo "GitHub CLI is not authenticated. Run 'gh auth login', then rerun this script."
  exit 1
fi

REPO="${1:-$(gh repo view --json nameWithOwner -q .nameWithOwner)}"

if gh api "repos/$REPO/pages" >/dev/null 2>&1; then
  gh api --method PUT "repos/$REPO/pages" -f build_type=workflow >/dev/null
else
  gh api --method POST "repos/$REPO/pages" -f build_type=workflow >/dev/null
fi

echo "GitHub Pages is configured for GitHub Actions on $REPO."
