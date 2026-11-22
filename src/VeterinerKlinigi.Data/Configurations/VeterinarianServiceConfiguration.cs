using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VeterinerKlinigi.Entities;

namespace VeterinerKlinigi.Data.Configurations;

/// <summary>
/// Veteriner ↔ Hizmet çoka-çok ilişkisinin ara tablosu.
/// </summary>
public class VeterinarianServiceConfiguration : IEntityTypeConfiguration<VeterinarianService>
{
    public void Configure(EntityTypeBuilder<VeterinarianService> builder)
    {
        builder.ToTable("VeterinarianServices");

        // Bileşik (composite) birincil anahtar: aynı hekim-hizmet eşleşmesi iki kez eklenemez.
        builder.HasKey(vs => new { vs.VeterinarianId, vs.ServiceId });

        // Cascade: Hekim veya hizmet silinirse eşleşme kaydı da silinir.
        // Bu kayıt tek başına anlam taşımadığı için Cascade uygundur.
        builder.HasOne(vs => vs.Veterinarian)
            .WithMany(v => v.VeterinarianServices)
            .HasForeignKey(vs => vs.VeterinarianId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(vs => vs.Service)
            .WithMany(s => s.VeterinarianServices)
            .HasForeignKey(vs => vs.ServiceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
