using Elastic.Ingest.Elasticsearch;
using Elastic.Ingest.Elasticsearch.DataStreams;
using Elastic.Serilog.Sinks;
using FinancialTransactions.Application;
using FinancialTransactions.Infrastructure;
using FinancialTransactions.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Debugging;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

SelfLog.Enable(message => Console.Error.WriteLine($"SERILOG SELFLOG: {message}"));

var elasticsearchUri = builder.Configuration["Elasticsearch:Uri"];

var loggerConfiguration = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "FinancialTransactions.Api")
    .Enrich.WithProperty("Environment", builder.Environment.EnvironmentName)
    .WriteTo.Console();

if (!string.IsNullOrWhiteSpace(elasticsearchUri))
{
    loggerConfiguration = loggerConfiguration.WriteTo.Elasticsearch(
        new[] { new Uri(elasticsearchUri) },
        options =>
        {
            options.DataStream = new DataStreamName(
                "logs",
                "financial-transactions",
                builder.Environment.EnvironmentName.ToLowerInvariant());

            options.BootstrapMethod = BootstrapMethod.None;
        });
}

Log.Logger = loggerConfiguration.CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("Postgres")!)
    .AddRedis(builder.Configuration["Redis:ConnectionString"]!);

var app = builder.Build();

app.UseSerilogRequestLogging();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.MapControllers();

app.MapHealthChecks("/health");

if (!app.Environment.IsEnvironment("Testing"))
{
    using var scope = app.Services.CreateScope();

    var dbContext = scope.ServiceProvider.GetRequiredService<FinancialTransactionsDbContext>();

    await dbContext.Database.MigrateAsync();
}

app.Run();

public partial class Program
{
}