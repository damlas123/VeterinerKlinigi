// =====================================================================
// PatiVet — Chart.js yardımcıları (dashboard ve rapor sayfaları)
// Grafik verisi sunucudan JSON olarak gelir; burada sadece çizilir.
// =====================================================================

window.PatiCharts = (function () {
    var palette = ['#2e9e6b', '#f2a65a', '#5aa9d6', '#9b7fd1', '#e07a7a', '#8fbfa9', '#c9b458'];
    var money = new Intl.NumberFormat('tr-TR', { style: 'currency', currency: 'TRY', maximumFractionDigits: 0 });
    var charts = {};

    Chart.defaults.font.family = getComputedStyle(document.body).fontFamily;
    Chart.defaults.color = '#5b7a70';

    // Aynı canvas'a tekrar çizilirken (rapor yenileme) eski grafik yok edilir.
    function draw(canvasId, config) {
        if (charts[canvasId]) { charts[canvasId].destroy(); }
        charts[canvasId] = new Chart(document.getElementById(canvasId), config);
        return charts[canvasId];
    }

    function emptyText(canvasId, items) {
        if (items && items.length > 0) { return false; }
        $('#' + canvasId).replaceWith('<p class="text-body-secondary small text-center pt-5">Henüz veri yok.</p>');
        return true;
    }

    return {
        // Aylık randevu sayısı (çubuk) + ciro (çizgi), iki eksenli
        monthly: function (canvasId, items) {
            if (emptyText(canvasId, items)) { return; }
            draw(canvasId, {
                data: {
                    labels: items.map(function (x) { return x.month; }),
                    datasets: [
                        { type: 'bar', label: 'Randevu', data: items.map(function (x) { return x.appointmentCount; }), backgroundColor: '#cfeadd', borderRadius: 8, yAxisID: 'y' },
                        { type: 'line', label: 'Ciro', data: items.map(function (x) { return x.revenue; }), borderColor: '#2e9e6b', backgroundColor: '#2e9e6b', tension: 0.35, yAxisID: 'y1' }
                    ]
                },
                options: {
                    maintainAspectRatio: false,
                    interaction: { mode: 'index', intersect: false },
                    scales: {
                        y: { beginAtZero: true, ticks: { precision: 0 } },
                        y1: { beginAtZero: true, position: 'right', grid: { drawOnChartArea: false }, ticks: { callback: function (v) { return money.format(v); } } }
                    },
                    plugins: { tooltip: { callbacks: { label: function (c) { return c.dataset.label + ': ' + (c.dataset.yAxisID === 'y1' ? money.format(c.raw) : c.raw); } } } }
                }
            });
        },

        // { label, count } listesinden halka grafik
        doughnut: function (canvasId, items) {
            if (emptyText(canvasId, items)) { return; }
            draw(canvasId, {
                type: 'doughnut',
                data: {
                    labels: items.map(function (x) { return x.label; }),
                    datasets: [{ data: items.map(function (x) { return x.count; }), backgroundColor: palette, borderWidth: 2, borderColor: '#ffffff' }]
                },
                options: { maintainAspectRatio: false, cutout: '62%', plugins: { legend: { position: 'bottom' } } }
            });
        },

        // { label, count } listesinden çubuk grafik
        bar: function (canvasId, items, label) {
            draw(canvasId, {
                type: 'bar',
                data: {
                    labels: items.map(function (x) { return x.label; }),
                    datasets: [{ label: label, data: items.map(function (x) { return x.count; }), backgroundColor: '#2e9e6b', borderRadius: 8 }]
                },
                options: { maintainAspectRatio: false, plugins: { legend: { display: false } }, scales: { y: { beginAtZero: true, ticks: { precision: 0 } } } }
            });
        },

        // Hizmet bazında ciro (yatay çubuk)
        revenue: function (canvasId, items) {
            draw(canvasId, {
                type: 'bar',
                data: {
                    labels: items.map(function (x) { return x.serviceName; }),
                    datasets: [{ label: 'Ciro', data: items.map(function (x) { return x.totalRevenue; }), backgroundColor: palette, borderRadius: 8 }]
                },
                options: {
                    indexAxis: 'y',
                    maintainAspectRatio: false,
                    plugins: { legend: { display: false }, tooltip: { callbacks: { label: function (c) { return money.format(c.raw); } } } },
                    scales: { x: { beginAtZero: true, ticks: { callback: function (v) { return money.format(v); } } } }
                }
            });
        }
    };
})();
