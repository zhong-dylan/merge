@echo off
setlocal

set WORKSPACE=%~dp0..
set SERVER_DIR=%WORKSPACE%\Server

cd /d %SERVER_DIR%
bash ./restart.sh
