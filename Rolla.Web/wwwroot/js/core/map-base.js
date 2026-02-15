// wwwroot/js/core/map-base.js
let map;
let userMarker;

function initMap(lat = 35.71, lng = 51.41, zoom = 13) {
    map = L.map('map').setView([lat, lng], zoom);

    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        attribution: '© OpenStreetMap'
    }).addTo(map);

    return map;
}

// تابع کمکی برای گذاشتن مارکر
function addMarker(lat, lng, popupText, iconUrl = null) {
    // اینجا بعداً می‌توانیم آیکون ماشین یا مسافر را جدا کنیم
    return L.marker([lat, lng]).addTo(map).bindPopup(popupText);
}