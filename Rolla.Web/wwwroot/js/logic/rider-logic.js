// wwwroot/js/logic/rider-logic.js

// اینجا فقط راننده‌های اطراف را می‌گیریم
async function fetchNearbyDrivers() {
    const center = map.getCenter();
    try {
        const response = await fetch(`/api/TripApi/nearby?lat=${center.lat}&lng=${center.lng}`);
        const data = await response.json();

        // منطق نمایش ماشین‌ها روی نقشه...
        // (فعلاً ساده نگه می‌داریم)
        console.log("Drivers nearby:", data.count);
    } catch (err) {
        console.error(err);
    }
}

initMap();
setInterval(fetchNearbyDrivers, 5000);