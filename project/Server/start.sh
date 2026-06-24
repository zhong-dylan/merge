#!/usr/bin/env bash

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
LOG_DIR="${SCRIPT_DIR}/logs"
LOG_FILE="${LOG_DIR}/nakama-$(date +%Y%m%d-%H%M%S).log"
NAKAMA_VERSION="3.39.0"
NAKAMA_IMAGE="heroiclabs/nakama:${NAKAMA_VERSION}"
PLUGINBUILDER_IMAGE="heroiclabs/nakama-pluginbuilder:${NAKAMA_VERSION}"
SERVERCONFIG_JSON_FILE="${SERVERCONFIG_JSON_FILE:-${SCRIPT_DIR}/Config/Json/global_tbconfig.json}"
SERVERCONFIG_COMPOSE_ENV="${SCRIPT_DIR}/serverconfig/compose.env"
cd "${SCRIPT_DIR}"

docker_compose() {
  docker compose --env-file "${SERVERCONFIG_COMPOSE_ENV}" "$@"
}

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
  echo "Please start Docker Desktop (or your Docker service) and retry:"
  echo "  cd ${SCRIPT_DIR}"
  echo "  ./start.sh"
  exit 1
}

ensure_docker_running

mkdir -p "${LOG_DIR}"

echo "Generating server config from ${SERVERCONFIG_JSON_FILE}..."
"${SCRIPT_DIR}/serverconfig/generate.sh" "${SERVERCONFIG_JSON_FILE}"

ensure_image_present() {
  local image="$1"

  if docker image inspect "${image}" >/dev/null 2>&1; then
    echo "Using local image: ${image}"
    return 0
  fi

  echo "Pulling missing image: ${image}"
  docker pull "${image}"
}

echo "Checking Nakama base images..."
ensure_image_present "${NAKAMA_IMAGE}"
ensure_image_present "${PLUGINBUILDER_IMAGE}"

echo "Starting PostgreSQL..."
docker_compose up -d postgres

echo "Building Nakama image with local Docker cache..."
DOCKER_BUILDKIT=0 COMPOSE_DOCKER_CLI_BUILD=0 docker_compose build --pull=false nakama

echo "Starting Nakama..."
docker_compose up -d --no-build nakama

echo "Services are up:"
docker_compose ps

console_port="$(awk '
  /^console:/ { in_console=1; next }
  in_console && /^  port:/ { gsub(/ /, "", $2); print $2; exit }
' "${SCRIPT_DIR}/nakama.yml")"
socket_port="$(awk '
  /^socket:/ { in_socket=1; next }
  /^console:/ { in_socket=0 }
  in_socket && /^  port:/ { gsub(/ /, "", $2); print $2; exit }
' "${SCRIPT_DIR}/nakama.yml")"
console_username="$(awk '
  /^console:/ { in_console=1; next }
  in_console && /^  username:/ { gsub(/"/, "", $2); print $2; exit }
' "${SCRIPT_DIR}/nakama.yml")"
console_password="$(awk '
  /^console:/ { in_console=1; next }
  in_console && /^  password:/ { gsub(/"/, "", $2); print $2; exit }
' "${SCRIPT_DIR}/nakama.yml")"
grpc_port="$(grep '^NAKAMA_GRPC_PORT=' "${SERVERCONFIG_COMPOSE_ENV}" | cut -d '=' -f 2)"

echo "Nakama Console:"
echo "  URL: http://127.0.0.1:${console_port}"
echo "  Username: ${console_username}"
echo "  Password: ${console_password}"
echo "Game Server:"
echo "  gRPC: 127.0.0.1:${grpc_port}"
echo "  HTTP: http://127.0.0.1:${socket_port}"

echo "Streaming Nakama logs..."
echo "Log file: ${LOG_FILE}"
docker_compose logs -f nakama | tee -a "${LOG_FILE}"
