using FinancialTransactions.Api.Contracts;
using FinancialTransactions.Application.Abstractions;
using FinancialTransactions.Application.Common;
using Microsoft.AspNetCore.Mvc;

namespace FinancialTransactions.Api.Controllers
{
    [ApiController]
    [Route("api/accounts")]
    public sealed class AccountsController : ControllerBase
    {
        private readonly IAccountRepository _accountRepository;
        private readonly ITransactionRepository _transactionRepository;
        private readonly ICacheService _cacheService;

        public AccountsController(
            IAccountRepository accountRepository,
            ITransactionRepository transactionRepository,
            ICacheService cacheService)
        {
            _accountRepository = accountRepository;
            _transactionRepository = transactionRepository;
            _cacheService = cacheService;
        }

        [HttpGet("{accountId:guid}")]
        [ProducesResponseType(typeof(AccountResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetAccountAsync(
            Guid accountId,
            CancellationToken cancellationToken)
        {
            var cacheKey = CacheKeys.Account(accountId);

            var cachedAccount = await _cacheService.GetAsync<AccountResponse>(
                cacheKey,
                cancellationToken);

            if (cachedAccount is not null)
                return Ok(cachedAccount);

            var account = await _accountRepository.GetByIdAsync(accountId, cancellationToken);

            if (account is null)
                return NotFound();

            var response = new AccountResponse(
                account.Id,
                account.Balance,
                account.CreatedAt,
                account.UpdatedAt);

            await _cacheService.SetAsync(
                cacheKey,
                response,
                TimeSpan.FromMinutes(5),
                cancellationToken);

            return Ok(response);
        }

        [HttpGet("{accountId:guid}/transactions")]
        [ProducesResponseType(typeof(IReadOnlyList<TransactionResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetTransactionsAsync(
            Guid accountId,
            CancellationToken cancellationToken)
        {
            var transactions = await _transactionRepository.GetByAccountIdAsync(accountId, cancellationToken);

            var response = transactions
                .Select(transaction => new TransactionResponse(
                    transaction.Id,
                    transaction.EventId,
                    transaction.AccountId,
                    transaction.Type.ToString(),
                    transaction.Amount,
                    transaction.OccurredAt,
                    transaction.CreatedAt))
                .ToList();

            return Ok(response);
        }
    }
}
