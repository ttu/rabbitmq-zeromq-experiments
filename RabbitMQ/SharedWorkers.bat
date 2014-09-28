start "Client 1" cmd /T:0C /k .\Client\bin\debug\Client.exe sw
start "Client 2" cmd /T:0D /k .\Client\bin\debug\Client.exe sw
start "Client 3" cmd /T:0D /k .\Client\bin\debug\Client.exe sw

start "Worker 1" cmd /T:0E /k .\Worker\bin\debug\Worker.exe sw
start "Worker 2" cmd /T:0E /k .\Worker\bin\debug\Worker.exe sw