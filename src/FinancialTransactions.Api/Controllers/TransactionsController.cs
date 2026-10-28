using FinancialTransactions.Api.Contracts;
using FinancialTransactions.Application.Abstractions;
using FinancialTransactions.Application.Transactions;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace FinancialTransactions.Api.Controllers
{
    [ApiController]
    [Route("api/transactions")]
    public sealed class TransactionsController : ControllerBase
    {
        private readonly ProcessTransactionUseCase _processTransactionUseCase;
        private readonly ITransactionMessagePublisher _transactionMessagePublisher;
        private readonly IValidator<ProcessTransactionCommand> _validator;
        private readonly ILogger<TransactionsController> _logger;

        public TransactionsController(
            ProcessTransactionUseCase processTransactionUseCase,
            ITransactionMessagePublisher transactionMessagePublisher,
            IValidator<ProcessTransactionCommand> validator,
            ILogger<TransactionsController> logger)
        {
            _processTransactionUseCase = processTransactionUseCase;
            _transactionMessagePublisher = transactionMessagePublisher;
            _validator = validator;
            _logger = logger;
        }

        [HttpPost]
        [ProducesResponseType(typeof(ProcessTransactionResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProcessTransactionResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProcessTransactionResponse), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> ProcessAsync(
            [FromBody] ProcessTransactionRequest request,
            CancellationToken cancellationToken)
        {
            var command = new ProcessTransactionCommand(
                request.EventId,
                request.AccountId,
                request.Type,
                request.Amount,
                request.OccurredAt);

            var result = await _processTransactionUseCase.ExecuteAsync(command, cancellationToken);

            var response = new ProcessTransactionResponse(
                result.Status.ToString(),
                result.Message,
                result.EventId,
                result.AccountId,
                result.CurrentBalance);

            return result.Status switch
            {
                ProcessTransactionStatus.Processed => Ok(response),
                ProcessTransactionStatus.Duplicated => Ok(response),
                ProcessTransactionStatus.InsufficientFunds => Conflict(response),
                ProcessTransactionStatus.Invalid => BadRequest(response),
                _ => BadRequest(response)
            };
        }

        [HttpPost("async")]
        [ProducesResponseType(typeof(ProcessTransactionResponse), StatusCodes.Status202Accepted)]
        [ProducesResponseType(typeof(ProcessTransactionResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ProcessAsyncByQueue(
            [FromBody] ProcessTransactionRequest request,
            CancellationToken cancellationToken)
        {
            var command = new ProcessTransactionCommand(
                request.EventId,
                request.AccountId,
                request.Type,
                request.Amount,
                request.OccurredAt);

            var validationResult = await _validator.ValidateAsync(command, cancellationToken);

            if (!validationResult.IsValid)
            {
                var message = string.Join(
                    " | ",
                    validationResult.Errors.Select(error => error.ErrorMessage));

                var invalidResponse = new ProcessTransactionResponse(
                    ProcessTransactionStatus.Invalid.ToString(),
                    message,
                    command.EventId,
                    command.AccountId,
                    null);

                return BadRequest(invalidResponse);
            }

            await _transactionMessagePublisher.PublishAsync(command, cancellationToken);

            var response = new ProcessTransactionResponse(
                "Accepted",
                "Transaction event accepted for asynchronous processing.",
                command.EventId,
                command.AccountId,
                null);

            return Accepted(response);
        }
    }
}
