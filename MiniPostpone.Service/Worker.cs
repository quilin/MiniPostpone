using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MiniPostpone.MessageProvider;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace MiniPostpone.Service
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> logger;
        private readonly IOptions<MqConfiguration> configuration;

        public Worker(
            ILogger<Worker> logger,
            IOptions<MqConfiguration> configuration)
        {
            this.logger = logger;
            this.configuration = configuration;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var connectionFactory = new ConnectionFactory
            {
                Endpoint = new AmqpTcpEndpoint(new Uri(configuration.Value.ConnectionUri))
            };

            var connection = connectionFactory.CreateConnection();
            var channel = connection.CreateModel();
            channel.ExchangeDeclare(PostponeMq.OutputExchangeName, ExchangeType.Topic, true);
            channel.QueueDeclare(PostponeMq.OutputClearQueueName, true, false);
            channel.QueueBind(PostponeMq.OutputClearQueueName, PostponeMq.OutputExchangeName, "#");

            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += (_, args) =>
            {
                try
                {
                    var (queue, exchange) = ExtractSourceInformation(args.BasicProperties);
                    channel.QueueDelete(queue);
                    channel.ExchangeDelete(exchange);
                    channel.BasicAck(args.DeliveryTag, false);
                }
                catch
                {
                    channel.BasicNack(args.DeliveryTag, false, false);
                }
            };
            channel.BasicConsume(consumer, PostponeMq.OutputClearQueueName);

            while (!stoppingToken.IsCancellationRequested)
            {
                logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                await Task.Delay(1000, stoppingToken);
            }
        }

        private static (string queue, string exchange) ExtractSourceInformation(IBasicProperties basicProperties)
        {
            var headersEnvelope = (List<object>) basicProperties.Headers["x-death"];
            var headerValueEnvelope = (Dictionary<string, object>) headersEnvelope[0];
            var queueNameBytes = (byte[]) headerValueEnvelope["queue"];
            var exchangeNameBytes = (byte[]) headerValueEnvelope["exchange"];
            return (Encoding.UTF8.GetString(queueNameBytes), Encoding.UTF8.GetString(exchangeNameBytes));
        }
    }
}