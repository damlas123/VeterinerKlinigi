using System.ComponentModel.DataAnnotations;

namespace VeterinerKlinigi.Entities.Enums;

/// <summary>
/// Evcil hayvan türü. Veritabanında metin olarak saklanır ("Cat", "Dog" ...).
/// </summary>
public enum PetSpecies
{
    [Display(Name = "Kedi")]
    Cat = 0,

    [Display(Name = "Köpek")]
    Dog = 1,

    [Display(Name = "Kuş")]
    Bird = 2,

    [Display(Name = "Tavşan")]
    Rabbit = 3,

    [Display(Name = "Kemirgen")]
    Rodent = 4,

    [Display(Name = "Sürüngen")]
    Reptile = 5,

    [Display(Name = "Diğer")]
    Other = 6
}
