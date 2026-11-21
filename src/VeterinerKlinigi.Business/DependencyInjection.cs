using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace VeterinerKlinigi.Business;

/// <summary>
/// İş katmanındaki servislerin Dependency Injection kayıtları.
/// Program.cs bu metodu çağırır; böylece Web projesi Business'ın iç yapısını bilmek zorunda kalmaz.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddBusinessLayer(this IServiceCollection services)
    {
        // TimeProvider: .NET'in "şu an saat kaç?" sorusunu cevaplayan sınıfı.
        // DateTime.Now yerine bunu kullanırız; çünkü testlerde sahte bir saat verebiliriz.
        // Randevu kurallarındaki "geçmiş tarihe randevu verilmez" kontrolü de bu sınıfı kullanır.
        services.TryAddSingleton(TimeProvider.System);

        return services;
    }
}
