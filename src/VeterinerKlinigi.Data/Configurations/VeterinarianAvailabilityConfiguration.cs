using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VeterinerKlinigi.Entities;

namespace VeterinerKlinigi.Data.Configurations;

public class VeterinarianAvailabilityConfiguration : IEntityTypeConfiguration<VeterinarianAvailability>
{
    public void Configure(EntityTypeBuilder<VeterinarianAvailability> builder)
    {
        builder.ToTable("VeterinarianAvailabilities", t =>
            t.HasCheckConstraint("CK_VeterinarianAvailabilities_TimeRange", "[StartTime] < [EndTime]"));

        builder.HasKey(a => a.Id);

        // Uygun saat hesaplamasında "bu hekimin bu günkü aralıkları" sorgusu çok sık çalışır.
        builder.HasIndex(a => new { a.VeterinarianId, a.DayOfWeek });

        // Cascade: Hekim silinirse müsaitlik aralıkları da silinir.
        builder.HasOne(a => a.Veterinarian)
            .WithMany(v => v.Availabilities)
            .HasForeignKey(a => a.VeterinarianId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
