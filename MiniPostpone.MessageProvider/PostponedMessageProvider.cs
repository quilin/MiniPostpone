using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Framing;

namespace MiniPostpone.MessageProvider
{
    public class PostponedMessageProvider : IPostponedMessageProvider
    {
        private readonly IConnectionFactory connectionFactory;

        public PostponedMessageProvider(
            IConnectionFactory connectionFactory)
        {
            this.connectionFactory = connectionFactory;
        }

        private static void EnsureTopology(IModel channel, Guid messageId, TimeSpan timeout)
        {
            var exchangeName = PostponeMq.InputExchangeName(messageId);
            var queueName = PostponeMq.InputQueueName(messageId);
            channel.ExchangeDeclare(PostponeMq.OutputExchangeName, ExchangeType.Topic, true);
            channel.ExchangeDeclare(exchangeName, ExchangeType.Topic, true);
            channel.QueueDeclare(queueName, true, false, false,
                new Dictionary<string, object>
                {
                    ["x-dead-letter-exchange"] = PostponeMq.OutputExchangeName,
                    ["x-message-ttl"] = (int) timeout.TotalMilliseconds
                });
            channel.QueueBind(queueName, exchangeName, "#");
        }

        public Task<Guid> ScheduleMessage(object message, string routingKey, TimeSpan timeout)
        {
            using var connection = connectionFactory.CreateConnection();
            using var channel = connection.CreateModel();

            var messageId = Guid.NewGuid();
            EnsureTopology(channel, messageId, timeout);

            var messageBody = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(message));
            var basicProperties = new BasicProperties {Persistent = true};
            var exchangeName = PostponeMq.InputExchangeName(messageId);
            channel.BasicPublish(exchangeName, routingKey, basicProperties, messageBody);

            return Task.FromResult(messageId);
        }

        public Task<Guid> ScheduleMessage(object message, string routingKey, DateTime dateTime)
        {
            var timeSpan = dateTime.ToUniversalTime() - DateTime.UtcNow;
            if (timeSpan <= TimeSpan.Zero)
            {
                throw new PostponedMessageException($"Message should be scheduled in the future! Was {timeSpan} ago");
            }

            return ScheduleMessage(message, routingKey, timeSpan);
        }

        public Task CancelSchedule(Guid messageId)
        {
            using var connection = connectionFactory.CreateConnection();
            using var channel = connection.CreateModel();
            channel.QueueDelete(PostponeMq.InputQueueName(messageId));
            channel.ExchangeDelete(PostponeMq.InputExchangeName(messageId));
            return Task.CompletedTask;
        }
    }
}