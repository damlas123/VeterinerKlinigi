using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VeterinerKlinigi.Entities;
using VeterinerKlinigi.Entities.Constants;

namespace VeterinerKlinigi.Data.Configurations;

public class VaccinationConfiguration : IEntityTypeConfiguration<Vaccination>
{
    public void Configure(EntityTypeBuilder<Vaccination> builder)
    {
        // Sonraki doz tarihi boş olabilir; doluysa aşı tarihinden sonra olmalıdır.
        builder.ToTable("Vaccinations", t =>
            t.HasCheckConstraint("CK_Vaccinations_NextDoseDate",
                "[NextDoseDate] IS NULL OR [NextDoseDate] > [VaccinationDate]"));

        builder.HasKey(v => v.Id);

        builder.Property(v => v.VaccineName)
            .IsRequired()
            .HasMaxLength(FieldLengths.VaccineName);

        builder.Property(v => v.Description)
            .HasMaxLength(FieldLengths.VaccinationDescription);

        // Bir hayvanın aşı geçmişi tarih sırasıyla listelenir.
        builder.HasIndex(v => new { v.PetId, v.VaccinationDate });

        // "Yaklaşan aşılar" (NextDoseDate önümüzdeki 30 gün içinde) sorgusu için.
        builder.HasIndex(v => v.NextDoseDate);

        // Restrict: Aşı kaydı olan hayvan silinemez (sağlık geçmişi korunur; hayvan arşivlenir).
        builder.HasOne(v => v.Pet)
            .WithMany(p => p.Vaccinations)
            .HasForeignKey(v => v.PetId)
            .OnDelete(DeleteBehavior.Restrict);

        // SetNull: Uygulayan hekim silinirse aşı kaydı kalır, sadece hekim alanı boşalır.
        builder.HasOne(v => v.Veterinarian)
            .WithMany(vet => vet.Vaccinations)
            .HasForeignKey(v => v.VeterinarianId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
