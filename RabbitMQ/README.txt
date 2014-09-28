RabbitMQ .NET Client & API documentation:
https://www.rabbitmq.com/releases/rabbitmq-dotnet-client/

----------------------

Install Erlang & RabbitMQ:
http://www.erlang.org/download.html
https://www.rabbitmq.com/install-windows.html

Enable management plugin:

e.g.
cd C:\Program Files\RabbitMQ Server\rabbitmq_server-2.7.0\sbin
cd C:\Program Files (x86)\RabbitMQ Server\rabbitmq_server-3.2.4\sbin

rabbitmq-plugins.bat enable rabbitmq_management

rabbitmq-service stop
rabbitmq-service remove
rabbitmq-service install
rabbitmq-service start

OR

Use Vagrant from Scratchpad

----------------------

Manager default url (prior 3.0)
http://localhost:55672/#/
After 3.0
http://localhost:15672

username: guest
password: guest

----------------------

Start more Clients and Workers from Visual Studio:
Debug -> Start new instance

Other links:
http://www.codeproject.com/Articles/309786/Rabbit-Mq-Shovel-Example

----------------------

PubSub:
Publisher sends message to all workers (Exchange fanout).
http://www.rabbitmq.com/tutorials/tutorial-three-dotnet.html

Shared Worker:
Producers send work to same queue and multiple workers process work from that queue using fair dispatch and message acknowledgment.
https://www.rabbitmq.com/tutorials/tutorial-two-dotnet.html

Producers create a callback queue where workers send response.
https://www.rabbitmq.com/tutorials/tutorial-six-dotnet.html

Scatter Gather:
http://www.eaipatterns.com/BroadcastAggregate.html
Sends message to all workers and then aggreates responses back.

Implementation in this project has only 1 work queue, but Producer could send messages through Exchange.
http://geekswithblogs.net/michaelstephenson/archive/2012/08/06/150373.aspx

Implementation also uses fair dispatch, so message is sent only to 1 worker and messages are acknowledged.

Client specific workers:
Is an implementaion based on Scatter Gather with exchange.

Each Client has own queue and messges from Producers go through exchange. Routing key is ClientId.
ClientRegister could also work through RabbitMQ.

----------------------

xml files are Draw.io diagrams
https://www.draw.io/