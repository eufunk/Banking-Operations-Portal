using Banking.Domain.Accounts;
using Banking.Domain.Common;
using Banking.Domain.Transactions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Banking.Infrastructure.Persistence.Configurations;

public sealed class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.ToTable("Transactions");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id)
            .HasConversion(id => id.Value, value => new TransactionId(value))
            .ValueGeneratedNever();

        builder.Property(t => t.AccountId)
            .HasConversion(id => id.Value, value => new AccountId(value))
            .IsRequired();

        builder.HasOne<Account>()
            .WithMany()
            .HasForeignKey(t => t.AccountId)
            .OnDelete(DeleteBehavior.Restrict);

        // Amount besteht aus zwei zusammengehörigen, aber identitätslosen Werten
        // (Betrag + Währung) -> EF-Core-"Complex Type" (seit EF Core 8), nicht OwnsOne.
        // Kein Schatten-Primärschlüssel, keine eigene Tracking-Identität - genau richtig
        // für ein Value Object, das nie unabhängig von seiner Transaction existiert.
        builder.ComplexProperty(t => t.Amount, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("Amount")
                .HasPrecision(18, 2);

            money.Property(m => m.Currency)
                .HasConversion(c => c.Value, v => new CurrencyCode(v))
                .HasColumnName("Currency")
                .HasMaxLength(3);
        });

        builder.Property(t => t.BookingDate).IsRequired();

        builder.Property(t => t.Description).HasMaxLength(Transaction.MaxDescriptionLength);

        builder.Property(t => t.TransactionType)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(t => t.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        // Häufigste Abfrage (Modul "Transaktionen suchen und analysieren"): Kontoauszug
        // eines Kontos über einen Zeitraum, meist absteigend nach Buchungsdatum sortiert.
        builder.HasIndex(t => new { t.AccountId, t.BookingDate });

        // Operativ wichtig: "alle fehlgeschlagenen/offenen Transaktionen" für Monitoring/Fehleranalyse.
        builder.HasIndex(t => t.Status);
    }
}
