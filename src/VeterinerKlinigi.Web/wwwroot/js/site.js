// =====================================================================
// PatiVet — Tüm sayfalarda çalışan ortak JavaScript kodları (jQuery)
// =====================================================================

/**
 * Sağ üstte bildirim (toast) gösterir. Sayfa yenilenmeden yapılan işlemlerden sonra kullanılır.
 * Sunucudan gelen mesajlar (TempData) _StatusMessage.cshtml ile aynı kutuya basılır.
 * @param {string} message Gösterilecek metin (HTML olarak değil, düz metin olarak eklenir).
 * @param {"success"|"error"} type
 */
window.pvToast = function (message, type) {
    var isError = type === "error";
    var $toast = $('<div class="toast pv-toast" role="status"><div class="d-flex align-items-start gap-2 p-3">' +
        '<i class="bi pv-toast-icon"></i><div class="toast-body p-0 flex-grow-1"></div>' +
        '<button type="button" class="btn-close" data-bs-dismiss="toast" aria-label="Kapat"></button></div></div>');
    $toast.addClass(isError ? "pv-toast-error" : "pv-toast-success")
        .attr("role", isError ? "alert" : "status");
    $toast.find(".pv-toast-icon").addClass(isError ? "bi-exclamation-triangle-fill" : "bi-check-circle-fill");
    $toast.find(".toast-body").text(message); // .text(): XSS'e karşı güvenli
    $("#pvToasts").append($toast);
    bootstrap.Toast.getOrCreateInstance($toast[0], { autohide: !isError, delay: 5000 }).show();
    $toast.on("hidden.bs.toast", function () { $toast.remove(); });
};

/**
 * Mobil tablolar: küçük ekranda her satır kart gibi görünür (site.css → .pv-table).
 * Her hücreye sütun başlığı data-label olarak yazılır; CSS bu etiketi hücrenin solunda gösterir.
 * AJAX ile doldurulan tablolarda (ör. ciro raporu) doldurduktan sonra tekrar çağrılır.
 */
window.pvLabelTables = function (root) {
    $(root).find("table.pv-table").addBack("table.pv-table").each(function () {
        var headers = $(this).find("thead th").map(function () { return $(this).text().trim(); }).get();
        $(this).find("tbody tr, tfoot tr").each(function () {
            var column = 0;
            $(this).children("td, th").each(function () {
                var span = parseInt(this.getAttribute("colspan") || "1", 10);
                if (span === 1 && headers[column]) {
                    this.setAttribute("data-label", headers[column]);
                }
                column += span;
            });
        });
    });
};

