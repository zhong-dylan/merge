#!/usr/bin/env bash

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
LOG_DIR="${SCRIPT_DIR}/logs"
LOG_FILE="${LOG_DIR}/nakama-manual-$(date +%Y%m%d-%H%M%S).log"
cd "${SCRIPT_DIR}"

mkdir -p "${LOG_DIR}"

echo "Streaming Nakama logs..."
echo "Log file: ${LOG_FILE}"
docker compose logs -f nakama | tee -a "${LOG_FILE}"
