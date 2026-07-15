#!/usr/bin/env bash

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SERVER_DIR="$(cd "${SCRIPT_DIR}/.." && pwd)"
COMPOSE_ENV="${SERVER_DIR}/releases/compose.env"
VERSION="${1:-}"

if [[ -z "${VERSION}" && -f "${COMPOSE_ENV}" ]]; then
  VERSION="$(grep '^RELEASE_VERSION=' "${COMPOSE_ENV}" | cut -d '=' -f 2- || true)"
fi

if [[ -z "${VERSION}" ]]; then
  echo "Usage: ./Tools/restart.sh <version>"
  exit 1
fi

"${SCRIPT_DIR}/stop.sh" "${VERSION}" || true
exec "${SCRIPT_DIR}/start.sh" "${VERSION}"
