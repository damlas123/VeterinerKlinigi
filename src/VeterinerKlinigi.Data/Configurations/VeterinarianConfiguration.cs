using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VeterinerKlinigi.Entities;
using VeterinerKlinigi.Entities.Constants;

namespace VeterinerKlinigi.Data.Configurations;

public class VeterinarianConfiguration : IEntityTypeConfiguration<Veterinarian>
{
    public void Configure(EntityTypeBuilder<Veterinarian> builder)
    {
        builder.ToTable("Veterinarians");

        builder.HasKey(v => v.Id);

        builder.Property(v => v.UserId)
            .IsRequired();

        builder.Property(v => v.FullName)
            .IsRequired()
            .HasMaxLength(FieldLengths.FullName);

        builder.Property(v => v.Specialty)
            .IsRequired()
            .HasMaxLength(FieldLengths.Specialty);

        builder.Property(v => v.Biography)
            .HasMaxLength(FieldLengths.Biography);

        // Bir kullanıcı hesabına en fazla bir hekim kaydı bağlanabilir.
        builder.HasIndex(v => v.UserId)
            .IsUnique();

        // Bire-bir: ApplicationUser (1) ── (0..1) Veterinarian
        // Restrict: Hekim kaydı olan bir kullanıcı silinemez.
        builder.HasOne(v => v.User)
            .WithOne(u => u.Veterinarian)
            .HasForeignKey<Veterinarian>(v => v.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Bire-çok: Clinic (1) ── (*) Veterinarian
        // Restrict: Hekimi olan bir klinik silinemez.
        builder.HasOne(v => v.Clinic)
            .WithMany(c => c.Veterinarians)
            .HasForeignKey(v => v.ClinicId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
