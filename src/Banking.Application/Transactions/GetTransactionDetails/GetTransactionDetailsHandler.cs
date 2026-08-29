using Banking.Application.Common;
using Banking.Application.Common.Errors;
using Banking.Application.Common.Messaging;
using Banking.Application.Transactions.Dtos;

namespace Banking.Application.Transactions.GetTransactionDetails;

public sealed class GetTransactionDetailsHandler : IQueryHandler<GetTransactionDetailsQuery, TransactionDetailsDto>
{
    private readonly ITransactionRepository _transactionRepository;

    public GetTransactionDetailsHandler(ITransactionRepository transactionRepository)
    {
        _transactionRepository = transactionRepository;
    }

    public async Task<Result<TransactionDetailsDto>> Handle(GetTransactionDetailsQuery query, CancellationToken cancellationToken)
    {
        var transaction = await _transactionRepository.GetByIdAsync(query.TransactionId, cancellationToken);
        if (transaction is null)
        {
            return Result<TransactionDetailsDto>.Failure(Error.NotFound(
                "Transaction.NotFound", $"Transaktion {query.TransactionId} wurde nicht gefunden."));
        }

        var dto = new TransactionDetailsDto(
            transaction.Id,
            transaction.AccountId,
            transaction.Amount.Amount,
            transaction.Amount.Currency.Value,
            transaction.BookingDate,
            transaction.TransactionType,
            transaction.Status,
            transaction.Description);

        return Result<TransactionDetailsDto>.Success(dto);
    }
}
