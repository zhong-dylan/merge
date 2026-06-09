@echo off
setlocal

set WORKSPACE=%~dp0..
set LUBAN_DLL=%WORKSPACE%\Luban\Luban.dll
set CONF_ROOT=%WORKSPACE%\Config\Luban
set CLIENT_CODE_DIR=%WORKSPACE%\Assets\Gen\Luban\Json
set SERVER_CODE_DIR=%WORKSPACE%\Server\Gen\LubanGoJson
set CLIENT_DATA_DIR=%WORKSPACE%\Assets\Addressables_Remote\Config\Json
set SERVER_DATA_DIR=%WORKSPACE%\Server\Config\Json
set DOTNET_ROLL_FORWARD=Major

echo Export client json code and data...
dotnet %LUBAN_DLL% ^
    -t client ^
    -c cs-simple-json ^
    -d json ^
    --conf %CONF_ROOT%\luban.conf ^
    -x outputCodeDir=%CLIENT_CODE_DIR% ^
    -x outputDataDir=%CLIENT_DATA_DIR% ^
    -x pathValidator.rootDir=%WORKSPACE% ^
    -x l10n.provider=default ^
    -x l10n.textFile.path=*@%CONF_ROOT%\Datas\l10n\texts.json ^
    -x l10n.textFile.keyFieldName=key
if errorlevel 1 goto :end

echo Export server go json code and data...
dotnet %LUBAN_DLL% ^
    -t server ^
    -c go-json ^
    -d json ^
    --conf %CONF_ROOT%\luban.conf ^
    -x outputCodeDir=%SERVER_CODE_DIR% ^
    -x outputDataDir=%SERVER_DATA_DIR% ^
    -x pathValidator.rootDir=%WORKSPACE% ^
    -x l10n.provider=default ^
    -x l10n.textFile.path=*@%CONF_ROOT%\Datas\l10n\texts.json ^
    -x l10n.textFile.keyFieldName=key ^
    -x lubanGoModule=project/server/gen/luban

:end
pause
