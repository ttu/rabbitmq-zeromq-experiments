:: Responses fully implemented

start "Worker 1" cmd /T:0A /k .\Worker\bin\debug\Worker.exe rep
start "Worker 2" cmd /T:0A /k .\Worker\bin\debug\Worker.exe rep
start "Worker 3" cmd /T:0A /k .\Worker\bin\debug\Worker.exe rep

start "Broker" cmd /T:0E /k .\Broker\bin\debug\Broker.exe pull

start "Client 1" cmd /T:0C /k .\Client\bin\debug\Client.exe push
start "Client 2" cmd /T:0C /k .\Client\bin\debug\Client.exe push