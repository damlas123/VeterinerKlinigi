// =====================================================================
// jQuery Validate için Türkçe sayı biçimi.
// Varsayılan kurallar ondalık sayıyı "4.5" biçiminde bekler; uygulama tr-TR kültüründe
// çalıştığı için sunucu "4,5" biçimini kullanır. İstemci doğrulaması da virgülü kabul etmeli.
// Bu dosya _ValidationScriptsPartial içinde, doğrulama kütüphanelerinden SONRA yüklenir.
// =====================================================================
if ($.validator) {
    $.validator.methods.number = function (value, element) {
        return this.optional(element) || /^-?\d+(?:[.,]\d+)?$/.test(value);
    };
    $.validator.methods.range = function (value, element, param) {
        var number = parseFloat(String(value).replace(",", "."));
        return this.optional(element) || (number >= param[0] && number <= param[1]);
    };
}

// HTML5 özniteliklerinden (min, max, step, type="email" ...) gelen kuralların varsayılan
// İngilizce mesajlarını Türkçeleştir. (DataAnnotations'tan gelen mesajlar zaten Türkçedir.)
if ($.validator) {
    $.extend($.validator.messages, {
        required: "Bu alan zorunludur.",
        email: "Lütfen geçerli bir e-posta adresi girin.",
        url: "Lütfen geçerli bir adres (URL) girin.",
        date: "Lütfen geçerli bir tarih girin.",
        number: "Lütfen geçerli bir sayı girin.",
        digits: "Lütfen sadece rakam girin.",
        equalTo: "Lütfen aynı değeri tekrar girin.",
        maxlength: $.validator.format("En fazla {0} karakter girebilirsiniz."),
        minlength: $.validator.format("En az {0} karakter girmelisiniz."),
        rangelength: $.validator.format("{0} ile {1} karakter arasında bir değer girin."),
        range: $.validator.format("{0} ile {1} arasında bir değer girin."),
        max: $.validator.format("En fazla {0} olabilir."),
        min: $.validator.format("En az {0} olmalıdır."),
        step: $.validator.format("Değer {0} ve katları olmalıdır.")
    });
}
