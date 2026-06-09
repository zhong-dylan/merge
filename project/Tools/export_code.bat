@echo off
setlocal

set WORKSPACE=%~dp0..
set LUBAN_DLL=%WORKSPACE%\Luban\Luban.dll
set CONF_ROOT=%WORKSPACE%\Config\Luban
set CLIENT_CODE_DIR=%WORKSPACE%\Assets\Gen\Luban\Client
set SERVER_CODE_DIR=%WORKSPACE%\Server\Gen\LubanGo
set REMOTE_DATA_DIR=%WORKSPACE%\Assets\Addressables_Remote\Config\Bytes

echo Export client code and bytes...
set DOTNET_ROLL_FORWARD=Major
dotnet %LUBAN_DLL% ^
    -t client ^
    -c cs-bin ^
    -d bin ^
    --conf %CONF_ROOT%\luban.conf ^
    -x outputCodeDir=%CLIENT_CODE_DIR% ^
    -x outputDataDir=%REMOTE_DATA_DIR% ^
    -x pathValidator.rootDir=%WORKSPACE% ^
    -x l10n.provider=default ^
    -x l10n.textFile.path=*@%CONF_ROOT%\Datas\l10n\texts.json ^
    -x l10n.textFile.keyFieldName=key
if errorlevel 1 goto :end

echo Export server go code and bytes...
dotnet %LUBAN_DLL% ^
    -t server ^
    -c go-bin ^
    -d bin ^
    --conf %CONF_ROOT%\luban.conf ^
    -x outputCodeDir=%SERVER_CODE_DIR% ^
    -x outputDataDir=%REMOTE_DATA_DIR% ^
    -x pathValidator.rootDir=%WORKSPACE% ^
    -x l10n.provider=default ^
    -x l10n.textFile.path=*@%CONF_ROOT%\Datas\l10n\texts.json ^
    -x l10n.textFile.keyFieldName=key ^
    -x lubanGoModule=project/server/gen/luban

:end
pause
