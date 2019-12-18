using System;
using System.Threading;
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
            var counter = 0;
            const string testQueue = nameof(testQueue);

            using var connection = connectionFactory.CreateConnection();
            using var channel = connection.CreateModel();
            channel.ExchangeDeclare(PostponeMq.OutputExchangeName, ExchangeType.Topic, true);
            channel.QueueDeclare(testQueue, true);
            channel.QueueBind(testQueue, PostponeMq.OutputExchangeName, "test.*");
            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += (_, args) => Interlocked.Increment(ref counter);
            channel.BasicConsume(consumer, testQueue);

            await messageProvider.ScheduleMessage("whatever", "test.me", TimeSpan.FromSeconds(5));

            await Task.Delay(TimeSpan.FromSeconds(4));
            Assert.Equal(0, counter);

            await Task.Delay(TimeSpan.FromSeconds(2));
            Assert.Equal(1, counter);
        }
    }
}