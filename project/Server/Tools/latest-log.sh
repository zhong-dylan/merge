#!/usr/bin/env bash

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SERVER_DIR="$(cd "${SCRIPT_DIR}/.." && pwd)"
LOG_DIR="${SERVER_DIR}/logs"

if [[ ! -d "${LOG_DIR}" ]]; then
  echo "No logs directory found: ${LOG_DIR}"
  exit 1
fi

LATEST_LOG="$(find "${LOG_DIR}" -type f -name '*.log' | sort | tail -n 1)"

if [[ -z "${LATEST_LOG}" ]]; then
  echo "No log files found in ${LOG_DIR}"
  exit 1
fi

echo "${LATEST_LOG}"
