#!/usr/bin/env bash

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SERVER_DIR="$(cd "${SCRIPT_DIR}/.." && pwd)"
RELEASES_DIR="${SERVER_DIR}/releases"

usage() {
  echo "Usage:"
  echo "  ./Tools/switch.sh <version>"
  echo ""
  echo "Optional env:"
  echo "  RELEASE_HOST=http://127.0.0.1"
  echo "  DATABASE_ADDRESS=local:localdb@postgres:5432/nakama"
  echo "  LOGGER_LEVEL=INFO"
  echo "  SESSION_TOKEN_EXPIRY_SEC=7200"
  echo "  SESSION_ENCRYPTION_KEY=local_session_encryption_key_change_me"
  echo "  SESSION_REFRESH_ENCRYPTION_KEY=local_refresh_encryption_key_change_me"
  echo "  SOCKET_SERVER_KEY=local_socket_server_key_change_me"
  echo "  CONSOLE_USERNAME=admin"
  echo "  CONSOLE_PASSWORD=password"
  echo "  RUNTIME_HTTP_KEY=local_runtime_http_key_change_me"
}

VERSION="${1:-}"
if [[ -z "${VERSION}" ]]; then
  usage >&2
  exit 1
fi

generated_env="$(
  /usr/bin/ruby -ruri -rshellwords -e '
    version = ARGV.fetch(0)
    release_number = Integer(version)
    raise "version must be positive" if release_number <= 0

    release_host = ENV.fetch("RELEASE_HOST", "http://127.0.0.1")
    release_uri = URI.parse(release_host)
    raise "RELEASE_HOST must include scheme and host" if release_uri.scheme.nil? || release_uri.host.nil?

    base_http_port = 7350
    port_step = 10
    http_port = base_http_port + ((release_number - 1) * port_step)
    grpc_port = http_port - 1
    admin_port = http_port + 1
    game_url = release_uri.dup
    game_url.port = http_port
    admin_url = release_uri.dup
    admin_url.port = admin_port

    defaults = {
      "LOGGER_LEVEL" => "INFO",
      "DATABASE_ADDRESS" => "local:localdb@postgres:5432/nakama",
      "SESSION_TOKEN_EXPIRY_SEC" => "7200",
      "SESSION_ENCRYPTION_KEY" => "local_session_encryption_key_change_me",
      "SESSION_REFRESH_ENCRYPTION_KEY" => "local_refresh_encryption_key_change_me",
      "SOCKET_SERVER_KEY" => "local_socket_server_key_change_me",
      "CONSOLE_USERNAME" => "admin",
      "CONSOLE_PASSWORD" => "password",
      "RUNTIME_HTTP_KEY" => "local_runtime_http_key_change_me"
    }

    exports = {
      "RELEASE_VERSION" => version,
      "NAKAMA_NAME" => "nakama#{version}",
      "NAKAMA_HTTP_PORT" => game_url.port.to_s,
      "NAKAMA_CONSOLE_PORT" => admin_url.port.to_s,
      "NAKAMA_GRPC_PORT" => grpc_port.to_s,
      "ADMIN_URL" => admin_url.to_s,
      "GAME_URL" => game_url.to_s,
      "RELEASE_HOST" => release_host
    }

    defaults.each do |key, default_value|
      value = ENV.fetch(key, default_value)
      raise "#{key} is empty" if value.strip.empty?
      exports[key] = value
    end

    exports.each do |key, value|
      puts "#{key}=#{Shellwords.escape(value)}"
    end
  ' "${VERSION}"
)" || {
  echo "Failed to switch server config to version ${VERSION}" >&2
  exit 1
}

eval "${generated_env}"

OUTPUT_PATH="${RELEASES_DIR}/nakama.yml"
COMPOSE_ENV_PATH="${RELEASES_DIR}/compose.env"
COMPOSE_PATH="${RELEASES_DIR}/docker-compose.yml"

mkdir -p "${RELEASES_DIR}"

cat > "${OUTPUT_PATH}" <<EOF
name: ${NAKAMA_NAME}

database:
  address:
    - "${DATABASE_ADDRESS}"

logger:
  level: ${LOGGER_LEVEL}

session:
  token_expiry_sec: ${SESSION_TOKEN_EXPIRY_SEC}
  encryption_key: "${SESSION_ENCRYPTION_KEY}"
  refresh_encryption_key: "${SESSION_REFRESH_ENCRYPTION_KEY}"

socket:
  server_key: "${SOCKET_SERVER_KEY}"
  port: ${NAKAMA_HTTP_PORT}

console:
  port: ${NAKAMA_CONSOLE_PORT}
  username: "${CONSOLE_USERNAME}"
  password: "${CONSOLE_PASSWORD}"

runtime:
  http_key: "${RUNTIME_HTTP_KEY}"
EOF

cat > "${COMPOSE_ENV_PATH}" <<EOF
RELEASE_VERSION=${RELEASE_VERSION}
NAKAMA_NAME=${NAKAMA_NAME}
DATABASE_ADDRESS=${DATABASE_ADDRESS}
NAKAMA_GRPC_PORT=${NAKAMA_GRPC_PORT}
NAKAMA_HTTP_PORT=${NAKAMA_HTTP_PORT}
NAKAMA_CONSOLE_PORT=${NAKAMA_CONSOLE_PORT}
EOF

cat > "${COMPOSE_PATH}" <<'EOF'
services:
  nakama:
    build:
      context: ..
      dockerfile: Dockerfile
      pull: false
    container_name: ${NAKAMA_NAME}
    restart: unless-stopped
    entrypoint:
      - "/bin/sh"
      - "-ecx"
      - >
        /nakama/nakama migrate up --database.address ${DATABASE_ADDRESS} &&
        exec /nakama/nakama
        --name ${NAKAMA_NAME}
        --database.address ${DATABASE_ADDRESS}
        --config /nakama/data/nakama.yml
    ports:
      - "${NAKAMA_GRPC_PORT}:${NAKAMA_GRPC_PORT}"
      - "${NAKAMA_HTTP_PORT}:${NAKAMA_HTTP_PORT}"
      - "${NAKAMA_CONSOLE_PORT}:${NAKAMA_CONSOLE_PORT}"
    volumes:
      - ./nakama.yml:/nakama/data/nakama.yml:ro
      - ../Config:/nakama/data/modules/runtime/Config:ro
    networks:
      - nakama_shared

networks:
  nakama_shared:
    external: true
    name: nakama_shared
EOF

echo "Switched current server config:"
echo "  ${OUTPUT_PATH}"
echo "  ${COMPOSE_ENV_PATH}"
echo "  ${COMPOSE_PATH}"
echo "Current server version: ${RELEASE_VERSION}"
echo "Game URL: ${GAME_URL}"
echo "Admin URL: ${ADMIN_URL}"
