// ۱. تنظیمات اولیه نقشه (تمرکز روی تهران)
var map = L.map('map').setView([35.71, 51.41], 13);

L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
    attribution: '© OpenStreetMap contributors'
}).addTo(map);

// لیستی برای نگه داشتن مارکرهای راننده‌ها
var driverMarkers = {};

// ۲. متد خواندن راننده‌ها از API (همان که تست کردیم)
async function fetchNearbyDrivers() {
    try {
        const response = await fetch('/api/TripApi/nearby?lat=35.71&lng=51.41');
        const data = await response.json();

        document.getElementById('driver-count').innerText = `راننده‌های فعال: ${data.count}`;

        // نمایش راننده‌ها روی نقشه
        data.driverIds.forEach(id => {
            if (!driverMarkers[id]) {
                // ایجاد مارکر جدید برای راننده (موقعیت فرضی در شروع)
                driverMarkers[id] = L.marker([35.71, 51.41]).addTo(map)
                    .bindPopup(`راننده: ${id}`);
            }
        });
    } catch (err) {
        console.error("خطا در دریافت لیست رانندگان:", err);
    }
}

// ۳. اجرای خودکار هر ۵ ثانیه یکبار
setInterval(fetchNearbyDrivers, 5000);
fetchNearbyDrivers();

// ۴. اتصال به SignalR (طبق فایل signalr-client تو)
const connection = new hubConnectionBuilder() // این بخش را بعدا کامل‌تر می‌کنیم
    .withUrl("/rideHub")
    .build();

connection.on("ReceiveNewTripRequest", function (trip) {
    alert("سفر جدید ثبت شد! قیمت: " + trip.price);
});

connection.start().then(() => {
    document.getElementById('status-text').innerText = "متصل به شبکه رولا ✅";
    document.getElementById('status-text').className = "text-success";
});