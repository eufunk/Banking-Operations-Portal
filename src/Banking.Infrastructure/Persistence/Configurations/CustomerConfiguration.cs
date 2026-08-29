using Banking.Domain.Customers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Banking.Infrastructure.Persistence.Configurations;

public sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("Customers");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id)
            .HasConversion(id => id.Value, value => new CustomerId(value))
            .ValueGeneratedNever();

        builder.Property(c => c.CustomerNumber)
            .HasConversion(n => n.Value, v => new CustomerNumber(v))
            .HasMaxLength(CustomerNumber.MaxLength)
            .IsRequired();

        // Eindeutige Kundennummer: fachliche Identität nach außen, unabhängig vom technischen Id (Guid).
        builder.HasIndex(c => c.CustomerNumber)
            .IsUnique();

        builder.Property(c => c.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(c => c.LastName).HasMaxLength(100).IsRequired();

        builder.Property(c => c.Email)
            .HasConversion(e => e.Value, v => new Email(v))
            .HasMaxLength(Email.MaxLength)
            .IsRequired();

        // Häufige Suchoperationen (Modul "Kunden verwalten und suchen"): nach E-Mail oder Name filtern.
        builder.HasIndex(c => c.Email);
        builder.HasIndex(c => new { c.LastName, c.FirstName });

        // Enum als string statt int: verhindert, dass eine spätere Umsortierung der
        // Enum-Werte stillschweigend die Bedeutung bereits gespeicherter Datensätze ändert.
        builder.Property(c => c.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(c => c.CreatedAt).IsRequired();
    }
}
