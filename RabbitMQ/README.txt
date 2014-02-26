Install Erlang & RabbitMQ:
http://www.erlang.org/download.html
https://www.rabbitmq.com/install-windows.html

Enable management plugin:

cd C:\Program Files\RabbitMQ Server\rabbitmq_server-2.7.0\sbin

rabbitmq-plugins.bat enable rabbitmq_management

rabbitmq-service stop
rabbitmq-service remove
rabbitmq-service install
rabbitmq-service start

Manager default url: http://localhost:55672/#/
username: guest
password: guest

Start more Clients and Workers from Visual Studio:
Debug -> Start new instance

Other links:
http://www.codeproject.com/Articles/309786/Rabbit-Mq-Shovel-Example