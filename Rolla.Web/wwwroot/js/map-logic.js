// ۱. تنظیمات نقشه
var map = L.map('map').setView([35.71, 51.41], 13);

L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
    attribution: '© OpenStreetMap'
}).addTo(map);

var driverMarkers = {};

// ۲. دریافت رانندگان
async function fetchNearbyDrivers() {
    try {
        const response = await fetch('/api/TripApi/nearby?lat=35.71&lng=51.41');
        const data = await response.json();

        document.getElementById('driver-count').innerText = `راننده‌های فعال: ${data.driverIds.length}`;

        data.driverIds.forEach(id => {
            if (!driverMarkers[id]) {
                driverMarkers[id] = L.marker([35.71, 51.41]).addTo(map)
                    .bindPopup(`راننده: ${id}`);
            }
        });
    } catch (err) {
        console.error("خطا در API:", err);
    }
}

setInterval(fetchNearbyDrivers, 5000);
fetchNearbyDrivers();

// ۳. اتصال به SignalR (اصلاح شده)
const connection = new signalR.HubConnectionBuilder() // کلمه signalR اضافه شد
    .withUrl("/rideHub")
    .build();

connection.on("ReceiveNewTripRequest", function (trip) {
    alert("سفر جدید ثبت شد! قیمت: " + trip.price);
});

connection.start()
    .then(() => {
        document.getElementById('status-text').innerText = "متصل به شبکه رولا ✅";
        document.getElementById('status-text').className = "text-success";
    })
    .catch(err => {
        console.error("خطا در سیگنال آر:", err);
        document.getElementById('status-text').innerText = "خطا در اتصال ❌";
    });