$(function () {
    // ---------------------------------------------------------------
    // Menüde bulunulan sayfayı "aktif" işaretle
    // ---------------------------------------------------------------
    var currentPath = window.location.pathname.toLowerCase().replace(/\/index$/, "").replace(/\/$/, "") || "/";

    $(".pv-navbar .nav-link").each(function () {
        var linkPath = ($(this).attr("href") || "").toLowerCase().replace(/\/index$/, "") || "/";
        if (linkPath === currentPath || (linkPath === "/" && currentPath === "/home")) {
            $(this).addClass("active").attr("aria-current", "page");
        }
    });

    // Panel menüsü: /Admin/Clinics/Edit/3 gibi alt sayfalarda da ilgili menü öğesi aktif görünsün.
    // Dashboard bağlantısı panelin kök adresidir (/Admin); sadece kendisi için aktif olmalı,
    // aksi halde /Admin/... ile başlayan her sayfada Dashboard da vurgulanıyordu.
    var $sideLinks = $(".pv-side-nav .nav-link");
    var best = null;
    $sideLinks.each(function () {
        var linkPath = ($(this).attr("href") || "").toLowerCase().replace(/\/index$/, "").replace(/\/$/, "");
        var isDashboard = /\/dashboard$/.test(linkPath) || /^\/(admin|veterinarian|member)$/.test(linkPath);
        var root = linkPath.replace(/\/dashboard$/, "");
        var matches = isDashboard
            ? currentPath === root || currentPath === root + "/dashboard"
            : currentPath === linkPath || currentPath.indexOf(linkPath + "/") === 0;
        // Birden fazla bağlantı eşleşirse (ör. /Member/Appointments ve /Member/Appointments/Create) en uzun olanı seç.
        if (matches && (!best || linkPath.length > best.len)) {
            best = { el: this, len: linkPath.length };
        }
    });
    if (best) {
        $(best.el).addClass("active").attr("aria-current", "page");
        // Mobilde yatay kayan menüde aktif öğe görünür alana gelsin.
        var nav = $(best.el).closest(".pv-side-nav")[0];
        if (nav && nav.scrollWidth > nav.clientWidth) {
            nav.scrollLeft = best.el.offsetLeft - 16;
        }
    }

    // ---------------------------------------------------------------
    // Bildirimler (toast): sunucudan gelen mesajları göster
    // ---------------------------------------------------------------
    $("#pvToasts .toast").each(function () {
        bootstrap.Toast.getOrCreateInstance(this).show();
    });

    // ---------------------------------------------------------------
    // Onay penceresi: <form data-confirm="Emin misiniz?">
    // Tarayıcının sade confirm() kutusu yerine tasarıma uygun Bootstrap modal'ı kullanılır.
    // ---------------------------------------------------------------
    var confirmModalEl = document.getElementById("pvConfirmModal");
    var pendingForm = null;

    $(document).on("submit", "form[data-confirm]", function (e) {
        if (this.dataset.confirmed === "true") {
            return; // Kullanıcı onayladı, form normal şekilde gönderilsin.
        }

        e.preventDefault();
        e.stopImmediatePropagation();

        if (!confirmModalEl) {
            if (window.confirm($(this).data("confirm"))) {
                this.dataset.confirmed = "true";
                this.requestSubmit();
            }
            return;
        }

        pendingForm = { form: this, submitter: e.originalEvent && e.originalEvent.submitter };
        $("#pvConfirmText").text($(this).data("confirm"));
        $("#pvConfirmOk").text($(this).data("confirm-button") || "Evet, devam et");
        bootstrap.Modal.getOrCreateInstance(confirmModalEl).show();
    });

    $("#pvConfirmOk").on("click", function () {
        if (!pendingForm) {
            return;
        }

        var target = pendingForm;
        pendingForm = null;
        bootstrap.Modal.getOrCreateInstance(confirmModalEl).hide();
        target.form.dataset.confirmed = "true";
        target.form.requestSubmit(target.submitter || undefined);
    });

    // ---------------------------------------------------------------
    // Form gönderilirken butonu kilitle ve "işleniyor" göster (çift tıklama ile iki kez kayıt oluşmasın)
    // ---------------------------------------------------------------
    $(document).on("submit", "form", function (e) {
        var $form = $(this);
        if (e.isDefaultPrevented() || ($form.attr("method") || "get").toLowerCase() !== "post") {
            return;
        }

        // jQuery Validation varsa ve form geçersizse buton kilitlenmez (kullanıcı hatayı düzeltecek).
        if (typeof $form.valid === "function" && $form.data("validator") && !$form.valid()) {
            return;
        }

        var submitter = e.originalEvent && e.originalEvent.submitter;
        var $button = submitter ? $(submitter) : $form.find("button[type=submit]").first();
        if ($button.length && !$button.prop("disabled")) {
            // Butonun adı/değeri forma eklensin (disabled butonlar gönderilmez).
            if ($button.attr("name")) {
                $("<input type='hidden'>").attr("name", $button.attr("name")).val($button.val()).appendTo($form);
            }
            $button.data("original-html", $button.html())
                .prop("disabled", true)
                .html('<span class="spinner-border spinner-border-sm me-1" aria-hidden="true"></span>İşleniyor...');
        }
    });

    // Tarayıcıda "geri" ile dönüldüğünde (önbellekten) kilitli kalan butonları aç.
    $(window).on("pageshow", function () {
        $("button[type=submit]:disabled").each(function () {
            var original = $(this).data("original-html");
            if (original) {
                $(this).html(original).prop("disabled", false);
            }
        });
        $("form[data-confirm]").each(function () { delete this.dataset.confirmed; });
    });

    // ---------------------------------------------------------------
    // Tablolarda hızlı arama: <input data-table-search="#tabloId">
    // Sunucuya istek atmadan, yüklü satırlar içinde anında filtreler.
    // ---------------------------------------------------------------
    $(document).on("input", "[data-table-search]", function () {
        var term = $(this).val().toLocaleLowerCase("tr-TR").trim();
        var $table = $($(this).data("table-search"));
        var visible = 0;
        $table.find("tbody tr").not(".pv-search-empty").each(function () {
            var match = !term || $(this).text().toLocaleLowerCase("tr-TR").indexOf(term) !== -1;
            $(this).toggle(match);
            if (match) { visible++; }
        });

        var $empty = $table.find("tbody tr.pv-search-empty");
        if (!$empty.length) {
            var columns = $table.find("thead th").length || 1;
            $empty = $('<tr class="pv-search-empty"><td class="pv-empty"><i class="bi bi-search"></i>Aramanızla eşleşen kayıt yok.</td></tr>');
            $empty.find("td").attr("colspan", columns);
            $table.find("tbody").append($empty);
        }
        $empty.toggle(visible === 0);
    });

    // Mobil tablolar: küçük ekranda her satır kart gibi görünür (site.css → .pv-table).
    window.pvLabelTables(document);

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
});
