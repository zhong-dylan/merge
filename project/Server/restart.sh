#!/usr/bin/env bash

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

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
  echo "  ./restart.sh"
  exit 1
}

ensure_docker_running

echo "Restarting Nakama and PostgreSQL containers..."
cd "${SCRIPT_DIR}"
docker compose down
"${SCRIPT_DIR}/start.sh"
