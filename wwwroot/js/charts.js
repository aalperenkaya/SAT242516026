// wwwroot/js/charts.js
// Chart.js lazým.
// CanvasId baþýna chart instance saklayýp tekrar çizimde destroy ediyoruz.

window._charts = window._charts || {};

window.drawBarChart = function (canvasId, labels, datasets, beginAtZero, responsive, indexAxis) {
    const el = document.getElementById(canvasId);
    if (!el) { console.error("Canvas not found:", canvasId); return; }
    if (!window.Chart) { console.error("Chart.js not loaded"); return; }

    if (window._charts[canvasId]) {
        window._charts[canvasId].destroy();
        window._charts[canvasId] = null;
    }

    const ctx = el.getContext("2d");

    window._charts[canvasId] = new Chart(ctx, {
        type: "bar",
        data: { labels: labels, datasets: datasets },
        options: {
            responsive: responsive !== false,
            maintainAspectRatio: false,
            indexAxis: indexAxis || "x",
            scales: {
                y: { beginAtZero: beginAtZero === true }
            }
        }
    });
};

window.drawLineChart = function (canvasId, labels, datasets, beginAtZero, responsive, showLegend, tension) {
    const el = document.getElementById(canvasId);
    if (!el) { console.error("Canvas not found:", canvasId); return; }
    if (!window.Chart) { console.error("Chart.js not loaded"); return; }

    if (window._charts[canvasId]) {
        window._charts[canvasId].destroy();
        window._charts[canvasId] = null;
    }

    // dataset içine tension gelmediyse paramdan bas
    if (datasets && datasets.length) {
        for (const ds of datasets) {
            if (typeof ds.tension === "undefined" && typeof tension !== "undefined") {
                ds.tension = tension;
            }
        }
    }

    const ctx = el.getContext("2d");

    window._charts[canvasId] = new Chart(ctx, {
        type: "line",
        data: { labels: labels, datasets: datasets },
        options: {
            responsive: responsive !== false,
            maintainAspectRatio: false,
            plugins: {
                legend: { display: showLegend !== false }
            },
            scales: {
                y: { beginAtZero: beginAtZero === true }
            }
        }
    });
};
