using System;
using System.Diagnostics;
using System.Threading.Tasks;
using MiniPostpone.MessageProvider;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Xunit;

namespace MiniPostpone.Tests
{
    public class TestScheduling
    {
        private readonly PostponedMessageProvider messageProvider;
        private readonly ConnectionFactory connectionFactory;

        public TestScheduling()
        {
            connectionFactory = new ConnectionFactory
            {
                Endpoint = new AmqpTcpEndpoint(new Uri("amqp://localhost:5672"))
            };
            messageProvider = new PostponedMessageProvider(connectionFactory);
        }

        [Fact]
        public async Task PostponeMessage()
        {
            const string testQueue = nameof(testQueue);
            var flag = false;
            var timer = new Stopwatch();

            using var connection = connectionFactory.CreateConnection();
            using var channel = connection.CreateModel();
            channel.ExchangeDeclare(PostponeMq.OutputExchangeName, ExchangeType.Topic, true);
            channel.QueueDeclare(testQueue, true);
            channel.QueueBind(testQueue, PostponeMq.OutputExchangeName, "test.*");
            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += (_, args) =>
            {
                flag = true;
                timer.Stop();
                channel.BasicAck(args.DeliveryTag, false);
            };
            channel.BasicConsume(consumer, testQueue);

            var timeout = TimeSpan.FromSeconds(5);
            await messageProvider.ScheduleMessage("whatever", "test.me", timeout);
            timer.Start();

            await Task.Delay(TimeSpan.FromSeconds(4));
            Assert.False(flag);

            await Task.Delay(TimeSpan.FromSeconds(2));
            Assert.True(flag);

            Assert.True(Math.Abs(timer.ElapsedMilliseconds - timeout.TotalMilliseconds) < 100);
        }
    }
}