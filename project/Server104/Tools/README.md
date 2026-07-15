# Server Tools

These scripts manage the local Nakama server for one active version at a time.

`Server/releases` stores the current generated config only:
- `Server/releases/nakama.yml`
- `Server/releases/compose.env`
- `Server/releases/docker-compose.yml`

## Common Flow

```bash
cd Server
./Tools/start.sh 101
```

This switches the current config to version `101`, starts shared PostgreSQL, builds Nakama, starts Nakama, and streams logs.

To switch the current config without stopping or starting servers:

```bash
cd Server
./Tools/switch.sh 101
```

To start the already selected version:

```bash
cd Server
./Tools/start.sh
```

## Scripts

- `switch.sh <version>`
  Switches current config files under `Server/releases` to the input version. It does not stop any running server.

- `start.sh [version]`
  Starts the server. If `version` is provided, it switches to that version first. Without a version, it starts the current config.
  Shared PostgreSQL is reused by container name `nakama-postgres`; if it already exists, the script starts it instead of creating another one.

- `stop.sh [version]`
  Stops the current server by default. If a version is provided and it is not the current config, it force-removes the matching `nakama<version>` container.

- `stop.sh --db [version]`
  Stops Nakama and the shared PostgreSQL container. It does not remove the shared PostgreSQL volume.

- `restart.sh [version]`
  Stops then starts the server. If `version` is provided, it switches to that version before starting.

- `logs.sh`
  Streams logs for the current server config and saves them under `Server/logs`.

- `list.sh`
  Shows shared PostgreSQL status, current server config status, and matching Nakama containers.

- `latest-log.sh`
  Prints the newest log file path under `Server/logs`.

## Shared Resources

- PostgreSQL container: `nakama-postgres`
- PostgreSQL volume: `nakama_postgres_data`
- Docker network: `nakama_shared`

## Environment

These optional environment variables are read when switching config:

- `RELEASE_HOST`, default `http://127.0.0.1`
- `DATABASE_ADDRESS`, default `local:localdb@postgres:5432/nakama`
- `LOGGER_LEVEL`, default `INFO`
- `SESSION_TOKEN_EXPIRY_SEC`, default `7200`
- `SESSION_ENCRYPTION_KEY`, default `local_session_encryption_key_change_me`
- `SESSION_REFRESH_ENCRYPTION_KEY`, default `local_refresh_encryption_key_change_me`
- `SOCKET_SERVER_KEY`, default `local_socket_server_key_change_me`
- `CONSOLE_USERNAME`, default `admin`
- `CONSOLE_PASSWORD`, default `password`
- `RUNTIME_HTTP_KEY`, default `local_runtime_http_key_change_me`

Example:

```bash
cd Server
RELEASE_HOST=http://10.0.0.8 SOCKET_SERVER_KEY=xxx ./Tools/switch.sh 101
```

## Ports

Ports are derived from the version number:

- HTTP: `7350 + (version - 1) * 10`
- gRPC: `HTTP - 1`
- Console: `HTTP + 1`

For version `101`:

- gRPC: `8349`
- HTTP: `8350`
- Console: `8351`
