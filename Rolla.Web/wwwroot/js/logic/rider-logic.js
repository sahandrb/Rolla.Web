// wwwroot/js/logic/rider-logic.js

// متغیرهای سراسری
let originMarker = null;
let destMarker = null;
let step = 1; // 1: انتخاب مبدا، 2: انتخاب مقصد
let driverMarker = null;
// در بالای هر دو فایل js:
let activeTripId = null;
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





// تعریف آیکون ماشین
// متغیرهای وضعیت برای انیمیشن
let lastUpdateTimestamp = 0;
let animationFrameId = null;

// آیکون ماشین
const carIcon = L.icon({
    iconUrl: 'https://cdn-icons-png.flaticon.com/512/744/744465.png',
    iconSize: [40, 40],
    iconAnchor: [20, 20],
    popupAnchor: [0, -20]
});

connection.on("ReceiveDriverLocation", function (targetLat, targetLng) {
    const now = Date.now();

    // اگر اولین بار است که لوکیشن می‌گیریم
    if (!driverMarker) {
        driverMarker = L.marker([targetLat, targetLng], { icon: carIcon }).addTo(map)
            .bindPopup("🚖 راننده").openPopup();
        lastUpdateTimestamp = now;
        return;
    }

    // محاسبه زمان سپری شده از آخرین آپدیت (برای پیش‌بینی سرعت)
    // اگر تاخیر شبکه داشتیم، حداقل ۱ ثانیه را در نظر می‌گیریم تا حرکت خیلی سریع نشود
    let duration = now - lastUpdateTimestamp;
    if (duration < 1000) duration = 1000; // Minimum 1 second smoothing
    lastUpdateTimestamp = now;

    // شروع انیمیشن نرم به سمت نقطه جدید
    animateMarker(targetLat, targetLng, duration);
});

// === تابع ریاضی برای حرکت نرم (Interpolation) ===
function animateMarker(targetLat, targetLng, duration) {
    const startLatLng = driverMarker.getLatLng();
    const startTime = performance.now();

    // اگر انیمیشن قبلی هنوز تمام نشده، کنسلش کن تا تداخل پیش نیاید
    if (animationFrameId) cancelAnimationFrame(animationFrameId);

    function step(currentTime) {
        const elapsed = currentTime - startTime;
        const progress = Math.min(elapsed / duration, 1); // عدد بین 0 تا 1

        // فرمول ریاضی: نقطه فعلی + (اختلاف * درصد پیشرفت)
        const currentLat = startLatLng.lat + (targetLat - startLatLng.lat) * progress;
        const currentLng = startLatLng.lng + (targetLng - startLatLng.lng) * progress;

        driverMarker.setLatLng([currentLat, currentLng]);

        // اگر هنوز به مقصد نرسیده، فریم بعدی را درخواست کن
        if (progress < 1) {
            animationFrameId = requestAnimationFrame(step);
        } else {
            animationFrameId = null;
        }
    }

    requestAnimationFrame(step);
}






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

    activeTripId = data.tripId;

    // ۱. تغییر متن دکمه و غیرفعال کردن
    const btn = document.getElementById('btn-request');
    btn.className = "btn btn-success w-100 btn-lg";
    btn.innerText = `🚗 راننده پیدا شد! (${data.driverId})`;

    // ۲. نمایش نوتیفیکیشن
    alert(data.message);

    // ۳. عضویت در گروه سفر برای دیدن حرکت زنده راننده
    connection.invoke("JoinTripGroup", data.tripId);
});



// ... (کدهای قبلی سر جایشان باشند)

// دریافت وضعیت سفر
connection.on("ReceiveStatusUpdate", function (message) {
    console.log("Status Update:", message);

    const btn = document.getElementById('btn-request');

    if (message === "Arrived") {
        btn.className = "btn btn-warning w-100 btn-lg";
        btn.innerText = "🚖 راننده رسید! سوار شوید.";
        alert("راننده به مبدا رسید.");
    }
    else if (message === "Started") {
        btn.className = "btn btn-info w-100 btn-lg";
        btn.innerText = "🚀 در حال سفر...";
        alert("سفر شما شروع شد.");
    }
    else if (message === "Finished") {
        btn.className = "btn btn-success w-100 btn-lg";
        btn.innerText = "✅ سفر تمام شد. پرداخت انجام شد.";
        alert("سفر به پایان رسید.");
        setTimeout(() => { location.reload(); }, 3000);
    }
    else if (message === "Canceled") {
        alert("⛔ سفر لغو شد.");
        location.reload();
    }

    // ✅ این قسمت باید همین‌جا (داخل تابع) باشد:
    if (message === "Finished" || message === "Canceled") {
        document.getElementById('chatBox').style.display = 'none';
        document.getElementById('btn-open-chat').style.display = 'none';
    }
});

// ... (توابع toggleChat و sendChatMessage و ... سر جایشان باشند)

// مدیریت چت
function toggleChat() {
    const box = document.getElementById('chatBox');
    box.style.display = box.style.display === 'none' ? 'block' : 'none';
}

async function sendChatMessage() {
    const input = document.getElementById('chatInput');
    const message = input.value.trim();
    if (!message || !activeTripId) return;

    // فراخوانی متد هاب که در RideHub ساختیم
    await connection.invoke("SendChatMessage", activeTripId, message);
    input.value = "";
}

// دریافت پیام از سیگنال‌آر
connection.on("ReceiveChatMessage", function (senderId, message) {
    const chatMessages = document.getElementById('chatMessages');

    const isMe = (typeof currentUserId !== 'undefined') && (currentUserId === senderId);

    const msgDiv = document.createElement('div');
    msgDiv.className = `mb-2 p-2 rounded ${isMe ? 'bg-primary text-white text-start' : 'bg-light text-dark text-end'}`; // (رنگ‌ها را برعکس کردم تا با راننده هماهنگ باشد، یا هرطور سلیقه شماست)

    // ✅ اصلاح نام‌گذاری برای مسافر
    const senderName = isMe ? "شما" : "راننده";

    msgDiv.innerHTML = `<small class="fw-bold d-block">${senderName}:</small> <span>${message}</span>`;

    chatMessages.appendChild(msgDiv);
    chatMessages.scrollTop = chatMessages.scrollHeight;

    // ... بقیه کدها ...
});

// در فایل rider-logic.js بخش connection.on("TripAccepted", ...) را پیدا کنید:

connection.on("TripAccepted", function (data) {
    console.log("Driver Found!", data);

    // ✨ فیکس: ذخیره آیدی سفر برای ارسال پیام ✨
    activeTripId = data.tripId; // <--- این خط بسیار مهم است

    // تغییر دکمه‌ها و UI
    const btn = document.getElementById('btn-request');
    btn.className = "btn btn-success w-100 btn-lg";
    btn.innerText = `🚗 راننده پیدا شد!`;

    alert(data.message);

    // نمایش دکمه چت برای مسافر
    document.getElementById('btn-open-chat').style.display = 'block';

    // عضویت در گروه
    connection.invoke("JoinTripGroup", data.tripId);
});

