using FinancialTransactions.Application.Transactions;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace FinancialTransactions.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddScoped<ProcessTransactionUseCase>();

            services.AddScoped<IValidator<ProcessTransactionCommand>, ProcessTransactionCommandValidator>();

            return services;
        }
    }
}
