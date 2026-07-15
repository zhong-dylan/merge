# serverconfig

Runtime server scripts have moved to `Server/Tools`.

See `Server/Tools/README.md` for script usage and environment variables.

`Server/releases` now stores only the current generated config:
- `Server/releases/nakama.yml`
- `Server/releases/compose.env`
- `Server/releases/docker-compose.yml`

## Usage

```bash
cd Server
./Tools/switch.sh 101
./Tools/start.sh
```

Or switch and start in one command:

```bash
cd Server
./Tools/start.sh 101
```

Common commands:

```bash
./Tools/stop.sh
./Tools/restart.sh
./Tools/logs.sh
./Tools/list.sh
```

Override host or keys with environment variables when switching:

```bash
RELEASE_HOST=http://10.0.0.8 SOCKET_SERVER_KEY=xxx ./Tools/switch.sh 101
```

## Notes

- `switch.sh <version>` overwrites the current files under `Server/releases` and does not stop any running server.
- The version argument is the current release label/version marker.
- `RELEASE_HOST` controls the generated server host/ip. Default: `http://127.0.0.1`.
- PostgreSQL is shared through the `nakama_shared` Docker network.
- Server settings are read from environment variables:
  - `RELEASE_HOST`
  - `SOCKET_SERVER_KEY`
  - `CONSOLE_USERNAME`
  - `CONSOLE_PASSWORD`
  - `SESSION_ENCRYPTION_KEY`
  - `SESSION_REFRESH_ENCRYPTION_KEY`
  - `RUNTIME_HTTP_KEY`
  - `DATABASE_ADDRESS`
  - `LOGGER_LEVEL`
  - `SESSION_TOKEN_EXPIRY_SEC`

- Ports are auto-derived from `<version>`:
  - HTTP: `7350 + (version - 1) * 10`
  - gRPC: `HTTP - 1`
  - Admin: `HTTP + 1`
