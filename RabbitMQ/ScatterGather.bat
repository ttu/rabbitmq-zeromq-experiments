:: Should create work queue here. Workers crash if worker queue does not exist.

start "Client 1" cmd /T:0A /k .\Client\bin\debug\Client.exe s 10 15
start "Client 2" cmd /T:0B /k .\Client\bin\debug\Client.exe s 10 150
start "Client 3" cmd /T:0C /k .\Client\bin\debug\Client.exe s 10 150
start "Client 4" cmd /T:0D /k .\Client\bin\debug\Client.exe s 10 150

start "Worker 1" cmd /T:0E /k .\Worker\bin\debug\Worker.exe s
start "Worker 2" cmd /T:0E /k .\Worker\bin\debug\Worker.exe s
start "Worker 3" cmd /T:0E /k .\Worker\bin\debug\Worker.exe s
start "Worker 4" cmd /T:0E /k .\Worker\bin\debug\Worker.exe s
start "Worker 5" cmd /T:0E /k .\Worker\bin\debug\Worker.exe s