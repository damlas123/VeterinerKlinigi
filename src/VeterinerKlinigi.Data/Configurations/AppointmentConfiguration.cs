using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VeterinerKlinigi.Entities;
using VeterinerKlinigi.Entities.Constants;

namespace VeterinerKlinigi.Data.Configurations;

public class AppointmentConfiguration : IEntityTypeConfiguration<Appointment>
{
    public void Configure(EntityTypeBuilder<Appointment> builder)
    {
        builder.ToTable("Appointments", t =>
        {
            t.HasCheckConstraint("CK_Appointments_Duration", "[Duration] > 0");
            t.HasCheckConstraint("CK_Appointments_Price", "[Price] >= 0");
        });

        builder.HasKey(a => a.Id);

        builder.Property(a => a.OwnerId)
            .IsRequired();

        builder.Property(a => a.Price)
            .HasPrecision(10, 2);

        // "Pending", "Approved" ... şeklinde metin olarak saklanır.
        builder.Property(a => a.Status)
            .HasConversion<string>()
            .HasMaxLength(FieldLengths.EnumText);

        builder.Property(a => a.OwnerNote)
            .HasMaxLength(FieldLengths.Note);

        builder.Property(a => a.RejectionReason)
            .HasMaxLength(FieldLengths.Note);

        // EndDate hesaplanan bir özelliktir, kolon olarak oluşturulmaz.
        builder.Ignore(a => a.EndDate);

        // ---------- Index'ler ----------
        // En kritik index: çakışma kontrolü ve uygun saat hesaplama
        // "bu hekimin bu günkü randevuları" sorgusunu sürekli çalıştırır.
        builder.HasIndex(a => new { a.VeterinarianId, a.AppointmentDate });

        // "Randevularım" (üye) listesi.
        builder.HasIndex(a => new { a.OwnerId, a.AppointmentDate });

        // Aynı hayvanın çakışan randevusu kontrolü ve hayvanın randevu geçmişi.
        builder.HasIndex(a => new { a.PetId, a.AppointmentDate });

        // Durum filtreleri (Bekleyen randevular vb.).
        builder.HasIndex(a => a.Status);

        // ---------- İlişkiler ----------
        // Hepsi Restrict: Randevusu olan hayvan, kullanıcı, hekim veya hizmet silinemez.
        // Ayrıca SQL Server, aynı tabloya birden fazla Cascade yolu olmasına izin vermez
        // (ör. Kullanıcı → Pet → Appointment ve Kullanıcı → Appointment).
        builder.HasOne(a => a.Pet)
            .WithMany(p => p.Appointments)
            .HasForeignKey(a => a.PetId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.Owner)
            .WithMany(u => u.Appointments)
            .HasForeignKey(a => a.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.Veterinarian)
            .WithMany(v => v.Appointments)
            .HasForeignKey(a => a.VeterinarianId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.Service)
            .WithMany(s => s.Appointments)
            .HasForeignKey(a => a.ServiceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
