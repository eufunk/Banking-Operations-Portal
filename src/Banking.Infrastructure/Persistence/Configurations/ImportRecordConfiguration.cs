using Banking.Domain.Imports;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Banking.Infrastructure.Persistence.Configurations;

public sealed class ImportRecordConfiguration : IEntityTypeConfiguration<ImportRecord>
{
    public void Configure(EntityTypeBuilder<ImportRecord> builder)
    {
        builder.ToTable("ImportRecords");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id)
            .HasConversion(id => id.Value, value => new ImportRecordId(value))
            .ValueGeneratedNever();

        builder.Property(r => r.ImportJobId)
            .HasConversion(id => id.Value, value => new ImportJobId(value))
            .IsRequired();

        builder.Property(r => r.Outcome)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(r => r.ExternalTransactionId)
            .HasMaxLength(100);

        // Über diesen Index läuft die Idempotenz-Prüfung (IImportJobRepository.ExistsSuccessfulRecordAsync).
        builder.HasIndex(r => r.ExternalTransactionId);

        builder.Property(r => r.ErrorMessage)
            .HasMaxLength(1000);
    }
}
