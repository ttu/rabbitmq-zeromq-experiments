echo off
REM ZeroMQ Paranoid Pirate pattern example

cd /d %~dp0
start "Broker" cmd /T:0E /k .\Broker\bin\debug\Broker.exe para
start "Worker 1" cmd /T:0A /k .\Worker\bin\debug\Worker.exe para
start "Worker 2" cmd /T:0A /k .\Worker\bin\debug\Worker.exe para
start "Worker 3" cmd /T:0A /k .\Worker\bin\debug\Worker.exe para
start "Client 1" cmd /T:0C /k .\Client\bin\debug\Client.exe para
start "Client 2" cmd /T:0C /k .\Client\bin\debug\Client.exe para