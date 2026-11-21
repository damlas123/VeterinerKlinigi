// =====================================================================
// PatiVet — Tüm sayfalarda çalışan ortak JavaScript kodları (jQuery)
// =====================================================================

$(function () {
    // Bulunulan sayfanın menü bağlantısını "aktif" olarak işaretle.
    // Örn. /Home/Index sayfasındayken "Ana Sayfa" bağlantısı vurgulanır.
    var currentPath = window.location.pathname.toLowerCase().replace(/\/index$/, "") || "/";
    $(".pv-navbar .nav-link").each(function () {
        var linkPath = ($(this).attr("href") || "").toLowerCase().replace(/\/index$/, "") || "/";
        if (linkPath === currentPath || (linkPath === "/" && currentPath === "/home")) {
            $(this).addClass("active").attr("aria-current", "page");
        }
    });

    // Bootstrap tooltip'lerini etkinleştir: title açıklaması olan öğelerde
    // data-bs-toggle="tooltip" yazmak yeterlidir.
    $('[data-bs-toggle="tooltip"]').each(function () {
        bootstrap.Tooltip.getOrCreateInstance(this);
    });

    // İşlem sonrası gösterilen bilgi mesajları (alert) 6 saniye sonra kendiliğinden kapansın.
    // Hata mesajları (alert-danger) kullanıcı okuyabilsin diye otomatik kapanmaz.
    setTimeout(function () {
        $(".alert.alert-dismissible").not(".alert-danger").each(function () {
            bootstrap.Alert.getOrCreateInstance(this).close();
        });
    }, 6000);
});
