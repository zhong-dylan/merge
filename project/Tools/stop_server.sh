#!/usr/bin/env bash
set -euo pipefail

WORKSPACE="$(cd "$(dirname "$0")/.." && pwd)"
SERVER_DIR="$WORKSPACE/Server"

cd "$SERVER_DIR"
./stop.sh
