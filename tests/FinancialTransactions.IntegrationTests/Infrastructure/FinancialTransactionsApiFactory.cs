using FinancialTransactions.Infrastructure.Messaging;
using FinancialTransactions.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Testcontainers.PostgreSql;

namespace FinancialTransactions.IntegrationTests.Infrastructure
{
    public sealed class FinancialTransactionsApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
    {
        private readonly PostgreSqlContainer _postgresContainer = new PostgreSqlBuilder()
            .WithImage("postgres:16")
            .WithDatabase("financial_transactions_tests")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();

        public async Task InitializeAsync()
        {
            await _postgresContainer.StartAsync();

            using var scope = Services.CreateScope();

            var dbContext = scope.ServiceProvider.GetRequiredService<FinancialTransactionsDbContext>();

            await dbContext.Database.MigrateAsync();
        }

        public new async Task DisposeAsync()
        {
            await _postgresContainer.DisposeAsync();
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");

            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<FinancialTransactionsDbContext>>();

                services.AddDbContext<FinancialTransactionsDbContext>(options =>
                {
                    options.UseNpgsql(_postgresContainer.GetConnectionString());
                });

                services.RemoveAll<IHostedService>();
            });
        }
    }
}
