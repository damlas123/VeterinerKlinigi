namespace VeterinerKlinigi.Entities.Constants;

/// <summary>
/// Metin alanlarının en fazla karakter uzunlukları.
/// Hem veritabanı kolonları (Data katmanındaki Fluent API ayarları) hem de form
/// doğrulamaları (ViewModel/DTO'lardaki [StringLength]) bu sabitleri kullanır.
/// Böylece bir sınırı değiştirmek istediğimizde tek bir yeri değiştiririz.
/// </summary>
public static class FieldLengths
{
    // Kişi bilgileri
    public const int PersonName = 50;
    public const int FullName = 100;
    public const int Phone = 20;
    public const int Email = 100;
    public const int Address = 250;

    // Klinik
    public const int ClinicName = 100;
    public const int ClinicDescription = 1000;

    // Veteriner
    public const int Specialty = 100;
    public const int Biography = 2000;

    // Hizmet
    public const int ServiceName = 100;
    public const int ServiceDescription = 500;

    // Evcil hayvan
    public const int PetName = 50;
    public const int Breed = 50;
    public const int Url = 260;

    // Aşı
    public const int VaccineName = 100;
    public const int VaccinationDescription = 500;

    // Randevu ve muayene
    public const int Note = 500;
    public const int Diagnosis = 500;
    public const int Treatment = 1000;
    public const int ExaminationNotes = 2000;

    // Enum'ların veritabanında metin olarak saklandığı kolonlar ("Pending", "Cat" ...)
    public const int EnumText = 20;
}
