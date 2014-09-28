start "Client 1" cmd /T:0D /k .\Client\bin\debug\Client.exe p

start "Worker 1" cmd /T:0E /k .\Worker\bin\debug\Worker.exe p
start "Worker 2" cmd /T:0E /k .\Worker\bin\debug\Worker.exe p