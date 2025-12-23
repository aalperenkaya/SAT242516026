"use strict";

(() => {
    const barCharts = {};
    const lineCharts = {};

    function ensurePlugins() {
        if (window.Chart && window.ChartDataLabels && !window.__datalabels_registered) {
            try { window.Chart.register(window.ChartDataLabels); } catch (e) { }
            window.__datalabels_registered = true;
        }
    }

    window.drawBarChart = (chartid, labels, seriesList, responsive, animation, indexAxis) => {
        ensurePlugins();

        const canvas = document.getElementById(chartid);
        if (!canvas || !window.Chart) return;

        const ctx = canvas.getContext("2d");

        if (barCharts[chartid]) {
            barCharts[chartid].destroy();
            delete barCharts[chartid];
        }

        const datasets = (seriesList || []).map(s => ({
            label: s.label,
            data: s.data,
            backgroundColor: s.backgroundColor,
            borderColor: s.borderColor,
            borderWidth: s.borderWidth ?? 1
        }));

        barCharts[chartid] = new window.Chart(ctx, {
            type: "bar",
            data: { labels: labels || [], datasets },
            options: {
                responsive: responsive ?? true,
                animation: animation ?? true,
                maintainAspectRatio: false,
                indexAxis: indexAxis || "x",
                scales: {
                    x: { beginAtZero: true },
                    y: { beginAtZero: true }
                },
                plugins: {
                    legend: { position: "bottom" }
                }
            }
        });
    };

    window.drawLineChart = (chartid, labels, seriesList, responsive, animation, fill, tension) => {
        ensurePlugins();

        const canvas = document.getElementById(chartid);
        if (!canvas || !window.Chart) return;

        const ctx = canvas.getContext("2d");

        if (lineCharts[chartid]) {
            lineCharts[chartid].destroy();
            delete lineCharts[chartid];
        }

        const datasets = (seriesList || []).map(s => ({
            label: s.label,
            data: s.data,
            backgroundColor: s.backgroundColor,
            borderColor: s.borderColor,
            borderWidth: s.borderWidth ?? 2,
            fill: s.fill ?? (fill ?? false),
            tension: s.tension ?? (tension ?? 0.2)
        }));

        lineCharts[chartid] = new window.Chart(ctx, {
            type: "line",
            data: { labels: labels || [], datasets },
            options: {
                responsive: responsive ?? true,
                animation: animation ?? true,
                maintainAspectRatio: false,
                scales: {
                    y: { beginAtZero: true }
                },
                plugins: {
                    legend: { position: "bottom" }
                }
            }
        });
    };
})();

