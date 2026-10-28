using FinancialTransactions.Application.Abstractions;
using FinancialTransactions.Application.Transactions;
using FinancialTransactions.Domain.Accounts;
using FinancialTransactions.Domain.Transactions;
using FluentAssertions;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Moq;

namespace FinancialTransactions.UnitTests.Application
{
    public sealed class ProcessTransactionUseCaseTests
    {
        private readonly Mock<IAccountRepository> _accountRepositoryMock = new();
        private readonly Mock<ITransactionRepository> _transactionRepositoryMock = new();
        private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
        private readonly Mock<ILogger<ProcessTransactionUseCase>> _loggerMock = new();
        private readonly Mock<ICacheService> _cacheServiceMock = new();
        private readonly IValidator<ProcessTransactionCommand> _validator = new ProcessTransactionCommandValidator();

        public ProcessTransactionUseCaseTests()
        {
            _unitOfWorkMock
                .Setup(unitOfWork => unitOfWork.ExecuteInTransactionAsync(
                    It.IsAny<Func<CancellationToken, Task>>(),
                    It.IsAny<CancellationToken>()))
                .Returns<Func<CancellationToken, Task>, CancellationToken>(
                    async (operation, cancellationToken) => await operation(cancellationToken));
        }

        [Fact]
        public async Task ExecuteAsync_Should_ProcessCredit_When_CommandIsValid()
        {
            var command = new ProcessTransactionCommand(
                Guid.NewGuid(),
                Guid.NewGuid(),
                "CREDIT",
                150.75m,
                DateTimeOffset.UtcNow);

            _transactionRepositoryMock
                .Setup(repository => repository.ExistsByEventIdAsync(command.EventId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            _accountRepositoryMock
                .Setup(repository => repository.GetByIdForUpdateAsync(command.AccountId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Account?)null);

            var useCase = CreateUseCase();

            var result = await useCase.ExecuteAsync(command, CancellationToken.None);

            result.Status.Should().Be(ProcessTransactionStatus.Processed);
            result.CurrentBalance.Should().Be(150.75m);

            _accountRepositoryMock.Verify(
                repository => repository.AddAsync(
                    It.Is<Account>(account => account.Id == command.AccountId && account.Balance == 150.75m),
                    It.IsAny<CancellationToken>()),
                Times.Once);

            _transactionRepositoryMock.Verify(
                repository => repository.AddAsync(
                    It.Is<FinancialTransaction>(transaction =>
                        transaction.EventId == command.EventId &&
                        transaction.AccountId == command.AccountId &&
                        transaction.Amount == command.Amount &&
                        transaction.Type == TransactionType.Credit),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_Should_ProcessDebit_When_BalanceIsEnough()
        {
            var account = new Account(Guid.NewGuid());
            account.Credit(200m);

            var command = new ProcessTransactionCommand(
                Guid.NewGuid(),
                account.Id,
                "DEBIT",
                50m,
                DateTimeOffset.UtcNow);

            _transactionRepositoryMock
                .Setup(repository => repository.ExistsByEventIdAsync(command.EventId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            _accountRepositoryMock
                .Setup(repository => repository.GetByIdForUpdateAsync(command.AccountId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(account);

            var useCase = CreateUseCase();

            var result = await useCase.ExecuteAsync(command, CancellationToken.None);

            result.Status.Should().Be(ProcessTransactionStatus.Processed);
            result.CurrentBalance.Should().Be(150m);
            account.Balance.Should().Be(150m);

            _transactionRepositoryMock.Verify(
                repository => repository.AddAsync(
                    It.Is<FinancialTransaction>(transaction =>
                        transaction.EventId == command.EventId &&
                        transaction.Type == TransactionType.Debit),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_Should_ReturnDuplicated_When_EventWasAlreadyProcessed()
        {
            var command = new ProcessTransactionCommand(
                Guid.NewGuid(),
                Guid.NewGuid(),
                "CREDIT",
                100m,
                DateTimeOffset.UtcNow);

            _transactionRepositoryMock
                .Setup(repository => repository.ExistsByEventIdAsync(command.EventId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var useCase = CreateUseCase();

            var result = await useCase.ExecuteAsync(command, CancellationToken.None);

            result.Status.Should().Be(ProcessTransactionStatus.Duplicated);
            result.Success.Should().BeTrue();

            _accountRepositoryMock.Verify(
                repository => repository.GetByIdForUpdateAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
                Times.Never);

            _transactionRepositoryMock.Verify(
                repository => repository.AddAsync(It.IsAny<FinancialTransaction>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task ExecuteAsync_Should_ReturnInsufficientFunds_When_DebitIsGreaterThanBalance()
        {
            var account = new Account(Guid.NewGuid());
            account.Credit(100m);

            var command = new ProcessTransactionCommand(
                Guid.NewGuid(),
                account.Id,
                "DEBIT",
                150m,
                DateTimeOffset.UtcNow);

            _transactionRepositoryMock
                .Setup(repository => repository.ExistsByEventIdAsync(command.EventId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            _accountRepositoryMock
                .Setup(repository => repository.GetByIdForUpdateAsync(command.AccountId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(account);

            var useCase = CreateUseCase();

            var result = await useCase.ExecuteAsync(command, CancellationToken.None);

            result.Status.Should().Be(ProcessTransactionStatus.InsufficientFunds);
            result.CurrentBalance.Should().Be(100m);
            account.Balance.Should().Be(100m);

            _transactionRepositoryMock.Verify(
                repository => repository.AddAsync(It.IsAny<FinancialTransaction>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task ExecuteAsync_Should_ReturnInvalid_When_AmountIsInvalid()
        {
            var command = new ProcessTransactionCommand(
                Guid.NewGuid(),
                Guid.NewGuid(),
                "CREDIT",
                0,
                DateTimeOffset.UtcNow);

            var useCase = CreateUseCase();

            var result = await useCase.ExecuteAsync(command, CancellationToken.None);

            result.Status.Should().Be(ProcessTransactionStatus.Invalid);
            result.Message.Should().Contain("Amount must be greater than zero.");

            _unitOfWorkMock.Verify(
                unitOfWork => unitOfWork.ExecuteInTransactionAsync(
                    It.IsAny<Func<CancellationToken, Task>>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task ExecuteAsync_Should_ReturnInvalid_When_TypeIsInvalid()
        {
            var command = new ProcessTransactionCommand(
                Guid.NewGuid(),
                Guid.NewGuid(),
                "TRANSFER",
                100m,
                DateTimeOffset.UtcNow);

            var useCase = CreateUseCase();

            var result = await useCase.ExecuteAsync(command, CancellationToken.None);

            result.Status.Should().Be(ProcessTransactionStatus.Invalid);
            result.Message.Should().Contain("Type must be CREDIT or DEBIT.");

            _unitOfWorkMock.Verify(
                unitOfWork => unitOfWork.ExecuteInTransactionAsync(
                    It.IsAny<Func<CancellationToken, Task>>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        private ProcessTransactionUseCase CreateUseCase()
        {
            return new ProcessTransactionUseCase(
                _accountRepositoryMock.Object,
                _transactionRepositoryMock.Object,
                _unitOfWorkMock.Object,
                _validator,
                _loggerMock.Object,
                _cacheServiceMock.Object);
        }
    }
}
