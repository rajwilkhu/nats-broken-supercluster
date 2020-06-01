# nats-cluster-tools

## Start Up

Start NATS supercluster with streaming cluster over two clusters.

```shell
docker-compose up
```

## Streaming STAN
(not working as expected) 

Start two STAN subscribers one in each cluster of the super cluster ... 

```shell
./stan-sub -s "nats://localhost:4221,nats://localhost:4222,nats://localhost:4223" -c test-cluster -id client1 test
```
```shell
./stan-sub -s "nats://localhost:4224,nats://localhost:4225,nats://localhost:4226" -c test-cluster -id client2 test
```

STAN pubish in one of the two clusters ... 

```shell
 ./stan-pub -s "nats://localhost:4221,nats://localhost:4222,nats://localhost:4223" -c test-cluster test "hello-world"
```

Publisher output

```shell
Published [test] : 'hello-world'
```

'client1' subscriber output

```shell
Connected to nats://localhost:4221,nats://localhost:4222,nats://localhost:4223 clusterID: [test-cluster] clientID: [client1]
Listening on [test], clientID=[client1], qgroup=[] durable=[]
[#1] Received: sequence:1 subject:"test" data:"hello-world" timestamp:1591024624270634300
[#2] Received: sequence:1 subject:"test" data:"hello-world" timestamp:1591024624270811400
[#3] Received: sequence:1 subject:"test" data:"hello-world" timestamp:1591024624270811400 redelivered:true redeliveryCount:1
[#4] Received: sequence:1 subject:"test" data:"hello-world" timestamp:1591024624270811400 redelivered:true redeliveryCount:2
[#5] Received: sequence:1 subject:"test" data:"hello-world" timestamp:1591024624270811400 redelivered:true redeliveryCount:3
[#6] Received: sequence:1 subject:"test" data:"hello-world" timestamp:1591024624270811400 redelivered:true redeliveryCount:4
^C
Received an interrupt, unsubscribing and closing connection...
```

CTRL-C as redelivery continues ... 

'client2' subscriber output

```shell
Connected to nats://localhost:4224,nats://localhost:4225,nats://localhost:4226 clusterID: [test-cluster] clientID: [client2]
Listening on [test], clientID=[client2], qgroup=[] durable=[]
[#1] Received: sequence:1 subject:"test" data:"hello-world" timestamp:1591024624270811400
[#2] Received: sequence:1 subject:"test" data:"hello-world" timestamp:1591024624270634300
[#3] Received: sequence:1 subject:"test" data:"hello-world" timestamp:1591024624270634300 redelivered:true redeliveryCount:1
[#4] Received: sequence:1 subject:"test" data:"hello-world" timestamp:1591024624270634300 redelivered:true redeliveryCount:2
[#5] Received: sequence:1 subject:"test" data:"hello-world" timestamp:1591024624270634300 redelivered:true redeliveryCount:3
[#6] Received: sequence:1 subject:"test" data:"hello-world" timestamp:1591024624270634300 redelivered:true redeliveryCount:4
^C
Received an interrupt, unsubscribing and closing connection...
```

CTRL-C as redelivery continues ... 


## Vanilla NATS
(working as expected)

Start two subscribers one is each cluster of the super cluster ... 

```shell
./nats-sub -s "nats://localhost:4221,nats://localhost:4222,nats://localhost:4223" -t test
```
```shell
./nats-sub -s "nats://localhost:4224,nats://localhost:4225,nats://localhost:4226" -t test
```

NATS pubish in one of the two clusters ...

```shell
./nats-pub  -s "nats://localhost:4221,nats://localhost:4222,nats://localhost:4223" test "hello-world"
```

Publisher output

```shell
Published [test] : 'hello-world'
```

'client1' subscriber output -> single message received.

```shell
Listening on [test]
2020/06/01 16:09:47 [#1] Received on [test]: 'hello-world'
^C
```

'client2' subscriber output -> single message received.

```shell
Listening on [test]
2020/06/01 16:09:47 [#1] Received on [test]: 'hello-world'
^C
```