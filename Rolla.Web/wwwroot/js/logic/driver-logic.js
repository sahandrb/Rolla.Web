// wwwroot/js/logic/driver-logic.js

const connection = new signalR.HubConnectionBuilder()
    .withUrl("/rideHub")
    .withAutomaticReconnect([0, 2000, 10000, 30000])
    .build();

let isOnline = false;
let locationInterval;
let currentOfferId = null;
let isWorkingOnTrip = false; // آیا در حال انجام سفر هستیم؟
let activeTripId = null;    // آیدی سفری که قبول کردیم

async function startSignalR() {
    try {
        await connection.start();
        console.log("SignalR Connected ✅");
        document.getElementById('status-indicator').innerText = "آنلاین";
        document.getElementById('status-indicator').className = "text-success fw-bold";
    } catch (err) {
        console.error("SignalR Error:", err);
        setTimeout(startSignalR, 5000);
    }
}

function toggleWork() {
    isOnline = !isOnline;
    const btn = document.getElementById('btn-toggle');

    if (isOnline) {
        btn.innerText = "🔴 پایان کار";
        btn.className = "btn btn-danger w-100";
        startSendingLocation();
    } else {
        btn.innerText = "🟢 شروع کار";
        btn.className = "btn btn-success w-100";
        stopSendingLocation();
    }
}

function startSendingLocation() {
    if (!navigator.geolocation) return;

    locationInterval = setInterval(() => {
        navigator.geolocation.getCurrentPosition(pos => {
            const { latitude, longitude } = pos.coords;

            // ۱. بروزرسانی مارکر راننده روی نقشه خودش
            if (userMarker) {
                userMarker.setLatLng([latitude, longitude]);
            } else {
                userMarker = L.marker([latitude, longitude]).addTo(map);
            }
            map.setView([latitude, longitude]);

            // ۲. ارسال مختصات به سرور
            // اگر در حال سفر هستیم، آیدی سفر را هم می‌فرستیم تا مسافر ببیند
            const tripIdToSend = isWorkingOnTrip ? activeTripId : null;

            connection.invoke("UpdateDriverLocation", latitude, longitude, tripIdToSend)
                .catch(err => console.error(err));

        }, err => console.error(err), { enableHighAccuracy: true });
    }, 3000);
}

function stopSendingLocation() {
    clearInterval(locationInterval);
    isWorkingOnTrip = false;
    activeTripId = null;
}

// ۳. دریافت پیشنهاد سفر
connection.on("ReceiveTripOffer", function (trip) {
    currentOfferId = trip.tripId;
    document.getElementById('modal-price').innerText = trip.price.toLocaleString() + " تومان";

    var myModal = new bootstrap.Modal(document.getElementById('tripModal'));
    myModal.show();
});

async function acceptTrip() {
    try {
        const res = await fetch(`/api/TripApi/accept/${currentOfferId}`, {
            method: 'POST'
        });

        if (res.ok) {
            // ۱. بستن مودال
            const modalElement = document.getElementById('tripModal');
            const modalInstance = bootstrap.Modal.getInstance(modalElement);
            if (modalInstance) modalInstance.hide();

            // ۲. تغییر متغیرهای وضعیت
            isWorkingOnTrip = true;
            activeTripId = currentOfferId;

            // ۳. تغییر UI (نمایش پنل سفر)
            showTripInfoPanel();

            // ۴. عضویت در گروه سفر برای ارسال لوکیشن دقیق
            await connection.invoke("JoinTripGroup", activeTripId);

        } else {
            alert("❌ متاسفانه سفر توسط راننده دیگری رزرو شد.");
        }
    } catch (err) {
        console.error("Error accepting trip:", err);
    }
}
// این تابع UI پنل راننده را بر اساس وضعیت سفر تغییر می‌دهد
function showTripInfoPanel(status = 'Accepted') {
    const statusDiv = document.querySelector('.card-body');

    let actionButtons = '';

    if (status === 'Accepted') {
        actionButtons = `<button class="btn btn-warning w-100 mb-2" onclick="sendArrived()">📍 رسیدم به مبدا</button>`;
    } else if (status === 'Arrived') {
        actionButtons = `<button class="btn btn-primary w-100 mb-2" onclick="sendStart()">🚀 شروع سفر</button>`;
    } else if (status === 'Started') {
        actionButtons = `<button class="btn btn-danger w-100 mb-2" onclick="sendFinish()">🏁 پایان سفر و دریافت پول</button>`;
    }

    statusDiv.innerHTML = `
        <h4 class="text-success">وضعیت: ${getStatusText(status)}</h4>
        <div id="trip-actions" class="mt-3">
            ${actionButtons}
        </div>
        <hr/>
        <button class="btn btn-dark w-100 mb-2" onclick="startSimulation()">🎮 شبیه‌سازی حرکت</button>
        <button class="btn btn-outline-secondary w-100" onclick="openWaze()">مسیریابی</button>
    `;
}

