using Banking.Application.Common.Messaging;
using Banking.Application.Transactions.Dtos;
using Banking.Domain.Transactions;

namespace Banking.Application.Transactions.GetTransactionDetails;

public sealed record GetTransactionDetailsQuery(TransactionId TransactionId) : IQuery<TransactionDetailsDto>;
