using Banking.Application.Common.Messaging;
using Banking.Application.Imports.Dtos;
using Banking.Domain.Imports;

namespace Banking.Application.Imports.GetImportJobDetails;

public sealed record GetImportJobDetailsQuery(ImportJobId ImportJobId) : IQuery<ImportJobDetailsDto>;