function getStatusText(status) {
    switch (status) {
        case 'Accepted': return 'در مسیر مبدا';
        case 'Arrived': return 'منتظر مسافر';
        case 'Started': return 'در حال سفر به مقصد';
        default: return status;
    }
}

// 1. تابع رسیدم به مبدا
async function sendArrived() {
    try {
        const res = await fetch(`/api/TripApi/arrive/${activeTripId}`, { method: 'POST' });
        if (res.ok) {
            showTripInfoPanel('Arrived');
            alert("به مسافر اطلاع داده شد که رسیدید.");
        }
    } catch (err) { console.error(err); }
}

// 2. تابع شروع سفر
async function sendStart() {
    try {
        const res = await fetch(`/api/TripApi/start/${activeTripId}`, { method: 'POST' });
        if (res.ok) {
            showTripInfoPanel('Started');
            alert("سفر شروع شد! به سمت مقصد برانید.");
        }
    } catch (err) { console.error(err); }
}

// 3. تابع پایان سفر
async function sendFinish() {
    if (!confirm("آیا مطمئن هستید سفر تمام شده؟ هزینه از کیف پول مسافر کسر می‌شود.")) return;

    try {
        const res = await fetch(`/api/TripApi/finish/${activeTripId}`, { method: 'POST' });
        if (res.ok) {
            alert("✅ سفر با موفقیت تمام شد و هزینه دریافت شد.");
            // بازنشانی صفحه برای سفر بعدی
            location.reload();
        } else {
            const err = await res.json();
            alert("❌ خطا: " + err.message);
        }
    } catch (err) { console.error(err); }
}


// === شبیه‌ساز حرکت (فقط برای تست) ===
let simulationInterval;

function startSimulation() {
    // نقطه شروع (مثلاً میدان آزادی)
    let lat = 35.71;
    let lng = 51.41;

    // جهت حرکت (کمی کج حرکت کند تا طبیعی‌تر باشد)
    const stepLat = 0.00015;
    const stepLng = 0.00015;

    alert("🎮 شبیه‌سازی حرکت شروع شد! به پنل مسافر بروید.");

    // جلوگیری از اجرای همزمان چند شبیه‌ساز
    if (simulationInterval) clearInterval(simulationInterval);

    simulationInterval = setInterval(() => {
        lat += stepLat;
        lng += stepLng;

        // ۱. آپدیت آنی نقشه خود راننده (راننده نیاز به انیمیشن ندارد، GPS خودش است)
        if (userMarker) {
            userMarker.setLatLng([lat, lng]);
        } else {
            userMarker = L.marker([lat, lng]).addTo(map);
        }
        map.panTo([lat, lng]); // دوربین دنبال ماشین برود

        // ۲. ارسال به سرور
        if (isWorkingOnTrip && activeTripId) {
            connection.invoke("UpdateDriverLocation", lat, lng, activeTripId)
                .catch(err => console.error(err));
        }
    }, 1000); // ارسال هر ۱۰۰۰ میلی‌ثانیه (۱ ثانیه)
}


function openWaze() {
    // اینجا باید مختصات مسافر رو داشته باشیم (فعلا هاردکد شده)
    window.open("https://waze.com/ul?ll=35.71,51.41&navigate=yes");
}

// شروع اولیه
initMap();
startSignalR();