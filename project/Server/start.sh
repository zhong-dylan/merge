#!/usr/bin/env bash

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
LOG_DIR="${SCRIPT_DIR}/logs"
LOG_FILE="${LOG_DIR}/nakama-$(date +%Y%m%d-%H%M%S).log"
NAKAMA_VERSION="3.39.0"
NAKAMA_IMAGE="heroiclabs/nakama:${NAKAMA_VERSION}"
PLUGINBUILDER_IMAGE="heroiclabs/nakama-pluginbuilder:${NAKAMA_VERSION}"
cd "${SCRIPT_DIR}"

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
docker compose up -d postgres

echo "Building Nakama image with local Docker cache..."
DOCKER_BUILDKIT=0 COMPOSE_DOCKER_CLI_BUILD=0 docker compose build --pull=false nakama

echo "Starting Nakama..."
docker compose up -d --no-build nakama

echo "Services are up:"
docker compose ps

echo "Nakama Console:"
echo "  URL: http://127.0.0.1:7351"
echo "  Username: admin"
echo "  Password: password"
echo "Game Server:"
echo "  gRPC: 127.0.0.1:7349"
echo "  HTTP: http://127.0.0.1:7350"

echo "Streaming Nakama logs..."
echo "Log file: ${LOG_FILE}"
docker compose logs -f nakama | tee -a "${LOG_FILE}"
