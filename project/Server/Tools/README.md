# Server Tools

These scripts manage the local Nakama server for one active version at a time.

`Server/releases` stores the current generated config only:
- `Server/releases/nakama.yml`
- `Server/releases/compose.env`
- `Server/releases/docker-compose.yml`

## Common Flow

```bash
cd Server
./Tools/switch.sh 101
./Tools/build.sh
./Tools/start.sh
```

This selects version `101` and builds its version-specific image explicitly. `start.sh` starts shared PostgreSQL and Nakama using the existing image, then streams logs. It never builds images.

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
  Shows shared PostgreSQL status, current server config status, and all Docker containers whose name contains `nakama`.

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

## Build from local Nakama source

`build.sh` compiles `Server/nakama` and the business plugin in Linux containers.
`start.sh` runs the resulting image without compiling or building it.
No host Go installation is required. Build the selected version before its first start.

```bash
cd Server
./Tools/switch.sh 103  # Select the server version
./Tools/build.sh       # Compile and build its image
./Tools/start.sh       # Start the existing image and stream logs
```

The builder uses Go 1.27.1, as required by the checked-in Nakama source. Both
Nakama and `backend.so` use `nakama/go.mod` and `nakama/vendor` dependencies to
keep Go plugin package versions consistent. If business code adds an external
dependency, add it to Nakama's dependency set and regenerate its vendor directory.
The compiled console UI in `nakama/console/ui/dist` is embedded in the server.
The first build needs access to Docker Hub and Debian package repositories.

## Deploy to a Linux server using Docker

By default `build.sh` builds **linux/amd64**, including on an ARM Mac. It checks
image architecture and the executable, checks runtime modules, and exports a
package under `Server/dist/nakama<version>-linux-amd64/` containing `image.tar.gz`,
configuration, and deployment scripts. The deployment Compose file uses the
prebuilt image and has no build step. Runtime Config files are included in the image.

```bash
cd Server
./Tools/build.sh
# Copy dist/nakama103-linux-amd64/ to the Linux x86_64 server.
# On that server, enter the copied directory:
./deploy.sh
```

The server needs Docker Engine and Docker Compose v2, but no Go compiler or source
checkout. `deploy.sh` loads the image and starts Nakama and shared PostgreSQL.
PostgreSQL's `postgres:15-alpine` image must already exist or be downloadable.
Review `releases/nakama.yml` and `releases/compose.env` for database settings and
credentials before deployment. Deployment starts the selected version without
running `switch.sh`; `start.sh` never compiles. Logs stream until Ctrl-C; containers
continue running in the background.

For an ARM64 target use `BUILD_PLATFORM=linux/arm64 ./Tools/build.sh`.

## Version-specific images

Each server version uses its own image, such as `nakama104-nakama:latest`.
The workflow is:

```bash
cd Server
./Tools/switch.sh 104  # Select configuration
./Tools/build.sh       # Compile and export the version-specific image
./Tools/start.sh       # Start the existing image without compiling
```

The exported deployment package references the same version-specific image.
