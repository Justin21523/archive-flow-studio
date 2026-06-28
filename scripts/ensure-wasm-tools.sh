#!/usr/bin/env bash
set -euo pipefail

if dotnet workload list | grep -q '^wasm-tools[[:space:]]'; then
  echo "wasm-tools workload is already installed."
  exit 0
fi

echo "Installing wasm-tools workload..."
dotnet workload install wasm-tools
