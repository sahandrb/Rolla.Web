// wwwroot/js/core/map-base.js
var map; // استفاده از var برای دسترسی سراسری
var userMarker;

function initMap(lat = 35.71, lng = 51.41, zoom = 13) {
    // اگر نقشه قبلاً ساخته شده، دوباره نساز
    if (map) return map;

    map = L.map('map').setView([lat, lng], zoom);

    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        attribution: '© OpenStreetMap'
    }).addTo(map);

    // یک ترفند برای اطمینان از رندر شدن نقشه بعد از لود صفحه
    setTimeout(function () {
        map.invalidateSize();
    }, 400);

    return map;
}

function addMarker(lat, lng, popupText) {
    if (!map) return;
    return L.marker([lat, lng]).addTo(map).bindPopup(popupText).openPopup();
}