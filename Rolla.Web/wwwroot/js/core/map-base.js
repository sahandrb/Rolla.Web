var map;
var userMarker;

function initMap(lat = 35.71, lng = 51.41, zoom = 13) {
    if (map) return map;

    // پیدا کردن تگ نقشه در HTML
    var mapContainer = document.getElementById('map');
    if (!mapContainer) {
        console.error("تگ با آیدی map در صفحه وجود ندارد!");
        return null;
    }

    map = L.map('map').setView([lat, lng], zoom);

    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        attribution: '© OpenStreetMap'
    }).addTo(map);

    // آپدیت کردن سایز نقشه بعد از لود شدن برای جلوگیری از خاکستری شدن
    setTimeout(function () {
        map.invalidateSize();
    }, 500);

    return map;
}

function addMarker(lat, lng, popupText) {
    if (!map) return;
    return L.marker([lat, lng]).addTo(map).bindPopup(popupText).openPopup();
}