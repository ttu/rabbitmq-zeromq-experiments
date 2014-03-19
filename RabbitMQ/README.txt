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

Manager default url (prior 3.0)
http://localhost:55672/#/
After 3.0
http://localhost:15672

username: guest
password: guest

Start more Clients and Workers from Visual Studio:
Debug -> Start new instance

Other links:
http://www.codeproject.com/Articles/309786/Rabbit-Mq-Shovel-Example

----------------------

Scatter Gather:
http://www.eaipatterns.com/BroadcastAggregate.html

Implementation in this project has only 1 work queue, but Producer could send messages through Exchange.
http://geekswithblogs.net/michaelstephenson/archive/2012/08/06/150373.aspx

Client specific workers is an implementaion based on Scatter Gather with exchange.

----------------------

xml files are Draw.io diagrams
https://www.draw.io/