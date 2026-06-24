#!/usr/bin/env bash

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SERVER_DIR="$(cd "${SCRIPT_DIR}/.." && pwd)"
CONFIG_JSON="${1:-${SERVER_DIR}/Config/Json/global_tbconfig.json}"
TEMPLATE_PATH="${SCRIPT_DIR}/nakama.yml.template"
OUTPUT_PATH="${SERVER_DIR}/nakama.yml"
COMPOSE_ENV_PATH="${SCRIPT_DIR}/compose.env"

if [[ ! -f "${CONFIG_JSON}" ]]; then
  echo "Global config json not found: ${CONFIG_JSON}" >&2
  exit 1
fi

generated_env="$(
  /usr/bin/ruby -rjson -ruri -rshellwords -e '
    data = JSON.parse(File.read(ARGV[0]))
    map = data.each_with_object({}) { |row, acc| acc[row.fetch("key")] = row.fetch("value") }
    version = map.fetch("release_version")
    release_number = Integer(version)
    raise "release_version must be positive" if release_number <= 0
    release_host = map.fetch("release_host")
    release_uri = URI.parse(release_host)
    raise "release_host must include scheme and host" if release_uri.scheme.nil? || release_uri.host.nil?

    def required(map, key)
      value = map[key]
      raise KeyError, key if value.nil? || value.strip.empty?
      value
    end

    base_http_port = 7350
    port_step = 10
    http_port = base_http_port + ((release_number - 1) * port_step)
    grpc_port = http_port - 1
    admin_port = http_port + 1
    game_url = release_uri.dup
    game_url.port = http_port
    admin_url = release_uri.dup
    admin_url.port = admin_port

    exports = {
      "RELEASE_VERSION" => version,
      "NAKAMA_NAME" => "nakama#{version}",
      "LOGGER_LEVEL" => map.fetch("logger_level", "INFO"),
      "DATABASE_ADDRESS" => map.fetch("database_address", "local:localdb@postgres:5432/nakama"),
      "SESSION_TOKEN_EXPIRY_SEC" => map.fetch("session_token_expiry_sec", "7200"),
      "SESSION_ENCRYPTION_KEY" => required(map, "session_encryption_key"),
      "SESSION_REFRESH_ENCRYPTION_KEY" => required(map, "session_refresh_encryption_key"),
      "SOCKET_SERVER_KEY" => required(map, "server_key"),
      "NAKAMA_HTTP_PORT" => game_url.port.to_s,
      "NAKAMA_CONSOLE_PORT" => admin_url.port.to_s,
      "NAKAMA_GRPC_PORT" => grpc_port.to_s,
      "CONSOLE_USERNAME" => required(map, "console_username"),
      "CONSOLE_PASSWORD" => required(map, "console_password"),
      "RUNTIME_HTTP_KEY" => required(map, "runtime_http_key"),
      "ADMIN_URL" => admin_url.to_s,
      "GAME_URL" => game_url.to_s,
      "RELEASE_HOST" => release_host
    }

    exports.each do |key, value|
      puts "#{key}=#{Shellwords.escape(value)}"
    end
  ' "${CONFIG_JSON}"
)" || {
  echo "Failed to generate server config from ${CONFIG_JSON}" >&2
  exit 1
}

eval "${generated_env}"

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
NAKAMA_NAME=${NAKAMA_NAME}
DATABASE_ADDRESS=${DATABASE_ADDRESS}
NAKAMA_GRPC_PORT=${NAKAMA_GRPC_PORT}
NAKAMA_HTTP_PORT=${NAKAMA_HTTP_PORT}
NAKAMA_CONSOLE_PORT=${NAKAMA_CONSOLE_PORT}
EOF

echo "Generated:"
echo "  ${OUTPUT_PATH}"
echo "  ${COMPOSE_ENV_PATH}"
echo "Active release version: ${RELEASE_VERSION}"
echo "Game URL: ${GAME_URL}"
echo "Admin URL: ${ADMIN_URL}"
