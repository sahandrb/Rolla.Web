// wwwroot/js/logic/rider-logic.js

// تمام متغیرها را ابتدای فایل تعریف کن
let originMarker = null;
let destMarker = null;
let step = 1;
let driverMarker = null;

// ۱. اول نقشه را لود کن (قبل از هر کار دیگری)
document.addEventListener("DOMContentLoaded", function () {
    initMap(); // فراخوانی از map-base.js

    // ۲. تعریف رویداد کلیک بعد از لود نقشه
    map.on('click', function (e) {
        if (step === 1) {
            if (originMarker) map.removeLayer(originMarker);
            originMarker = addMarker(e.latlng.lat, e.latlng.lng, "مبدا شما");
            step = 2;
            alert("حالا مقصد را روی نقشه انتخاب کنید");
        }
        else if (step === 2) {
            if (destMarker) map.removeLayer(destMarker);
            destMarker = addMarker(e.latlng.lat, e.latlng.lng, "مقصد شما");
            calculatePrice();
            document.getElementById('btn-request').disabled = false;
        }
    });
});

// ۳. اتصال به SignalR را در یک بلاک جداگانه بگذار که اگر خطا داد نقشه را خراب نکند
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/rideHub")
    .withAutomaticReconnect()
    .build();

connection.start().catch(err => console.error("SignalR Connection Error: ", err));

connection.on("ReceiveDriverLocation", function (lat, lng) {
    if (driverMarker) {
        driverMarker.setLatLng([lat, lng]);
    } else {
        driverMarker = L.marker([lat, lng]).addTo(map).bindPopup("راننده").openPopup();
    }
});

// بقیه توابع (submitRequest و calculatePrice) همان قبلی باشند...