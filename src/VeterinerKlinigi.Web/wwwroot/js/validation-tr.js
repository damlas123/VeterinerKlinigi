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
