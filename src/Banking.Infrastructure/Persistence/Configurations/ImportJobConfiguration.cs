using Banking.Domain.Imports;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Banking.Infrastructure.Persistence.Configurations;

public sealed class ImportJobConfiguration : IEntityTypeConfiguration<ImportJob>
{
    public void Configure(EntityTypeBuilder<ImportJob> builder)
    {
        builder.ToTable("ImportJobs");

        builder.HasKey(j => j.Id);
        builder.Property(j => j.Id)
            .HasConversion(id => id.Value, value => new ImportJobId(value))
            .ValueGeneratedNever();

        builder.Property(j => j.FileName)
            .HasMaxLength(260) // maximale Windows-Pfadlänge als praktikable Obergrenze
            .IsRequired();

        builder.Property(j => j.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        // Häufigste Abfrage: neueste Import-Läufe zuerst (siehe SearchAsync).
        builder.HasIndex(j => j.StartedAt);

        // Records ist eine reine Lese-Property (IReadOnlyCollection) ohne öffentlichen
        // Setter - EF Core materialisiert deshalb über das private Feld _records statt über
        // die Property selbst (Konvention: "_" + camelCase(Records) = "_records").
        builder.HasMany(j => j.Records)
            .WithOne()
            .HasForeignKey(r => r.ImportJobId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(j => j.Records).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
