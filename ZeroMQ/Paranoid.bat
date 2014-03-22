echo off
REM ZeroMQ Paranoid Pirate pattern example


cd /d %~dp0
start "Broker" cmd /T:0E /k .\Broker\bin\debug\Broker.exe
start "Worker 1" cmd /T:0A /k .\Worker\bin\debug\Worker.exe
start "Worker 2" cmd /T:0A /k .\Worker\bin\debug\Worker.exe
start "Client 1" cmd /T:0C /k .\Client\bin\debug\Client.exe
start "Client 2" cmd /T:0C /k .\Client\bin\debug\Client.exe