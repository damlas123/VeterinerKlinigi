using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VeterinerKlinigi.Entities;
using VeterinerKlinigi.Entities.Constants;

namespace VeterinerKlinigi.Data.Configurations;

public class ServiceConfiguration : IEntityTypeConfiguration<Service>
{
    public void Configure(EntityTypeBuilder<Service> builder)
    {
        builder.ToTable("Services", t =>
        {
            // Süre 5 dakika ile 8 saat (480 dk) arasında olmalı.
            t.HasCheckConstraint("CK_Services_Duration", "[Duration] BETWEEN 5 AND 480");
            // Ücret negatif olamaz.
            t.HasCheckConstraint("CK_Services_Price", "[Price] >= 0");
        });

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Name)
            .IsRequired()
            .HasMaxLength(FieldLengths.ServiceName);

        builder.Property(s => s.Description)
            .HasMaxLength(FieldLengths.ServiceDescription);

        // decimal(10,2): en fazla 99.999.999,99 TL; kuruş hassasiyeti.
        builder.Property(s => s.Price)
            .HasPrecision(10, 2);

        // Aynı klinikte aynı isimde iki AKTİF hizmet olamaz.
        // Filtreli index: arşivlenmiş (IsActive = 0) hizmetler bu kurala dahil değildir.
        builder.HasIndex(s => new { s.ClinicId, s.Name })
            .IsUnique()
            .HasFilter("[IsActive] = 1");

        // Bire-çok: Clinic (1) ── (*) Service
        builder.HasOne(s => s.Clinic)
            .WithMany(c => c.Services)
            .HasForeignKey(s => s.ClinicId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
