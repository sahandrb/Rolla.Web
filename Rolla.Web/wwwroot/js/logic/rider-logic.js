// wwwroot/js/logic/rider-logic.js

// ==========================================
// 1. متغیرهای سراسری و تنظیمات اولیه
// ==========================================

let originMarker = null;
let destMarker = null;
let step = 1; // 1: انتخاب مبدا، 2: انتخاب مقصد
let driverMarker = null;
let activeTripId = null;

// ==========================================
// 2. راه‌اندازی نقشه و رویدادهای کلیک
// ==========================================

document.addEventListener("DOMContentLoaded", function () {
    // اطمینان از لود شدن نقشه از map-base.js
    if (typeof initMap === "function") {
        initMap();
    } else {
        console.error("تابع initMap یافت نشد! فایل map-base.js لود نشده است.");
        if (typeof notify === "function") notify("خطا در بارگذاری نقشه", "error");
        return;
    }

    // تعریف رویداد کلیک روی نقشه
    map.on('click', function (e) {
        if (step === 1) {
            // مرحله ۱: انتخاب مبدا
            if (originMarker) map.removeLayer(originMarker);
            originMarker = addMarker(e.latlng.lat, e.latlng.lng, "📍 مبدا شما");
            step = 2;

            // UX: راهنمایی کاربر
            const btn = document.getElementById('btn-request');
            btn.innerText = "📍 مقصد را انتخاب کنید";

            // اگر از قبل مقصدی بود (مثلا کاربر مبدا را اصلاح کرده)، مقصد قبلی را پاک کن
            if (destMarker) {
                map.removeLayer(destMarker);
                destMarker = null;
                document.getElementById('price-display').style.display = 'none';
            }

        } else if (step === 2) {
            // مرحله ۲: انتخاب مقصد
            if (destMarker) map.removeLayer(destMarker);
            destMarker = addMarker(e.latlng.lat, e.latlng.lng, "🏁 مقصد شما");

            // محاسبه قیمت بلافاصله بعد از انتخاب مقصد
            calculatePrice();
        }
    });
});

// ==========================================
// 3. اتصال به SignalR
// ==========================================

const connection = new signalR.HubConnectionBuilder()
    .withUrl("/rideHub")
    .withAutomaticReconnect()
    .build();

connection.start().then(() => {
    console.log("Rider Connected to SignalR ✅");
}).catch(err => {
    console.error("SignalR Error:", err);
    if (typeof notify === "function") notify("خطا در اتصال به سرور", "error");
});

// ==========================================
// 4. انیمیشن حرکت راننده (Interpolation)
// ==========================================

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

    // محاسبه زمان سپری شده برای نرم کردن حرکت
    let duration = now - lastUpdateTimestamp;
    if (duration < 1000) duration = 1000; // حداقل ۱ ثانیه
    lastUpdateTimestamp = now;

    // شروع انیمیشن نرم به سمت نقطه جدید
    animateMarker(targetLat, targetLng, duration);
});

function animateMarker(targetLat, targetLng, duration) {
    const startLatLng = driverMarker.getLatLng();
    const startTime = performance.now();

    if (animationFrameId) cancelAnimationFrame(animationFrameId);

    function step(currentTime) {
        const elapsed = currentTime - startTime;
        const progress = Math.min(elapsed / duration, 1);

        const currentLat = startLatLng.lat + (targetLat - startLatLng.lat) * progress;
        const currentLng = startLatLng.lng + (targetLng - startLatLng.lng) * progress;

        driverMarker.setLatLng([currentLat, currentLng]);

        if (progress < 1) {
            animationFrameId = requestAnimationFrame(step);
        } else {
            animationFrameId = null;
        }
    }

    requestAnimationFrame(step);
}

// ==========================================
// 5. توابع مالی و درخواست سفر
// ==========================================

// محاسبه قیمت
async function calculatePrice() {
    document.getElementById('price-display').innerText = "در حال محاسبه...";
    document.getElementById('btn-request').disabled = true;

    const o = originMarker.getLatLng();
    const d = destMarker.getLatLng();

    try {
        const url = `/api/TripApi/calculate?oLat=${o.lat}&oLng=${o.lng}&dLat=${d.lat}&dLng=${d.lng}`;
        const res = await fetch(url);

        if (!res.ok) throw new Error("خطا در پاسخ سرور");

        const data = await res.json();
        console.log("قیمت محاسبه شد:", data.price);

        // نمایش قیمت
        document.getElementById('price-display').innerText = data.price.toLocaleString() + " تومان";

        // ذخیره قیمت در دکمه
        const btn = document.getElementById('btn-request');
        btn.setAttribute('data-price', data.price);
        btn.innerText = "درخواست رولا";
        btn.disabled = false;

    } catch (err) {
        console.error("Error calculating price:", err);
        document.getElementById('price-display').innerText = "خطا در محاسبه";
        if (typeof notify === "function") notify("خطا در محاسبه قیمت", "error");
    }
}

