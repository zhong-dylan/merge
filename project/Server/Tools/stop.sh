#!/usr/bin/env bash

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SERVER_DIR="$(cd "${SCRIPT_DIR}/.." && pwd)"
RELEASES_DIR="${SERVER_DIR}/releases"
COMPOSE_FILE="${RELEASES_DIR}/docker-compose.yml"
COMPOSE_ENV="${RELEASES_DIR}/compose.env"
ARG="${1:-}"
STOP_DB=0

cd "${SERVER_DIR}"

if [[ "${ARG}" == "--db" ]]; then
  STOP_DB=1
  ARG="${2:-}"
fi

if [[ -f "${COMPOSE_ENV}" ]]; then
  current_version="$(grep '^RELEASE_VERSION=' "${COMPOSE_ENV}" | cut -d '=' -f 2- || true)"
else
  current_version=""
fi

version="${ARG:-${current_version}}"
if [[ -z "${version}" ]]; then
  echo "Usage:"
  echo "  ./Tools/stop.sh [version]"
  echo "  ./Tools/stop.sh --db [version]"
  exit 1
fi

if [[ "${version}" == "${current_version}" && -f "${COMPOSE_FILE}" && -f "${COMPOSE_ENV}" ]]; then
  echo "Stopping current Nakama ${version}..."
  docker compose --project-name "nakama${version}" --env-file "${COMPOSE_ENV}" -f "${COMPOSE_FILE}" down
else
  echo "Stopping Nakama ${version} container..."
  docker rm -f "nakama${version}" >/dev/null 2>&1 || true
fi

if [[ "${STOP_DB}" == "1" ]]; then
  echo "Stopping shared PostgreSQL..."
  docker stop nakama-postgres >/dev/null 2>&1 || true
fi

echo "Done."
