#!/usr/bin/env bash
set -euo pipefail

WORKSPACE="$(cd "$(dirname "$0")/.." && pwd)"
LUBAN_DLL="$WORKSPACE/Luban/Luban.dll"
CONF_ROOT="$WORKSPACE/Config/Luban"
CLIENT_CODE_DIR="$WORKSPACE/Assets/Gen/Luban/Json"
SERVER_CODE_DIR="$WORKSPACE/Server/Gen/LubanGoJson"
CLIENT_DATA_DIR="$WORKSPACE/Assets/Addressables_Remote/Config/Json"
SERVER_DATA_DIR="$WORKSPACE/Server/Config/Json"
export DOTNET_ROLL_FORWARD=Major

echo "Export client json code and data..."
dotnet "$LUBAN_DLL" \
    -t client \
    -c cs-simple-json \
    -d json \
    --conf "$CONF_ROOT/luban.conf" \
    -x outputCodeDir="$CLIENT_CODE_DIR" \
    -x outputDataDir="$CLIENT_DATA_DIR" \
    -x pathValidator.rootDir="$WORKSPACE" \
    -x l10n.provider=default \
    -x l10n.textFile.path="*@$CONF_ROOT/Datas/l10n/texts.json" \
    -x l10n.textFile.keyFieldName=key

echo "Export server go json code and data..."
dotnet "$LUBAN_DLL" \
    -t server \
    -c go-json \
    -d json \
    --conf "$CONF_ROOT/luban.conf" \
    -x outputCodeDir="$SERVER_CODE_DIR" \
    -x outputDataDir="$SERVER_DATA_DIR" \
    -x pathValidator.rootDir="$WORKSPACE" \
    -x l10n.provider=default \
    -x l10n.textFile.path="*@$CONF_ROOT/Datas/l10n/texts.json" \
    -x l10n.textFile.keyFieldName=key \
    -x lubanGoModule=project/server/gen/luban
