#!/usr/bin/env bash

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SERVER_DIR="$(cd "${SCRIPT_DIR}/.." && pwd)"
LOG_DIR="${SERVER_DIR}/logs"
COMPOSE_FILE="${SERVER_DIR}/releases/docker-compose.yml"
COMPOSE_ENV="${SERVER_DIR}/releases/compose.env"

if [[ ! -f "${COMPOSE_FILE}" || ! -f "${COMPOSE_ENV}" ]]; then
  echo "Current server config has not been generated."
  echo "Run: ./Tools/switch.sh <version>"
  exit 1
fi

VERSION="$(grep '^RELEASE_VERSION=' "${COMPOSE_ENV}" | cut -d '=' -f 2-)"

mkdir -p "${LOG_DIR}"
LOG_FILE="${LOG_DIR}/nakama-${VERSION}-manual-$(date +%Y%m%d-%H%M%S).log"

echo "Streaming Nakama ${VERSION} logs..."
echo "Log file: ${LOG_FILE}"
docker compose --project-name "nakama${VERSION}" --env-file "${COMPOSE_ENV}" -f "${COMPOSE_FILE}" logs -f nakama | tee -a "${LOG_FILE}"
