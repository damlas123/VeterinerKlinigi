using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VeterinerKlinigi.Entities;
using VeterinerKlinigi.Entities.Constants;

namespace VeterinerKlinigi.Data.Configurations;

/// <summary>
/// AspNetUsers tablosuna eklediğimiz alanların ayarları.
/// Tablonun geri kalanını Identity (base.OnModelCreating) ayarlar.
/// </summary>
public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.Property(u => u.FirstName)
            .IsRequired()
            .HasMaxLength(FieldLengths.PersonName);

        builder.Property(u => u.LastName)
            .IsRequired()
            .HasMaxLength(FieldLengths.PersonName);

        // FullName hesaplanan bir özelliktir, kolon olarak oluşturulmaz.
        builder.Ignore(u => u.FullName);
    }
}
