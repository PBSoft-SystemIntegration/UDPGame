@echo off
start "" "Server\UDPGameServer.exe"
timeout /t 2 /nobreak
start "" "GameClient\UDPGame.exe"
timeout /t 2 /nobreak
start "" "GameClient\UDPGame.exe"
