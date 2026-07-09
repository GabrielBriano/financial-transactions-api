using System.Text;
using System.Text.Json;
using FinancialTransactions.Application.Transactions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace FinancialTransactions.Infrastructure.Messaging
{
    public sealed class RabbitMqTransactionConsumer : BackgroundService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly RabbitMqOptions _options;
        private readonly ILogger<RabbitMqTransactionConsumer> _logger;

        private IConnection? _connection;
        private IModel? _channel;

        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        public RabbitMqTransactionConsumer(
            IServiceScopeFactory serviceScopeFactory,
            IOptions<RabbitMqOptions> options,
            ILogger<RabbitMqTransactionConsumer> logger)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _options = options.Value;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    StartConsumer();

                    await Task.Delay(Timeout.Infinite, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        ex,
                        "RabbitMQ consumer is unavailable. Retrying in 10 seconds.");

                    await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
                }
            }
        }

        private void StartConsumer()
        {
            var factory = new ConnectionFactory
            {
                HostName = _options.Host,
                Port = _options.Port,
                UserName = _options.Username,
                Password = _options.Password,
                DispatchConsumersAsync = true
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            _channel.QueueDeclare(
                queue: _options.QueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            _channel.BasicQos(
                prefetchSize: 0,
                prefetchCount: 1,
                global: false);

            var consumer = new AsyncEventingBasicConsumer(_channel);

            consumer.Received += async (_, eventArgs) =>
            {
                await ProcessMessageAsync(eventArgs);
            };

            _channel.BasicConsume(
                queue: _options.QueueName,
                autoAck: false,
                consumer: consumer);

            _logger.LogInformation(
                "RabbitMQ consumer started. Queue: {QueueName}",
                _options.QueueName);
        }

        private async Task ProcessMessageAsync(BasicDeliverEventArgs eventArgs)
        {
            if (_channel is null)
                return;

            try
            {
                var json = Encoding.UTF8.GetString(eventArgs.Body.ToArray());

                var command = JsonSerializer.Deserialize<ProcessTransactionCommand>(
                    json,
                    JsonOptions);

                if (command is null)
                {
                    _channel.BasicNack(
                        deliveryTag: eventArgs.DeliveryTag,
                        multiple: false,
                        requeue: false);

                    return;
                }

                using var scope = _serviceScopeFactory.CreateScope();

                var useCase = scope.ServiceProvider.GetRequiredService<ProcessTransactionUseCase>();

                var result = await useCase.ExecuteAsync(command, CancellationToken.None);

                _channel.BasicAck(
                    deliveryTag: eventArgs.DeliveryTag,
                    multiple: false);

                _logger.LogInformation(
                    "RabbitMQ transaction message processed. EventId: {EventId}, Status: {Status}",
                    command.EventId,
                    result.Status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing RabbitMQ transaction message.");

                _channel.BasicNack(
                    deliveryTag: eventArgs.DeliveryTag,
                    multiple: false,
                    requeue: true);
            }
        }

        public override void Dispose()
        {
            _channel?.Dispose();
            _connection?.Dispose();

            base.Dispose();
        }
    }
}
