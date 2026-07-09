using FinancialTransactions.Application.Abstractions;
using FinancialTransactions.Application.Common;
using FinancialTransactions.Domain.Accounts;
using FinancialTransactions.Domain.Common;
using FinancialTransactions.Domain.Transactions;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace FinancialTransactions.Application.Transactions
{
    public sealed class ProcessTransactionUseCase
    {
        private readonly IAccountRepository _accountRepository;
        private readonly ITransactionRepository _transactionRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IValidator<ProcessTransactionCommand> _validator;
        private readonly ILogger<ProcessTransactionUseCase> _logger;
        private readonly ICacheService _cacheService;

        public ProcessTransactionUseCase(
            IAccountRepository accountRepository,
            ITransactionRepository transactionRepository,
            IUnitOfWork unitOfWork,
            IValidator<ProcessTransactionCommand> validator,
            ILogger<ProcessTransactionUseCase> logger,
            ICacheService cacheService)
        {
            _accountRepository = accountRepository;
            _transactionRepository = transactionRepository;
            _unitOfWork = unitOfWork;
            _validator = validator;
            _logger = logger;
            _cacheService = cacheService;
        }

        public async Task<ProcessTransactionResult> ExecuteAsync(
            ProcessTransactionCommand command,
            CancellationToken cancellationToken)
        {
            var validationResult = await _validator.ValidateAsync(command, cancellationToken);

            if (!validationResult.IsValid)
            {
                var message = string.Join(" | ", validationResult.Errors.Select(error => error.ErrorMessage));

                return ProcessTransactionResult.Invalid(
                    command.EventId,
                    command.AccountId,
                    message);
            }

            var transactionType = ParseTransactionType(command.Type);

            ProcessTransactionResult? result = null;

            await _unitOfWork.ExecuteInTransactionAsync(async ct =>
            {
                var alreadyProcessed = await _transactionRepository.ExistsByEventIdAsync(
                    command.EventId,
                    ct);

                if (alreadyProcessed)
                {
                    _logger.LogInformation(
                        "Duplicated transaction ignored. EventId: {EventId}, AccountId: {AccountId}",
                        command.EventId,
                        command.AccountId);

                    result = ProcessTransactionResult.Duplicated(
                        command.EventId,
                        command.AccountId);

                    return;
                }

                var account = await _accountRepository.GetByIdForUpdateAsync(
                    command.AccountId,
                    ct);

                if (account is null)
                {
                    account = new Account(command.AccountId);

                    await _accountRepository.AddAsync(account, ct);
                }

                try
                {
                    ApplyTransaction(account, transactionType, command.Amount);
                }
                catch (DomainException ex) when (ex.Message == "Insufficient funds.")
                {
                    result = ProcessTransactionResult.InsufficientFunds(
                        command.EventId,
                        command.AccountId,
                        account.Balance);

                    return;
                }

                var transaction = new FinancialTransaction(
                    command.EventId,
                    command.AccountId,
                    transactionType,
                    command.Amount,
                    command.OccurredAt);

                await _transactionRepository.AddAsync(transaction, ct);

                result = ProcessTransactionResult.Processed(
                    command.EventId,
                    command.AccountId,
                    account.Balance);

                await _cacheService.RemoveAsync(CacheKeys.Account(command.AccountId), ct);

                _logger.LogInformation(
                    "Transaction processed. EventId: {EventId}, AccountId: {AccountId}, Type: {Type}, Amount: {Amount}, Balance: {Balance}",
                    command.EventId,
                    command.AccountId,
                    command.Type,
                    command.Amount,
                    account.Balance);
            }, cancellationToken);

            return result ?? ProcessTransactionResult.Invalid(
                command.EventId,
                command.AccountId,
                "Transaction could not be processed.");
        }

        private static TransactionType ParseTransactionType(string type)
        {
            return type.Equals("CREDIT", StringComparison.OrdinalIgnoreCase)
                ? TransactionType.Credit
                : TransactionType.Debit;
        }

        private static void ApplyTransaction(
            Account account,
            TransactionType transactionType,
            decimal amount)
        {
            if (transactionType == TransactionType.Credit)
            {
                account.Credit(amount);
                return;
            }

            account.Debit(amount);
        }
    }
}
