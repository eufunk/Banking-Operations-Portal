using Banking.Application.Accounts.Dtos;
using Banking.Application.Common.Messaging;
using Banking.Domain.Accounts;

namespace Banking.Application.Accounts.GetAccountDetails;

public sealed record GetAccountDetailsQuery(AccountId AccountId) : IQuery<AccountDetailsDto>;