// ثبت درخواست سفر
async function submitRequest() {
    const btn = document.getElementById('btn-request');
    btn.disabled = true;
    btn.innerText = "⏳ در حال ارسال...";

    const o = originMarker.getLatLng();
    const d = destMarker.getLatLng();
    const price = btn.getAttribute('data-price');

    if (!price || price === "0") {
        if (typeof notify === "function") notify("قیمت نامعتبر است. لطفا دوباره مقصد را انتخاب کنید", "warning");
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

            // ✅ نوتیفیکیشن موفقیت
            if (typeof notify === "function") notify("درخواست شما ثبت شد. منتظر راننده باشید...", "success");

            // عضویت در گروه برای ردیابی
            await connection.invoke("JoinTripGroup", result.tripId);

            step = 3; // وضعیت انتظار
            btn.innerText = "🔍 در حال جستجوی راننده...";

        } else {
            const errorText = await res.text();
            console.error("Backend Error:", errorText);
            if (typeof notify === "function") notify("خطا در ثبت سفر: " + errorText, "error");

            btn.disabled = false;
            btn.innerText = "تلاش مجدد";
        }
    } catch (err) {
        console.error("Network Error:", err);
        if (typeof notify === "function") notify("خطا در اتصال به شبکه!", "error");

        btn.disabled = false;
        btn.innerText = "تلاش مجدد";
    }
}

// ==========================================
// 6. مدیریت وضعیت‌های سفر (سیگنال‌آر)
// ==========================================

// الف) پذیرش سفر توسط راننده
connection.on("TripAccepted", function (data) {
    console.log("Driver Found!", data);

    activeTripId = data.tripId; // ذخیره شناسه سفر

    // تغییر دکمه‌ها و UI
    const btn = document.getElementById('btn-request');
    btn.className = "btn btn-success w-100 btn-lg";
    btn.innerText = `🚗 راننده پیدا شد!`;

    // ✅ نوتیفیکیشن پذیرش
    if (typeof notify === "function") notify(data.message, "success");

    // نمایش دکمه چت
    const chatBtn = document.getElementById('btn-open-chat');
    if (chatBtn) chatBtn.style.display = 'block';

    // عضویت در گروه برای دیدن لوکیشن زنده
    connection.invoke("JoinTripGroup", data.tripId);
});

// ب) آپدیت وضعیت‌های سفر (رسیدن، شروع، پایان، لغو)
connection.on("ReceiveStatusUpdate", function (message) {
    console.log("Status Update:", message);

    const btn = document.getElementById('btn-request');

    if (message === "Arrived") {
        btn.className = "btn btn-warning w-100 btn-lg";
        btn.innerText = "🚖 راننده رسید! سوار شوید.";
        if (typeof notify === "function") notify("راننده به مبدا رسید", "info");
    }
    else if (message === "Started") {
        btn.className = "btn btn-info w-100 btn-lg";
        btn.innerText = "🚀 در حال سفر...";
        if (typeof notify === "function") notify("سفر شما شروع شد", "info");
    }
    else if (message === "Finished") {
        btn.className = "btn btn-success w-100 btn-lg";
        btn.innerText = "✅ سفر تمام شد.";
        if (typeof notify === "function") notify("سفر با موفقیت پایان یافت. هزینه پرداخت شد", "success");

        // مخفی کردن چت
        document.getElementById('chatBox').style.display = 'none';
        document.getElementById('btn-open-chat').style.display = 'none';

        setTimeout(() => { location.reload(); }, 3000);
    }
    else if (message === "Canceled") {
        if (typeof notify === "function") notify("⛔ سفر لغو شد", "error");

        // مخفی کردن چت
        document.getElementById('chatBox').style.display = 'none';
        document.getElementById('btn-open-chat').style.display = 'none';

        setTimeout(() => { location.reload(); }, 2000);
    }
});

// ==========================================
// 7. سیستم چت
// ==========================================

function toggleChat() {
    const box = document.getElementById('chatBox');
    const btn = document.getElementById('btn-open-chat');

    if (box.style.display === 'none') {
        // باز کردن چت
        box.style.display = 'block';
        // برگرداندن رنگ دکمه به حالت عادی
        if (btn) btn.className = "btn btn-info rounded-circle shadow d-flex align-items-center justify-content-center";
    } else {
        // بستن چت
        box.style.display = 'none';
    }
}

async function sendChatMessage() {
    const input = document.getElementById('chatInput');
    const message = input.value.trim();
    if (!message || !activeTripId) return;

    // فراخوانی متد سرور
    await connection.invoke("SendChatMessage", activeTripId, message);
    input.value = "";
}

// دریافت پیام چت
connection.on("ReceiveChatMessage", function (senderId, message) {
    const chatMessages = document.getElementById('chatMessages');

    const isMe = (typeof currentUserId !== 'undefined') && (currentUserId === senderId);

    const msgDiv = document.createElement('div');
    // استایل‌دهی: آبی برای من، طوسی برای راننده
    msgDiv.className = `mb-2 p-2 rounded ${isMe ? 'bg-primary text-white text-start' : 'bg-light text-dark text-end'}`;

    const senderName = isMe ? "شما" : "راننده";

    msgDiv.innerHTML = `<small class="fw-bold d-block">${senderName}:</small> <span>${message}</span>`;

    chatMessages.appendChild(msgDiv);
    chatMessages.scrollTop = chatMessages.scrollHeight;

    // اگر پنجره چت بسته است، نوتیفیکیشن بده
    const chatBox = document.getElementById('chatBox');
    if (chatBox.style.display === 'none') {
        const chatBtn = document.getElementById('btn-open-chat');
        if (chatBtn) {
            // قرمز کردن دکمه چت
            chatBtn.className = "btn btn-danger rounded-circle shadow-lg d-flex align-items-center justify-content-center";
        }
        if (typeof notify === "function") notify("پیام جدید از راننده", "info");
    }
});