

let originMarker = null;
let destMarker = null;
let step = 1; // 1: انتخاب مبدا، 2: انتخاب مقصد

initMap(); // لود نقشه

map.on('click', function (e) {
    if (step === 1) {
        // انتخاب مبدا
        if (originMarker) map.removeLayer(originMarker);
        originMarker = addMarker(e.latlng.lat, e.latlng.lng, "مبدا", "green");
        step = 2;
        alert("حالا مقصد را انتخاب کنید");
    }
    else if (step === 2) {
        // انتخاب مقصد
        if (destMarker) map.removeLayer(destMarker);
        destMarker = addMarker(e.latlng.lat, e.latlng.lng, "مقصد", "red");

        // محاسبه قیمت
        calculatePrice();
        document.getElementById('btn-request').disabled = false;
    }
});

async function calculatePrice() {
    const o = originMarker.getLatLng();
    const d = destMarker.getLatLng();

    // فراخوانی API جدیدی که باید بسازیم
    const res = await fetch(`/api/TripApi/calculate?oLat=${o.lat}&oLng=${o.lng}&dLat=${d.lat}&dLng=${d.lng}`);
    const data = await res.json();

    document.getElementById('price-display').innerText = data.price.toLocaleString() + " تومان";
    // ذخیره قیمت در دکمه برای ارسال نهایی
    document.getElementById('btn-request').setAttribute('data-price', data.price);
}

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
        alert("✅ درخواست ارسال شد! منتظر راننده باشید...");
        step = 3; // حالت انتظار
        document.getElementById('btn-request').innerText = "🔍 در حال جستجو...";
        document.getElementById('btn-request').disabled = true;
    }
}