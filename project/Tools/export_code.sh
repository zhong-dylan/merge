#!/usr/bin/env bash
set -euo pipefail

WORKSPACE="$(cd "$(dirname "$0")/.." && pwd)"
LUBAN_DLL="$WORKSPACE/Luban/Luban.dll"
CONF_ROOT="$WORKSPACE/Config/Luban"
CLIENT_CODE_DIR="$WORKSPACE/Assets/Gen/Luban/Client"
SERVER_CODE_DIR="$WORKSPACE/Server/Gen/LubanGo"
REMOTE_DATA_DIR="$WORKSPACE/Assets/Addressables_Remote/Config/Bytes"
export DOTNET_ROLL_FORWARD=Major

echo "Export client code and bytes..."
dotnet "$LUBAN_DLL" \
    -t client \
    -c cs-bin \
    -d bin \
    --conf "$CONF_ROOT/luban.conf" \
    -x outputCodeDir="$CLIENT_CODE_DIR" \
    -x outputDataDir="$REMOTE_DATA_DIR" \
    -x pathValidator.rootDir="$WORKSPACE" \
    -x l10n.provider=default \
    -x l10n.textFile.path="*@$CONF_ROOT/Datas/l10n/texts.json" \
    -x l10n.textFile.keyFieldName=key

echo "Export server go code and bytes..."
dotnet "$LUBAN_DLL" \
    -t server \
    -c go-bin \
    -d bin \
    --conf "$CONF_ROOT/luban.conf" \
    -x outputCodeDir="$SERVER_CODE_DIR" \
    -x outputDataDir="$REMOTE_DATA_DIR" \
    -x pathValidator.rootDir="$WORKSPACE" \
    -x l10n.provider=default \
    -x l10n.textFile.path="*@$CONF_ROOT/Datas/l10n/texts.json" \
    -x l10n.textFile.keyFieldName=key \
    -x lubanGoModule=project/server/gen/luban
