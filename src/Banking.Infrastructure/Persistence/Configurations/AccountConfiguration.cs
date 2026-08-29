using Banking.Domain.Accounts;
using Banking.Domain.Common;
using Banking.Domain.Customers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Banking.Infrastructure.Persistence.Configurations;

public sealed class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.ToTable("Accounts");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id)
            .HasConversion(id => id.Value, value => new AccountId(value))
            .ValueGeneratedNever();

        builder.Property(a => a.AccountNumber)
            .HasConversion(n => n.Value, v => new AccountNumber(v))
            .HasMaxLength(AccountNumber.MaxLength)
            .IsRequired();

        // Eindeutige Kontonummer über die gesamte Bank hinweg.
        builder.HasIndex(a => a.AccountNumber)
            .IsUnique();

        builder.Property(a => a.CustomerId)
            .HasConversion(id => id.Value, value => new CustomerId(value))
            .IsRequired();

        // Häufigste Abfrage: "alle Konten eines Kunden".
        builder.HasIndex(a => a.CustomerId);

        // FK existiert auf DB-Ebene für referenzielle Integrität, obwohl das Domain-Aggregat
        // bewusst keine Navigation auf Customer hält (siehe docs/architecture/overview.md).
        builder.HasOne<Customer>()
            .WithMany()
            .HasForeignKey(a => a.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(a => a.AccountType)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(a => a.Currency)
            .HasConversion(c => c.Value, v => new CurrencyCode(v))
            .HasMaxLength(3)
            .IsRequired();

        // Balance ist im Domain-Modell eine private, berechnete Property (nur über
        // Debit/Credit veränderbar) - EF Core kann sie deshalb nicht per Lambda
        // ansprechen. Zugriff auf die dahinterliegende private Property per Namen.
        builder.Ignore(a => a.Balance);
        builder.Property<decimal>("BalanceAmount")
            .HasColumnName("Balance")
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(a => a.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        // Optimistic Concurrency: zwei gleichzeitige Buchungen auf dasselbe Konto sind
        // der klassische Lost-Update-Fall - hier ist ein Konflikt real und teuer genug,
        // um ihn explizit abzusichern (anders als z. B. bei AuditLogEntry, das nie geändert wird).
        builder.Property<byte[]>("RowVersion")
            .IsRowVersion();
    }
}
