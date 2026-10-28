using RabbitMQ.Client;

namespace FinancialTransactions.Infrastructure.Messaging
{
    /// <summary>
    /// Declara a fila principal e sua fila de mensagens mortas (DLQ).
    /// Publisher e consumer usam o mesmo método para que os argumentos da fila sejam idênticos.
    /// </summary>
    internal static class RabbitMqTopology
    {
        public static string DeadLetterQueueName(RabbitMqOptions options) => $"{options.QueueName}.dlq";

        public static void Declare(IModel channel, RabbitMqOptions options)
        {
            var deadLetterQueue = DeadLetterQueueName(options);

            channel.QueueDeclare(
                queue: deadLetterQueue,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            channel.QueueDeclare(
                queue: options.QueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: new Dictionary<string, object>
                {
                    ["x-dead-letter-exchange"] = string.Empty,
                    ["x-dead-letter-routing-key"] = deadLetterQueue
                });
        }
    }
}
