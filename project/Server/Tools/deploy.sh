#!/usr/bin/env bash
set -euo pipefail
PACKAGE_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "${PACKAGE_DIR}"
if [[ ! -f image.tar.gz || ! -f releases/compose.env ]]; then
  echo "Run deploy.sh from the exported deployment package, not Server/Tools." >&2
  exit 1
fi
docker info >/dev/null
docker compose version >/dev/null
gzip -dc image.tar.gz | docker load
# No source compilation or image build occurs here.
exec ./Tools/start.sh
