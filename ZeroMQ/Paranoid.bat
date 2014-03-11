echo off
REM ZeroMQ Paranoid Pirate pattern example


cd /d %~dp0
start "Broker" cmd /T:8F /k .\Broker\bin\debug\Broker.exe
start "Worker 1" cmd /T:8E /k .\Worker\bin\debug\Worker.exe
start "Worker 2" cmd /T:8E /k .\Worker\bin\debug\Worker.exe
start "Client 1" cmd /T:8E /k .\Client\bin\debug\Client.exe