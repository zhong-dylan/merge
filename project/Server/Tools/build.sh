#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SERVER_DIR="$(cd "${SCRIPT_DIR}/.." && pwd)"
COMPOSE_ENV="${SERVER_DIR}/releases/compose.env"
COMPOSE_FILE="${SERVER_DIR}/releases/docker-compose.yml"

if [[ ! -f "${COMPOSE_ENV}" || ! -f "${COMPOSE_FILE}" ]]; then
  echo "Select a server first: ./Tools/switch.sh <version>" >&2
  exit 1
fi
if [[ ! -f "${SERVER_DIR}/nakama/go.mod" ]]; then
  echo "Missing Nakama source: ${SERVER_DIR}/nakama" >&2
  exit 1
fi
VERSION="$(sed -n 's/^RELEASE_VERSION=//p' "${COMPOSE_ENV}")"
if [[ ! "${VERSION}" =~ ^[0-9]+$ || "${VERSION}" -le 0 ]]; then
  echo "Invalid RELEASE_VERSION in ${COMPOSE_ENV}" >&2
  exit 1
fi
PLATFORM="${BUILD_PLATFORM:-linux/amd64}"
case "${PLATFORM}" in
  linux/amd64|linux/arm64) ;;
  *) echo "BUILD_PLATFORM must be linux/amd64 or linux/arm64" >&2; exit 1 ;;
esac
IMAGE="nakama${VERSION}-nakama:latest"
PACKAGE_DIR="${SERVER_DIR}/dist/nakama${VERSION}-${PLATFORM//\//-}"

echo "Compiling local Nakama and backend.so for ${PLATFORM}..."
docker buildx build --platform "${PLATFORM}" --load --progress plain \
  -t "${IMAGE}" -f "${SERVER_DIR}/Dockerfile" "${SERVER_DIR}"
# Confirm the exported image architecture and executable before packaging.
actual_platform="$(docker image inspect -f '{{.Os}}/{{.Architecture}}' "${IMAGE}")"
[[ "${actual_platform}" == "${PLATFORM}" ]] || { echo "Unexpected platform: ${actual_platform}" >&2; exit 1; }
docker run --rm --platform "${PLATFORM}" --entrypoint /nakama/nakama "${IMAGE}" --version
docker run --rm --platform "${PLATFORM}" --entrypoint /nakama/nakama "${IMAGE}" \
  check --runtime.path /nakama/data/modules

mkdir -p "${PACKAGE_DIR}/releases" "${PACKAGE_DIR}/Tools"
cp "${COMPOSE_ENV}" "${PACKAGE_DIR}/releases/compose.env"
cp "${SERVER_DIR}/releases/nakama.yml" "${PACKAGE_DIR}/releases/nakama.yml"
# Deployment compose is image-only; runtime Config is already inside the image.
sed '/    build:/,+3d; /      - ..\/Config:/d' "${COMPOSE_FILE}" > "${PACKAGE_DIR}/releases/docker-compose.yml"
cp "${SERVER_DIR}/docker-compose.yml" "${PACKAGE_DIR}/docker-compose.yml"
cp "${SCRIPT_DIR}/start.sh" "${PACKAGE_DIR}/Tools/start.sh"
cp "${SCRIPT_DIR}/deploy.sh" "${PACKAGE_DIR}/deploy.sh"
chmod +x "${PACKAGE_DIR}/deploy.sh" "${PACKAGE_DIR}/Tools/start.sh"
docker save "${IMAGE}" | gzip > "${PACKAGE_DIR}/image.tar.gz.tmp"
mv "${PACKAGE_DIR}/image.tar.gz.tmp" "${PACKAGE_DIR}/image.tar.gz"
echo "Deployment package: ${PACKAGE_DIR}"
echo "Copy this directory to the server, then run: ./deploy.sh"
