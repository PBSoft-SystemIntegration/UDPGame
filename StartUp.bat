@echo off
pushd "%~dp0"

dotnet build UDPGame.sln
if errorlevel 1 (
    pause
    exit /b 1
)

start "UDP Game Server" /D "%~dp0UDPGameServer\bin\Debug\net10.0" dotnet UDPGameServer.dll
timeout /t 1 /nobreak > nul
start "UDP Game Client 1" /D "%~dp0UDPGame\bin\Debug\net10.0" UDPGame.exe
timeout /t 1 /nobreak > nul
start "UDP Game Client 2" /D "%~dp0UDPGame\bin\Debug\net10.0" UDPGame.exe

popd
