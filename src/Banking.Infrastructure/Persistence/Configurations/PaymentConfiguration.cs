using Banking.Domain.Accounts;
using Banking.Domain.Common;
using Banking.Domain.Payments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Banking.Infrastructure.Persistence.Configurations;

public sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("Payments");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id)
            .HasConversion(id => id.Value, value => new PaymentId(value))
            .ValueGeneratedNever();

        builder.Property(p => p.SourceAccountId)
            .HasConversion(id => id.Value, value => new AccountId(value))
            .IsRequired();

        builder.HasIndex(p => p.SourceAccountId);

        builder.HasOne<Account>()
            .WithMany()
            .HasForeignKey(p => p.SourceAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        // Bewusst kein FK: Ziel kann eine bankfremde, externe IBAN sein, die in
        // unserem Accounts-Bestand gar nicht existiert.
        builder.Property(p => p.TargetAccountNumber)
            .HasConversion(n => n.Value, v => new AccountNumber(v))
            .HasColumnName("TargetAccountNumber")
            .HasMaxLength(AccountNumber.MaxLength)
            .IsRequired();

        builder.ComplexProperty(p => p.Amount, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("Amount")
                .HasPrecision(18, 2);

            money.Property(m => m.Currency)
                .HasConversion(c => c.Value, v => new CurrencyCode(v))
                .HasColumnName("Currency")
                .HasMaxLength(3);
        });

        builder.Property(p => p.Reference)
            .HasConversion(r => r.Value, v => new PaymentReference(v))
            .HasMaxLength(PaymentReference.MaxLength)
            .IsRequired();

        builder.Property(p => p.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        // Zahlungsstatus verfolgen: häufigster Filter im Payments-Dashboard
        // (z. B. "alle PendingApproval").
        builder.HasIndex(p => p.Status);

        builder.Property(p => p.CreatedAt).IsRequired();

        // Optimistic Concurrency: Der Freigabe-Workflow (Draft -> PendingApproval ->
        // Approved -> ...) kann von mehreren Sachbearbeitern gleichzeitig angestoßen
        // werden - ein doppeltes Approve/Reject wäre fachlich kritisch.
        builder.Property<byte[]>("RowVersion")
            .IsRowVersion();
    }
}
