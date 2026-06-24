# serverconfig

`serverconfig` generates release-facing server config from `global.config` exported json.

## Files

- `nakama.yml.template`
  Template used to generate `Server/nakama.yml`.
- `generate.sh`
  Reads `Server/Config/Json/global_tbconfig.json`, resolves `release_version`, and generates:
  - `Server/nakama.yml`
  - `Server/serverconfig/compose.env`

## Usage

```bash
cd Server/serverconfig
./generate.sh
```

Or use a specific exported global config json:

```bash
./generate.sh /absolute/path/to/global_tbconfig.json
```

## Notes

- `Server/nakama.yml` is generated. Do not hand-edit it for release changes.
- `release_version` is kept as the current release label/version marker.
- `release_host` controls the release server host/ip.
- Runtime keys are fixed names:
  - `release_host`
  - `server_key`
  - `console_username`
  - `console_password`
  - `session_encryption_key`
  - `session_refresh_encryption_key`
  - `runtime_http_key`
  - `database_address`
  - `logger_level`
  - `session_token_expiry_sec`

- Ports are auto-derived from `release_version`:
  - HTTP: `7350 + (release_version - 1) * 10`
  - gRPC: `HTTP - 1`
  - Admin: `HTTP + 1`
