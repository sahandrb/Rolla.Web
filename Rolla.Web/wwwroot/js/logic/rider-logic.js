// اضافه کردن اتصال SignalR برای مسافر
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/rideHub")
    .withAutomaticReconnect()
    .build();

let driverMarker = null;

connection.start().then(() => {
    console.log("Rider Connected to SignalR ✅");
}).catch(err => console.error(err));

// گوش دادن به حرکت راننده (این همان متدی است که در RideHub نوشتید)
connection.on("ReceiveDriverLocation", function (lat, lng) {
    console.log("موقعیت راننده دریافت شد:", lat, lng);

    if (driverMarker) {
        driverMarker.setLatLng([lat, lng]); // حرکت دادن ماشین راننده روی نقشه مسافر
    } else {
        // ایجاد مارکر ماشین راننده برای اولین بار
        var carIcon = L.icon({
            iconUrl: '/img/car-icon.png', // یک آیکون ماشین در پوشه img بگذارید
            iconSize: [32, 32]
        });
        driverMarker = L.marker([lat, lng], { icon: carIcon }).addTo(map)
            .bindPopup("راننده شما").openPopup();
    }
});

// اصلاح تابع ارسال درخواست مسافر
async function submitRequest() {
    const o = originMarker.getLatLng();
    const d = destMarker.getLatLng();
    const price = document.getElementById('btn-request').getAttribute('data-price');

    const dto = {
        originLat: o.lat, originLng: o.lng,
        destinationLat: d.lat, destinationLng: d.lng,
        estimatedPrice: parseFloat(price)
    };

    const res = await fetch('/api/TripApi/request', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(dto)
    });

    if (res.ok) {
        const result = await res.json();
        const tripId = result.tripId; // آیدی سفری که ساخته شد

        alert("✅ درخواست ارسال شد! در حال جستجوی راننده...");

        // مسافر بلافاصله عضو گروه این سفر می‌شود تا به محض قبول راننده، پیام‌ها را بگیرد
        await connection.invoke("JoinTripGroup", tripId);

        document.getElementById('btn-request').innerText = "🔍 در حال جستجو...";
        document.getElementById('btn-request').disabled = true;
    }
}