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

    // Panel menüsü: /Admin/Clinics/Edit/3 gibi alt sayfalarda da ilgili menü öğesi aktif görünsün.
    $(".pv-side-nav .nav-link").each(function () {
        var linkPath = ($(this).attr("href") || "").toLowerCase().replace(/\/index$/, "");
        var isDashboard = /\/dashboard$/.test(linkPath);
        var matches = isDashboard
            ? currentPath === linkPath || currentPath === linkPath.replace(/\/dashboard$/, "")
            : currentPath === linkPath || currentPath.indexOf(linkPath + "/") === 0;
        if (matches) {
            $(this).addClass("active").attr("aria-current", "page");
        }
    });

    // Geri alınamaz işlemler (iptal, sil, kilitle) için onay penceresi:
    // <form data-confirm="Emin misiniz?"> şeklinde kullanılır.
    $(document).on("submit", "form[data-confirm]", function (e) {
        if (!window.confirm($(this).data("confirm"))) {
            e.preventDefault();
        }
    });

    // data-copy-target="#token" → hedef alanın değerini panoya kopyalar (API token sayfası).
    $(document).on("click", "[data-copy-target]", function () {
        var $button = $(this);
        var $target = $($button.data("copy-target"));
        navigator.clipboard.writeText($target.val() || $target.text()).then(function () {
            var original = $button.html();
            $button.html('<i class="bi bi-check2 me-1"></i>Kopyalandı');
            setTimeout(function () { $button.html(original); }, 2000);
        });
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

