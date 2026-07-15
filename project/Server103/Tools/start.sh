#!/usr/bin/env bash

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SERVER_DIR="$(cd "${SCRIPT_DIR}/.." && pwd)"
LOG_DIR="${SERVER_DIR}/logs"
RELEASES_DIR="${SERVER_DIR}/releases"
COMPOSE_FILE="${RELEASES_DIR}/docker-compose.yml"
COMPOSE_ENV="${RELEASES_DIR}/compose.env"
NAKAMA_VERSION="3.39.0"
NAKAMA_IMAGE="heroiclabs/nakama:${NAKAMA_VERSION}"
PLUGINBUILDER_IMAGE="heroiclabs/nakama-pluginbuilder:${NAKAMA_VERSION}"
VERSION_ARG="${1:-}"

cd "${SERVER_DIR}"

ensure_docker_running() {
  if docker info >/dev/null 2>&1; then
    return 0
  fi

  echo "Docker daemon is not running."

  if [[ "$(uname -s)" == "Darwin" ]]; then
    echo "Starting Docker Desktop..."
    open -a Docker >/dev/null 2>&1 || true
  fi

  echo "Waiting for Docker to become ready..."
  for _ in {1..60}; do
    if docker info >/dev/null 2>&1; then
      echo "Docker is ready."
      return 0
    fi
    sleep 2
  done

  echo "Docker did not become ready in time."
  exit 1
}

ensure_image_present() {
  local image="$1"

  if docker image inspect "${image}" >/dev/null 2>&1; then
    echo "Using local image: ${image}"
    return 0
  fi

  echo "Pulling missing image: ${image}"
  docker pull "${image}"
}

ensure_shared_postgres() {
  if ! docker network inspect nakama_shared >/dev/null 2>&1; then
    echo "Creating shared Docker network: nakama_shared"
    docker network create nakama_shared >/dev/null
  fi

  if docker inspect nakama-postgres >/dev/null 2>&1; then
    echo "Using existing shared PostgreSQL container."
    docker start nakama-postgres >/dev/null
    return 0
  fi

  echo "Creating shared PostgreSQL..."
  docker compose --project-name nakama-shared -f "${SERVER_DIR}/docker-compose.yml" up -d postgres
}

if [[ -n "${VERSION_ARG}" ]]; then
  "${SCRIPT_DIR}/switch.sh" "${VERSION_ARG}"
elif [[ ! -f "${COMPOSE_ENV}" || ! -f "${COMPOSE_FILE}" ]]; then
  echo "Usage: ./Tools/start.sh <version>"
  echo "Or switch current config first: ./Tools/switch.sh <version>"
  exit 1
fi

VERSION="$(grep '^RELEASE_VERSION=' "${COMPOSE_ENV}" | cut -d '=' -f 2-)"
if [[ -z "${VERSION}" ]]; then
  echo "Missing RELEASE_VERSION in ${COMPOSE_ENV}"
  exit 1
fi

ensure_docker_running

mkdir -p "${LOG_DIR}"
LOG_FILE="${LOG_DIR}/nakama-${VERSION}-$(date +%Y%m%d-%H%M%S).log"

echo "Checking Nakama base images..."
ensure_image_present "${NAKAMA_IMAGE}"
ensure_image_present "${PLUGINBUILDER_IMAGE}"

echo "Starting shared PostgreSQL..."
ensure_shared_postgres

echo "Building Nakama ${VERSION} image with local Docker cache..."
DOCKER_BUILDKIT=0 COMPOSE_DOCKER_CLI_BUILD=0 docker compose --project-name "nakama${VERSION}" --env-file "${COMPOSE_ENV}" -f "${COMPOSE_FILE}" build --pull=false nakama

echo "Starting Nakama ${VERSION}..."
docker compose --project-name "nakama${VERSION}" --env-file "${COMPOSE_ENV}" -f "${COMPOSE_FILE}" up -d --no-build nakama

echo "Services are up:"
postgres_status="$(docker inspect -f '{{.State.Status}}' nakama-postgres 2>/dev/null || true)"
echo "Shared PostgreSQL: ${postgres_status:-not-created}"
docker compose --project-name "nakama${VERSION}" --env-file "${COMPOSE_ENV}" -f "${COMPOSE_FILE}" ps

console_port="$(grep '^NAKAMA_CONSOLE_PORT=' "${COMPOSE_ENV}" | cut -d '=' -f 2-)"
socket_port="$(grep '^NAKAMA_HTTP_PORT=' "${COMPOSE_ENV}" | cut -d '=' -f 2-)"
grpc_port="$(grep '^NAKAMA_GRPC_PORT=' "${COMPOSE_ENV}" | cut -d '=' -f 2-)"

echo "Nakama ${VERSION} Console:"
echo "  URL: http://127.0.0.1:${console_port}"
echo "Game Server:"
echo "  gRPC: 127.0.0.1:${grpc_port}"
echo "  HTTP: http://127.0.0.1:${socket_port}"

echo "Streaming Nakama ${VERSION} logs..."
echo "Log file: ${LOG_FILE}"
docker compose --project-name "nakama${VERSION}" --env-file "${COMPOSE_ENV}" -f "${COMPOSE_FILE}" logs -f nakama | tee -a "${LOG_FILE}"
