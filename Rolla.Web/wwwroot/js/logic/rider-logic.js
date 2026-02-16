// wwwroot/js/logic/rider-logic.js

// متغیرهای سراسری
let originMarker = null;
let destMarker = null;
let step = 1; // 1: انتخاب مبدا، 2: انتخاب مقصد
let driverMarker = null;

// ۱. راه‌اندازی نقشه و رویداد کلیک
document.addEventListener("DOMContentLoaded", function () {
    // اطمینان از لود شدن نقشه از map-base.js
    if (typeof initMap === "function") {
        initMap();
    } else {
        console.error("تابع initMap یافت نشد! فایل map-base.js لود نشده است.");
        return;
    }

    // تعریف رویداد کلیک روی نقشه
    map.on('click', function (e) {
        if (step === 1) {
            // انتخاب مبدا
            if (originMarker) map.removeLayer(originMarker);
            // استفاده از تابع addMarker که در map-base.js است
            originMarker = addMarker(e.latlng.lat, e.latlng.lng, "📍 مبدا شما");
            step = 2;

            // UX: راهنمایی کاربر
            const btn = document.getElementById('btn-request');
            btn.innerText = "📍 مقصد را انتخاب کنید";

        } else if (step === 2) {
            // انتخاب مقصد
            if (destMarker) map.removeLayer(destMarker);
            destMarker = addMarker(e.latlng.lat, e.latlng.lng, "🏁 مقصد شما");

            // محاسبه قیمت بلافاصله بعد از انتخاب مقصد
            calculatePrice();
        }
    });
});

// ۲. اتصال به SignalR (برای دیدن راننده)
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/rideHub")
    .withAutomaticReconnect()
    .build();

connection.start().then(() => {
    console.log("Rider Connected to SignalR ✅");
}).catch(err => console.error("SignalR Error:", err));

// دریافت لوکیشن راننده
connection.on("ReceiveDriverLocation", function (lat, lng) {
    if (driverMarker) {
        driverMarker.setLatLng([lat, lng]);
    } else {
        // اگر آیکون ماشین نداری، فعلا مارکر معمولی می‌گذاریم
        driverMarker = L.marker([lat, lng]).addTo(map).bindPopup("🚖 راننده").openPopup();
    }
});

// ۳. تابع محاسبه قیمت (در دسترس سراسری)
async function calculatePrice() {
    // تغییر متن برای اینکه کاربر بفهمد سیستم در حال کار است
    document.getElementById('price-display').innerText = "در حال محاسبه...";
    document.getElementById('btn-request').disabled = true;

    const o = originMarker.getLatLng();
    const d = destMarker.getLatLng();

    try {
        // فراخوانی API
        const url = `/api/TripApi/calculate?oLat=${o.lat}&oLng=${o.lng}&dLat=${d.lat}&dLng=${d.lng}`;
        const res = await fetch(url);

        if (!res.ok) throw new Error("خطا در پاسخ سرور");

        const data = await res.json();
        console.log("قیمت محاسبه شد:", data.price);

        // نمایش قیمت
        document.getElementById('price-display').innerText = data.price.toLocaleString() + " تومان";

        // ذخیره قیمت در دکمه برای ارسال نهایی
        const btn = document.getElementById('btn-request');
        btn.setAttribute('data-price', data.price);
        btn.innerText = "درخواست اسنپ";
        btn.disabled = false; // حالا دکمه فعال شود

    } catch (err) {
        console.error("Error calculating price:", err);
        document.getElementById('price-display').innerText = "خطا در محاسبه";
    }
}

// ۴. تابع ثبت درخواست (در دسترس سراسری برای دکمه HTML)
async function submitRequest() {
    const btn = document.getElementById('btn-request');
    btn.disabled = true;
    btn.innerText = "⏳ در حال ارسال...";

    const o = originMarker.getLatLng();
    const d = destMarker.getLatLng();
    const price = btn.getAttribute('data-price');

    if (!price || price === "0") {
        alert("قیمت نامعتبر است. لطفا دوباره مقصد را انتخاب کنید.");
        return;
    }

    const dto = {
        originLat: o.lat,
        originLng: o.lng,
        destinationLat: d.lat,
        destinationLng: d.lng,
        estimatedPrice: parseFloat(price)
    };

    try {
        const res = await fetch('/api/TripApi/request', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(dto)
        });

        if (res.ok) {
            const result = await res.json();
            alert("✅ درخواست با موفقیت ثبت شد! منتظر پذیرش راننده باشید.");

            // عضویت در گروه برای ردیابی راننده
            await connection.invoke("JoinTripGroup", result.tripId);

            step = 3; // وضعیت انتظار
            btn.innerText = "🔍 در حال جستجوی راننده...";
        } else {
            const errorText = await res.text();
            console.error("Backend Error:", errorText);
            alert("خطا در ثبت سفر: " + errorText);
            btn.disabled = false;
            btn.innerText = "تلاش مجدد";
        }
    } catch (err) {
        console.error("Network Error:", err);
        alert("خطای شبکه!");
        btn.disabled = false;
        btn.innerText = "تلاش مجدد";
    }
}
// دریافت پیام قبول شدن سفر
connection.on("TripAccepted", function (data) {
    console.log("Driver Found!", data);

    // ۱. تغییر متن دکمه و غیرفعال کردن
    const btn = document.getElementById('btn-request');
    btn.className = "btn btn-success w-100 btn-lg";
    btn.innerText = `🚗 راننده پیدا شد! (${data.driverId})`;

    // ۲. نمایش نوتیفیکیشن
    alert(data.message);

    // ۳. عضویت در گروه سفر برای دیدن حرکت زنده راننده
    connection.invoke("JoinTripGroup", data.tripId);
});