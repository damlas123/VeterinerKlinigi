using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VeterinerKlinigi.Entities;
using VeterinerKlinigi.Entities.Constants;

namespace VeterinerKlinigi.Data.Configurations;

public class ClinicConfiguration : IEntityTypeConfiguration<Clinic>
{
    public void Configure(EntityTypeBuilder<Clinic> builder)
    {
        // CHECK kısıtı: Kodda bir hata olsa bile veritabanı, kapanış saati açılıştan
        // önce olan bir klinik kaydını kabul etmez (ikinci savunma hattı).
        builder.ToTable("Clinics", t =>
            t.HasCheckConstraint("CK_Clinics_WorkingHours", "[OpeningTime] < [ClosingTime]"));

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(FieldLengths.ClinicName);

        builder.Property(c => c.Address)
            .IsRequired()
            .HasMaxLength(FieldLengths.Address);

        builder.Property(c => c.Phone)
            .IsRequired()
            .HasMaxLength(FieldLengths.Phone);

        builder.Property(c => c.Email)
            .HasMaxLength(FieldLengths.Email);

        builder.Property(c => c.Description)
            .HasMaxLength(FieldLengths.ClinicDescription);
    }
}
