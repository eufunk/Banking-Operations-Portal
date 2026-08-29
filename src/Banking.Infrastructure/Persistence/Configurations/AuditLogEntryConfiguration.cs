using Banking.Domain.Auditing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Banking.Infrastructure.Persistence.Configurations;

public sealed class AuditLogEntryConfiguration : IEntityTypeConfiguration<AuditLogEntry>
{
    public void Configure(EntityTypeBuilder<AuditLogEntry> builder)
    {
        builder.ToTable("AuditLogEntries");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id)
            .HasConversion(id => id.Value, value => new AuditLogEntryId(value))
            .ValueGeneratedNever();

        // UserId/EntityId bleiben bewusst nackte Guids: Auditing ist cross-cutting und
        // darf keine Abhängigkeit auf die stark typisierten IDs der Fachmodule haben.
        builder.Property(a => a.UserId).IsRequired();
        builder.Property(a => a.EntityId).IsRequired();

        builder.Property(a => a.Action)
            .HasMaxLength(AuditLogEntry.MaxActionLength)
            .IsRequired();

        builder.Property(a => a.EntityType)
            .HasMaxLength(AuditLogEntry.MaxEntityTypeLength)
            .IsRequired();

        // Häufigste Abfrage: "zeige die Änderungshistorie zu genau dieser Entität".
        builder.HasIndex(a => new { a.EntityType, a.EntityId });

        // Chronologische Auswertung / Retention-Abfragen ("alle Einträge älter als X").
        builder.HasIndex(a => a.Timestamp);

        // "Was hat Benutzer X gemacht" - z. B. für Compliance-Nachfragen.
        builder.HasIndex(a => a.UserId);

        builder.Property(a => a.Timestamp).IsRequired();

        // OldValue/NewValue bewusst ohne HasMaxLength -> nvarchar(max), da hier
        // serialisierte Snapshots beliebiger Länge landen können.
        builder.Property(a => a.OldValue);
        builder.Property(a => a.NewValue);

        // Kein RowVersion: AuditLogEntry wird nach dem Insert nie wieder verändert,
        // ein Schreibkonflikt ist hier strukturell ausgeschlossen.
    }
}
