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

        public Task<Guid> ScheduleMessage(object message, TimeSpan timeout)
        {
            using var connection = connectionFactory.CreateConnection();
            using var channel = connection.CreateModel();

            var messageId = Guid.NewGuid();
            var exchangeName = PostponeMq.InputExchangeName(messageId);
            var queueName = PostponeMq.InputQueueName(messageId);
            channel.ExchangeDeclare(exchangeName, ExchangeType.Direct, true);
            channel.QueueDeclare(queueName, true, false);
            channel.QueueBind(queueName, exchangeName, string.Empty, new Dictionary<string, object>
            {
                ["x-dead-letter-exchange"] = PostponeMq.OutputExchangeName,
                ["x-message-ttl"] = (int) timeout.TotalMilliseconds
            });
            channel.BasicPublish(exchangeName, string.Empty, new BasicProperties(),
                Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(message)));

            return Task.FromResult(messageId);
        }

        public Task<Guid> ScheduleMessage(object message, DateTime dateTime)
        {
            var timeSpan = dateTime.ToUniversalTime() - DateTime.UtcNow;
            if (timeSpan <= TimeSpan.Zero)
            {
                throw new PostponedMessageException($"Message should be scheduled in the future! Was {timeSpan} ago");
            }

            return ScheduleMessage(message, timeSpan);
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