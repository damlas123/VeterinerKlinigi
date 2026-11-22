using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VeterinerKlinigi.Entities;
using VeterinerKlinigi.Entities.Constants;

namespace VeterinerKlinigi.Data.Configurations;

public class ExaminationNoteConfiguration : IEntityTypeConfiguration<ExaminationNote>
{
    public void Configure(EntityTypeBuilder<ExaminationNote> builder)
    {
        builder.ToTable("ExaminationNotes");

        builder.HasKey(n => n.Id);

        builder.Property(n => n.Diagnosis)
            .IsRequired()
            .HasMaxLength(FieldLengths.Diagnosis);

        builder.Property(n => n.Treatment)
            .IsRequired()
            .HasMaxLength(FieldLengths.Treatment);

        builder.Property(n => n.Notes)
            .HasMaxLength(FieldLengths.ExaminationNotes);

        // Bir randevuya en fazla bir muayene notu.
        builder.HasIndex(n => n.AppointmentId)
            .IsUnique();

        // Bire-bir: Appointment (1) ── (0..1) ExaminationNote
        builder.HasOne(n => n.Appointment)
            .WithOne(a => a.ExaminationNote)
            .HasForeignKey<ExaminationNote>(n => n.AppointmentId)
            .OnDelete(DeleteBehavior.Restrict);

        // Bire-çok: Veterinarian (1) ── (*) ExaminationNote
        builder.HasOne(n => n.Veterinarian)
            .WithMany(v => v.ExaminationNotes)
            .HasForeignKey(n => n.VeterinarianId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
