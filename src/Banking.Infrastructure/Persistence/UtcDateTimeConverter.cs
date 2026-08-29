using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Banking.Infrastructure.Persistence;

/// <summary>
/// SQL Server (datetime2) kennt keine Zeitzonen - beim Lesen kommt ein DateTime mit
/// Kind=Unspecified zurück, selbst wenn wir ausschließlich UTC schreiben (die Domain
/// nutzt konsequent DateTime.UtcNow). Ohne diesen Converter würden nachgelagerte
/// Vergleiche/Serialisierungen die Kind-Information verlieren - ein klassischer,
/// schwer zu findender EF-Core/SQL-Server-Stolperstein. Wird global für alle
/// DateTime-Properties registriert, siehe BankingDbContext.ConfigureConventions.
/// </summary>
public sealed class UtcDateTimeConverter : ValueConverter<DateTime, DateTime>
{
    public UtcDateTimeConverter()
        : base(
            toProvider => toProvider,
            fromProvider => DateTime.SpecifyKind(fromProvider, DateTimeKind.Utc))
    {
    }
}
