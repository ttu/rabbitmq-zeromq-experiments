start "Worker 1" cmd /T:8E /k .\Worker\bin\debug\Worker.exe s
start "Worker 2" cmd /T:8E /k .\Worker\bin\debug\Worker.exe s
start "Worker 3" cmd /T:8E /k .\Worker\bin\debug\Worker.exe s
start "Worker 4" cmd /T:8E /k .\Worker\bin\debug\Worker.exe s
start "Worker 5" cmd /T:8E /k .\Worker\bin\debug\Worker.exe s

start "Client 1" cmd /T:8E /k .\Client\bin\debug\Client.exe s 20 15
start "Client 1" cmd /T:8E /k .\Client\bin\debug\Client.exe s 10 150
start "Client 1" cmd /T:8E /k .\Client\bin\debug\Client.exe s 10 150
start "Client 1" cmd /T:8E /k .\Client\bin\debug\Client.exe s 10 150