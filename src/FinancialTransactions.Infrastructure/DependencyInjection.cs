using FinancialTransactions.Application.Abstractions;
using FinancialTransactions.Infrastructure.Caching;
using FinancialTransactions.Infrastructure.Persistence;
using FinancialTransactions.Infrastructure.Persistence.Repositories;
using FinancialTransactions.Infrastructure.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FinancialTransactions.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("Postgres");

            services.AddDbContext<FinancialTransactionsDbContext>(options =>
            {
                options.UseNpgsql(connectionString);
            });

            services.AddScoped<IAccountRepository, AccountRepository>();
            services.AddScoped<ITransactionRepository, TransactionRepository>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            services.AddSingleton<ICacheService, RedisCacheService>();

            services.Configure<RabbitMqOptions>(configuration.GetSection("RabbitMq"));

            services.AddSingleton<ITransactionMessagePublisher, RabbitMqTransactionPublisher>();
            services.AddHostedService<RabbitMqTransactionConsumer>();

            return services;
        }
    }
}
