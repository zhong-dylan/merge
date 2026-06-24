#!/usr/bin/env bash

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SERVERCONFIG_COMPOSE_ENV="${SCRIPT_DIR}/serverconfig/compose.env"
cd "${SCRIPT_DIR}"

docker_compose() {
  docker compose --env-file "${SERVERCONFIG_COMPOSE_ENV}" "$@"
}

echo "Stopping Nakama and PostgreSQL containers..."
docker_compose down

echo "Services have been stopped."
