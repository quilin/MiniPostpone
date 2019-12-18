# MiniPostpone
Minor library for scheduled messages that only requires RabbitMQ and has almost nothing useful in functionality

## Why would you need that?
There are plenty of scenarios where you need to do something but not right now. Usually we do it like that:

~~0. Install Quartz or Hangfire~~
1. Put a record in a DB about thing that needs to be done
2. Schedule some service that will check the DB every other second/minute and fire events to MQ
3. Subscribe to the queue to get the message

And it brings whole lot of problems in maintainance, such as:

1. Time quantization. You can control it, for sure, but in any way it will be less reactive/realtime.
2. Service dependency. What if it fails? It is an application that has its own dependencies, such as the DB.
3. Scaling. You can shard your DB, right? And then scale your schedule services to be at least one (otherwize you won't get some of your messages) and at most one (or you must face race conditions) for every shard.
4. DB architecture. Well, you'll need to put `nvarchar(max)` to your `message` column, which is bad, right?

Of course, you could use Quartz or Hangfire and **I highly recommend you do so**. But what if your manager hates it? Or the manager wants to have a cluster configuration for your scheduled messages without payment requirements? Or you just want a quick start and don't want to bring the extra DB, lots of code and all that.

## How it works?
It is very simple. It uses the RabbitMQ TTL functionality: every queue and/or message may be signed by certain header that forces the message to be rejected after given timeout and thus moved to the dead letter exchange if any given.

Message scheduling requires the message itself, its routing key and relative or absolute time of its scheduled sending.

```csharp
var connectionFactory = new ConnectionFactory();                        // From RabbitMQ.Client NuGet package
var messageProvider = new PostponedMessageProvider(connectionFactory);  // The provider itself
var id = await messageProvider.ScheduleMessage("Hello, world!", "hello.world", TimeSpan.FromMinutes(5));
```

The provider then creates an exchange and a queue with unique names and binds those together. The queue has TTL of a given timeout.
It also declares the output exchange for messages to come to after the timeout and sets it as a dead letter exchange for the queue.
You now may bind your queue to this exchange called `mpp.output` and receive your messages with stringified body.

You can also cancel the message using the `id` that provider returns after scheduling your message. Just call the cancel method:

```csharp
await messageProvider.CancelSchedule(id);
```

The bad news is that those unique-named queue and exchange will eventually make your RabbitMQ server a pile of ~~trash~~ empty queues and exchanges that never being used. To fix it there's also a cleaning service in this repository. It is a Service worker from dotnet 3.0 and it creates specific queue to trace all the messages that come to the output exchange and then just removes their source queue and exchange.

## Wait, why not just put all the messages in one queue? Why many??
There are two reasons:
1. RabbitMQ queues are true FIFO, so even if your message is timed out it will not be out of the queue until it is in the start of it. So if you put a message with 3h timeout and then a message with 1h timeout, the second will get from the queue only after 3 hours. Pity.
2. Again, the FIFO issue. Sometimes you want to cancel the scheduled sending and it means you have to remove the message from the queue, but you cannot do that if the message is somewhere in the middle.

You can put some gateway to read messages and then remove it after the timeout but that means holding a storage with message ids (even if the id is just `DeliveryTag`).

I'm not a big fan of this workaround, either. But got nothing better in my mind.

## What next?
This project is not some sort of a startup or even something serious. I'm not using it myself yet in a petproject, but I'm about to.
