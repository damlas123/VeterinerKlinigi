using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VeterinerKlinigi.Entities;
using VeterinerKlinigi.Entities.Constants;

namespace VeterinerKlinigi.Data.Configurations;

public class PetConfiguration : IEntityTypeConfiguration<Pet>
{
    public void Configure(EntityTypeBuilder<Pet> builder)
    {
        builder.ToTable("Pets", t =>
            t.HasCheckConstraint("CK_Pets_Weight", "[Weight] > 0"));

        builder.HasKey(p => p.Id);

        builder.Property(p => p.OwnerId)
            .IsRequired();

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(FieldLengths.PetName);

        // Enum'u veritabanında sayı (0, 1) yerine metin ("Cat", "Dog") olarak saklar.
        builder.Property(p => p.Species)
            .HasConversion<string>()
            .HasMaxLength(FieldLengths.EnumText);

        builder.Property(p => p.Breed)
            .HasMaxLength(FieldLengths.Breed);

        // decimal(5,2): en fazla 999,99 kg.
        builder.Property(p => p.Weight)
            .HasPrecision(5, 2);

        builder.Property(p => p.PhotoUrl)
            .HasMaxLength(FieldLengths.Url);

        // (OwnerId, Name): "Evcil hayvanlarım" listesi ve MCP'deki asi_takvimi(hayvanAdi)
        // aracı bu index'i kullanır. OwnerId ile başladığı için sadece OwnerId'ye göre
        // yapılan sorgular da bu index'ten yararlanır.
        builder.HasIndex(p => new { p.OwnerId, p.Name });

        // Bire-çok: ApplicationUser (1) ── (*) Pet
        builder.HasOne(p => p.Owner)
            .WithMany(u => u.Pets)
            .HasForeignKey(p => p.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
