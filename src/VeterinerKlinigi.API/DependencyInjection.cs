using Microsoft.Extensions.DependencyInjection;

namespace VeterinerKlinigi.API;

/// <summary>
/// API katmanının dışarıya açtığı kayıt metotları.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Bu projedeki (VeterinerKlinigi.API) controller'ları MVC'ye tanıtır.
    /// ASP.NET Core varsayılan olarak controller'ları sadece çalışan projede (Web) arar;
    /// AddApplicationPart ile "bu derlemedeki controller'ları da kullan" demiş oluruz.
    /// </summary>
    public static IMvcBuilder AddVeterinerKlinigiApi(this IMvcBuilder mvcBuilder)
    {
        mvcBuilder.AddApplicationPart(typeof(DependencyInjection).Assembly);
        return mvcBuilder;
    }
}
