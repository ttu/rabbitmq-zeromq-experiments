No installation required. Uses clrzmq or NetMQ library.

----------------------

Bind allows peers to connect. Connect connects to binded peer.

PUSH/DEALER will rotate messages out
PUB does multicast
SUB/PULL/DEALER rotates messages in
ROUTER rotates in, and uses addressed output
PAIR always sends to its unique peer, if any

PUSH & PUB difference:
PUB sends same message to all subscribers. PUSH round-robins messages to PULLers.

Good introduction:
http://www.coastrd.com/zeromq-messaging

Patterns:

PUSH/PULL
 - a pipelining mechanism
PUB/SUB
 - a data distribution pattern
REQ/REP
 - a RPC and task dirstibution 
DEALER/REP
DEALER/ROUTER
REOUTER/ROUTER
ROUTER/REQ(?)
PAIR/PAIR
 - 2 exclusive sockets
 
----------------------

This example has following:

Broker has Pull to Client and REP to Worker
Client has Push to Broker
Worker has REQ to Broker

Push (connect)
 - Send package
Pull (bind)
 - Receive package
REP (bind)
 - Wait for e.g. Ready message
 - Send package
REQ (connect)
 - Send e.g. Ready message
 - Receive package

----------------------

Links:

Guide
http://zguide.zeromq.org/page:all

Patterns (code samples)
https://github.com/imatix/zguide

Clrzmq is managed wrapper for unmanaged ZeroMQ libarary (libzm.dll)
https://github.com/zeromq/clrzmq

Netmq is native C# implementation of ZeroMQ
https://github.com/zeromq/netmq

http://www.codeproject.com/Articles/514959/ZeroMQ-via-Csharp-Multi-part-messages-JSON-and-Syn