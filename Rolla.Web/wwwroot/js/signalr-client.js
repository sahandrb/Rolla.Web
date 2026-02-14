// ==========================================
//   Rolla Real-time Client (SignalR)
// ==========================================

"use strict";

// 1. تنظیم اتصال به هاب (Hub)
// آدرس "/rideHub" همون چیزیه که تو Program.cs تعریف کردیم
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/rideHub") // آدرس سرور مخابرات
    .withAutomaticReconnect() // اگه نت قطع شد، خودش تلاش کنه وصل شه
    .build();

// 2. تعریف تابع استارت (شروع اتصال)
async function startConnection() {
    try {
        await connection.start();
        console.log("✅ اتصال به شبکه رولا برقرار شد! (SignalR Connected)");
    } catch (err) {
        console.error("❌ خطا در اتصال به سرور:", err);
        // اگه نشد، 5 ثانیه صبر کن دوباره تلاش کن
        setTimeout(startConnection, 5000);
    }
}

// 3. گوش دادن به پیام‌های سرور (Receivers)

// الف) دریافت پیام برای راننده: "درخواست سفر جدید آمد"
connection.on("ReceiveNewTripRequest", function (trip) {
    console.log("🔔 درخواست سفر جدید دریافت شد:", trip);

    // فعلاً یک آلرت ساده میدیم (بعداً مودال خوشگل میسازیم)
    alert(`مسافر جدید پیدا شد!\nقیمت: ${trip.price} تومان\nکد سفر: ${trip.tripId}`);
});

// ب) دریافت پیام برای مسافر: "راننده حرکت کرد"
connection.on("ReceiveLocationUpdate", function (lat, lng) {
    console.log(`📍 راننده در مختصات (${lat}, ${lng}) است.`);

    // TODO: اینجا بعداً تابع حرکت مارکر روی نقشه رو صدا می‌زنیم
    // moveDriverMarker(lat, lng);
});

// 4. استارت نهایی
startConnection();