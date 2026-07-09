using System.Text;
using System.Text.Json;
using FinancialTransactions.Application.Abstractions;
using FinancialTransactions.Application.Transactions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace FinancialTransactions.Infrastructure.Messaging
{
    public sealed class RabbitMqTransactionPublisher : ITransactionMessagePublisher
    {
        private readonly RabbitMqOptions _options;
        private readonly ILogger<RabbitMqTransactionPublisher> _logger;

        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        public RabbitMqTransactionPublisher(
            IOptions<RabbitMqOptions> options,
            ILogger<RabbitMqTransactionPublisher> logger)
        {
            _options = options.Value;
            _logger = logger;
        }

        public Task PublishAsync(ProcessTransactionCommand command, CancellationToken cancellationToken)
        {
            var factory = CreateConnectionFactory();

            using var connection = factory.CreateConnection();
            using var channel = connection.CreateModel();

            channel.QueueDeclare(
                queue: _options.QueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(command, JsonOptions));

            var properties = channel.CreateBasicProperties();
            properties.Persistent = true;
            properties.MessageId = command.EventId.ToString();

            channel.BasicPublish(
                exchange: string.Empty,
                routingKey: _options.QueueName,
                basicProperties: properties,
                body: body);

            _logger.LogInformation(
                "Transaction message published to RabbitMQ. EventId: {EventId}, AccountId: {AccountId}",
                command.EventId,
                command.AccountId);

            return Task.CompletedTask;
        }

        private ConnectionFactory CreateConnectionFactory()
        {
            return new ConnectionFactory
            {
                HostName = _options.Host,
                Port = _options.Port,
                UserName = _options.Username,
                Password = _options.Password,
                DispatchConsumersAsync = true
            };
        }
    }
}
