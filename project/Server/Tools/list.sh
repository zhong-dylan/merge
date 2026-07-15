#!/usr/bin/env bash

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SERVER_DIR="$(cd "${SCRIPT_DIR}/.." && pwd)"
COMPOSE_ENV="${SERVER_DIR}/releases/compose.env"

if ! docker info >/dev/null 2>&1; then
  echo "Docker daemon is not running."
  exit 1
fi

postgres_status="$(docker inspect -f '{{.State.Status}}' nakama-postgres 2>/dev/null || true)"
if [[ -n "${postgres_status}" ]]; then
  echo "Shared PostgreSQL: ${postgres_status}"
else
  echo "Shared PostgreSQL: not-created"
fi

echo
if [[ -f "${COMPOSE_ENV}" ]]; then
  version="$(grep '^RELEASE_VERSION=' "${COMPOSE_ENV}" | cut -d '=' -f 2-)"
  name="$(grep '^NAKAMA_NAME=' "${COMPOSE_ENV}" | cut -d '=' -f 2-)"
  http_port="$(grep '^NAKAMA_HTTP_PORT=' "${COMPOSE_ENV}" | cut -d '=' -f 2-)"
  console_port="$(grep '^NAKAMA_CONSOLE_PORT=' "${COMPOSE_ENV}" | cut -d '=' -f 2-)"
  grpc_port="$(grep '^NAKAMA_GRPC_PORT=' "${COMPOSE_ENV}" | cut -d '=' -f 2-)"
  status="$(docker inspect -f '{{.State.Status}}' "${name}" 2>/dev/null || true)"
  status="${status:-not-created}"

  printf "%-10s %-16s %-12s %-22s %-22s %-16s\n" "VERSION" "CONTAINER" "STATUS" "HTTP" "ADMIN" "GRPC"
  printf "%-10s %-16s %-12s %-22s %-22s %-16s\n" \
    "${version}" \
    "${name}" \
    "${status}" \
    "http://127.0.0.1:${http_port}" \
    "http://127.0.0.1:${console_port}" \
    "127.0.0.1:${grpc_port}"
else
  echo "No current server config. Run: ./Tools/switch.sh <version>"
fi

echo
echo "All Nakama containers:"
containers="$(docker ps -a --filter "name=nakama" --format "table {{.Names}}\t{{.Image}}\t{{.Status}}\t{{.Ports}}")"
if [[ -z "${containers}" ]]; then
  echo "No Nakama containers."
else
  echo "${containers}"
fi
