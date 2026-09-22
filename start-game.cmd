@echo off
setlocal
cd /d "%~dp0"
set "ASTER_NODE=node"
if exist "%USERPROFILE%\.cache\codex-runtimes\codex-primary-runtime\dependencies\node\bin\node.exe" set "ASTER_NODE=%USERPROFILE%\.cache\codex-runtimes\codex-primary-runtime\dependencies\node\bin\node.exe"
if not defined PORT set "PORT=4173"
echo newASTER - http://127.0.0.1:%PORT%
echo Keep this window open while playing. Ctrl+C stops the server.
"%ASTER_NODE%" server.mjs
if errorlevel 1 pause
