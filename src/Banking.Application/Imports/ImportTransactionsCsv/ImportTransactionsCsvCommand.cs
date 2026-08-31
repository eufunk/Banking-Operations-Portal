using Banking.Application.Common.Messaging;
using Banking.Application.Imports.Dtos;

namespace Banking.Application.Imports.ImportTransactionsCsv;

/// <summary>CsvContent statt Stream/IFormFile - Application bleibt frei von ASP.NET-Core-Typen,
/// Banking.Api liest die hochgeladene Datei in Program.cs/Controller in einen string ein.</summary>
public sealed record ImportTransactionsCsvCommand(string FileName, string CsvContent) : ICommand<ImportJobSummaryDto>;
