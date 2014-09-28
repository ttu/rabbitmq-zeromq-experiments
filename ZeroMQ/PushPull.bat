start "Worker 1" cmd /T:0A /k .\Worker\bin\debug\Worker.exe pull
start "Worker 2" cmd /T:0A /k .\Worker\bin\debug\Worker.exe pull
start "Client 1" cmd /T:0C /k .\Client\bin\debug\Client.exe push