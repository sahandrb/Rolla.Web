// wwwroot/js/logic/driver-logic.js

// ==========================================
// 1. تنظیمات اولیه و متغیرها
// ==========================================

const connection = new signalR.HubConnectionBuilder()
    .withUrl("/rideHub")
    .withAutomaticReconnect([0, 2000, 10000, 30000])
    .build();

let isOnline = false;
let locationInterval;
let currentOfferId = null;
let isWorkingOnTrip = false; // آیا در حال انجام سفر هستیم؟
let activeTripId = null;    // آیدی سفری که قبول کردیم
let navigationRouteLayer = null; // نگهدارنده خط قرمز/آبی روی نقشه
let userMarker = null; // مارکر خود راننده روی نقشه

// ==========================================
// 2. اتصال به سرور (SignalR)
// ==========================================

async function startSignalR() {
    try {
        await connection.start();
        console.log("SignalR Connected ✅");

        // تغییر وضعیت در UI
        document.getElementById('status-indicator').innerText = "آنلاین";
        document.getElementById('status-indicator').className = "text-success fw-bold";

        // اگر تابع notify لود شده باشد (که در Layout هست)
        if (typeof notify === "function") {
            // notify("اتصال به مرکز برقرار شد", "success");
        }
    } catch (err) {
        console.error("SignalR Error:", err);
        setTimeout(startSignalR, 5000);
    }
}

// ==========================================
// 3. مدیریت وضعیت کاری (آنلاین/آفلاین)
// ==========================================

function toggleWork() {
    isOnline = !isOnline;
    const btn = document.getElementById('btn-toggle');

    if (isOnline) {
        btn.innerText = "🔴 پایان کار";
        btn.className = "btn btn-danger w-100";
        btn.style.boxShadow = "0 8px 25px rgba(239, 68, 68, 0.3)";
        startSendingLocation();
        if (typeof notify === "function") notify("شما آنلاین شدید و قابل رویت هستید", "success");
    } else {
        btn.innerText = "🟢 شروع کار";
        btn.className = "btn btn-success w-100";
        btn.style.boxShadow = "0 8px 25px rgba(34, 197, 94, 0.3)";
        stopSendingLocation();
        if (typeof notify === "function") notify("شما آفلاین شدید", "warning");
    }
}

function startSendingLocation() {
    if (!navigator.geolocation) {
        if (typeof notify === "function") notify("مرورگر شما از موقعیت مکانی پشتیبانی نمی‌کند", "error");
        return;
    }

    locationInterval = setInterval(() => {
        navigator.geolocation.getCurrentPosition(pos => {
            const { latitude, longitude } = pos.coords;

            // الف) بروزرسانی مارکر راننده روی نقشه خودش
            if (userMarker) {
                userMarker.setLatLng([latitude, longitude]);
            } else {
                // آیکون پیش‌فرض یا آیکون ماشین
                userMarker = L.marker([latitude, longitude]).addTo(map);
            }
            map.setView([latitude, longitude]);

            // ب) ارسال مختصات به سرور
            // اگر در حال سفر هستیم، آیدی سفر را هم می‌فرستیم تا مسافر ببیند
            const tripIdToSend = isWorkingOnTrip ? activeTripId : null;

            connection.invoke("UpdateDriverLocation", latitude, longitude, tripIdToSend)
                .catch(err => console.error(err));

        }, err => console.error(err), { enableHighAccuracy: true });
    }, 3000); // هر ۳ ثانیه
}

function stopSendingLocation() {
    clearInterval(locationInterval);
    // ریست کردن وضعیت سفر در صورت آفلاین شدن دستی
    // isWorkingOnTrip = false; 
    // activeTripId = null;
}

// ==========================================
// 4. دریافت و مدیریت درخواست‌های سفر
// ==========================================

// دریافت پیشنهاد سفر از سرور
connection.on("ReceiveTripOffer", function (trip) {
    currentOfferId = trip.tripId;
    document.getElementById('modal-price').innerText = trip.price.toLocaleString() + " تومان";
    // فاصله فرضی (بعداً می‌توان دقیق‌تر کرد)
    document.getElementById('modal-dist').innerText = "نزدیک شما";

    const modalEl = document.getElementById('tripModal');
    let myModal = bootstrap.Modal.getInstance(modalEl);
    if (!myModal) {
        myModal = new bootstrap.Modal(modalEl);
    }
    myModal.show();

    // پخش صدای نوتیفیکیشن یا ویبره (اختیاری)
    if (typeof notify === "function") notify("🔔 درخواست سفر جدید!", "info");
});

// رد کردن سفر
function rejectTrip() {
    if (!currentOfferId) return;

    fetch(`/api/TripApi/reject/${currentOfferId}`, {
        method: 'POST'
    })
        .then(res => {
            if (res.ok) {
                const modalElement = document.getElementById('tripModal');
                const modalInstance = bootstrap.Modal.getInstance(modalElement);
                if (modalInstance) modalInstance.hide();
                currentOfferId = null;
            }
        })
        .catch(err => console.error(err));
}

// قبول کردن سفر
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

            // ۲. تنظیم متغیرها
            isWorkingOnTrip = true;
            activeTripId = currentOfferId;

            // ۳. تغییر UI
            showTripInfoPanel();

            // ۴. نمایش دکمه چت
            const chatBtn = document.getElementById('btn-open-chat');
            if (chatBtn) chatBtn.style.display = 'block';

            // ۵. عضویت در گروه سفر و مسیریابی
            await connection.invoke("JoinTripGroup", activeTripId);
            updateNavigationRoute(activeTripId);

            if (typeof notify === "function") notify("✅ سفر با موفقیت رزرو شد", "success");

        } else {
            // خطای رزرو
            if (typeof notify === "function") notify("❌ متاسفانه سفر توسط راننده دیگری رزرو شد", "error");

            const modalElement = document.getElementById('tripModal');
            const modalInstance = bootstrap.Modal.getInstance(modalElement);
            if (modalInstance) modalInstance.hide();
        }
    } catch (err) {
        console.error("Error accepting trip:", err);
        if (typeof notify === "function") notify("خطا در ارتباط با سرور", "error");
    }
}

// ==========================================
// 5. مدیریت پنل وضعیت سفر (UI Logic)
// ==========================================

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

    // ساختار HTML پنل پایین
    statusDiv.innerHTML = `
        <h4 class="text-success mb-3">وضعیت: ${getStatusText(status)}</h4>
        <div id="trip-actions" class="w-100">
            ${actionButtons}
        </div>
        <hr class="w-100 my-3"/>
        <div class="row w-100 g-2">
            <div class="col-6">
                 <button class="btn btn-dark w-100" style="font-size: 0.9rem;" onclick="startSimulation()">🎮 شبیه‌سازی</button>
            </div>
            <div class="col-6">
                 <button class="btn btn-outline-secondary w-100" style="font-size: 0.9rem;" onclick="openWaze()">🗺️ مسیریابی</button>
            </div>
        </div>
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

// ==========================================
// 6. اکشن‌های تغییر وضعیت سفر
// ==========================================

// الف) اعلام رسیدن به مبدا
async function sendArrived() {
    try {
        const res = await fetch(`/api/TripApi/arrive/${activeTripId}`, { method: 'POST' });
        if (res.ok) {
            showTripInfoPanel('Arrived');
            if (typeof notify === "function") notify("📍 وضعیت: رسیدن به مبدا ثبت شد", "success");
        }
    } catch (err) { console.error(err); }
}

// ب) شروع سفر
async function sendStart() {
    try {
        const res = await fetch(`/api/TripApi/start/${activeTripId}`, { method: 'POST' });
        if (res.ok) {
            showTripInfoPanel('Started');
            if (typeof notify === "function") notify("🚀 سفر شروع شد! مسیر تا مقصد روی نقشه است", "info");
            updateNavigationRoute(activeTripId); // رسم مسیر از مبدأ تا مقصد نهایی
        }
    } catch (err) { console.error(err); }
}

// ج) پایان سفر
async function sendFinish() {
    if (!confirm("آیا از پایان سفر و دریافت هزینه اطمینان دارید؟")) return;

    try {
        const res = await fetch(`/api/TripApi/finish/${activeTripId}`, { method: 'POST' });
        if (res.ok) {
            if (typeof notify === "function") notify("✅ سفر با موفقیت به پایان رسید", "success");

            // مخفی کردن چت
            const chatBox = document.getElementById('chatBox');
            const chatBtn = document.getElementById('btn-open-chat');
            if (chatBox) chatBox.style.display = 'none';
            if (chatBtn) chatBtn.style.display = 'none';

            // ریلود صفحه برای آماده شدن برای سفر بعدی
            setTimeout(() => { location.reload(); }, 2000);
        }
    } catch (err) { console.error(err); }
}

// ==========================================
// 7. ابزارها (شبیه‌ساز، ویز، مسیریابی)
// ==========================================

let simulationInterval;

function startSimulation() {
    // نقطه شروع (مثلاً میدان آزادی)
    let lat = 35.71;
    let lng = 51.41;

    // جهت حرکت
    const stepLat = 0.00015;
    const stepLng = 0.00015;

    if (typeof notify === "function") notify("🎮 شبیه‌سازی حرکت شروع شد! در پنل مسافر حرکت را ببینید.", "info");

    if (simulationInterval) clearInterval(simulationInterval);

    simulationInterval = setInterval(() => {
        lat += stepLat;
        lng += stepLng;

        // ۱. آپدیت آنی نقشه خود راننده
        if (userMarker) {
            userMarker.setLatLng([lat, lng]);
        } else {
            userMarker = L.marker([lat, lng]).addTo(map);
        }
        map.panTo([lat, lng]);

        // ۲. ارسال به سرور
        if (isWorkingOnTrip && activeTripId) {
            connection.invoke("UpdateDriverLocation", lat, lng, activeTripId)
                .catch(err => console.error(err));
        }
    }, 1000);
}

function openWaze() {
    window.open("https://waze.com/ul?ll=35.71,51.41&navigate=yes");
}

async function updateNavigationRoute(tripId) {
    try {
        const response = await fetch(`/api/TripApi/navigation/${tripId}`);
        if (!response.ok) return;

        const data = await response.json();

        if (data && data.encodedPolyline) {
            if (navigationRouteLayer) {
                map.removeLayer(navigationRouteLayer);
            }

            const coordinates = decodePolyline(data.encodedPolyline);

            navigationRouteLayer = L.polyline(coordinates, {
                color: '#2ecc71',
                weight: 6,
                opacity: 0.8,
                lineJoin: 'round'
            }).addTo(map);

            map.fitBounds(navigationRouteLayer.getBounds(), { padding: [50, 50] });
        }
    } catch (error) {
        console.error("خطا در دریافت مسیر:", error);
    }
}

function decodePolyline(str, precision) {
    var index = 0, lat = 0, lng = 0, coordinates = [], shift = 0, result = 0, byte = null, latitude_change, longitude_change, factor = Math.pow(10, precision || 5);
    while (index < str.length) {
        byte = null; shift = 0; result = 0;
        do { byte = str.charCodeAt(index++) - 63; result |= (byte & 0x1f) << shift; shift += 5; } while (byte >= 0x20);
        latitude_change = ((result & 1) ? ~(result >> 1) : (result >> 1)); lat += latitude_change;
        shift = 0; result = 0;
        do { byte = str.charCodeAt(index++) - 63; result |= (byte & 0x1f) << shift; shift += 5; } while (byte >= 0x20);
        longitude_change = ((result & 1) ? ~(result >> 1) : (result >> 1)); lng += longitude_change;
        coordinates.push([lat / factor, lng / factor]);
    }
    return coordinates;
};

// ==========================================
// 8. سیستم چت
// ==========================================

// دریافت پیام
connection.on("ReceiveChatMessage", function (senderId, message) {
    const chatMessages = document.getElementById('chatMessages');

    const isMe = (typeof currentUserId !== 'undefined') && (currentUserId === senderId);
    const msgDiv = document.createElement('div');

    // استایل‌دهی پیام‌ها
    msgDiv.className = `mb-2 p-2 rounded ${isMe ? 'bg-primary text-white text-start' : 'bg-light text-dark text-end'}`;
    const senderName = isMe ? "شما" : "مسافر";
    msgDiv.innerHTML = `<small class="fw-bold d-block">${senderName}:</small> <span>${message}</span>`;

    chatMessages.appendChild(msgDiv);
    chatMessages.scrollTop = chatMessages.scrollHeight;

    // نوتیفیکیشن چت اگر بسته باشد
    const chatBox = document.getElementById('chatBox');
    if (chatBox && (chatBox.style.display === 'none' || chatBox.style.display === '')) {
        const chatBtn = document.getElementById('btn-open-chat');
        if (chatBtn) {
            chatBtn.className = "btn btn-danger rounded-circle shadow-lg d-flex align-items-center justify-content-center";
        }
        if (typeof notify === "function") notify("💬 پیام جدید از مسافر", "info");
    }
});

// ارسال پیام
async function sendChatMessage() {
    const input = document.getElementById('chatInput');
    const message = input.value.trim();

    if (!message || !activeTripId) return;

    try {
        await connection.invoke("SendChatMessage", activeTripId, message);
        input.value = "";
    } catch (err) {
        console.error("خطا در ارسال پیام:", err);
    }
}

// فعال‌سازی چت هنگام قبول سفر
connection.on("TripAccepted", function (data) {
    activeTripId = data.tripId;
    const btnChat = document.getElementById('btn-open-chat');
    if (btnChat) btnChat.style.display = 'block';
});

// مدیریت وضعیت چت هنگام پایان/لغو
connection.on("ReceiveStatusUpdate", function (message) {
    console.log("Status Update:", message);

    if (message === "Finished" || message === "Canceled") {
        const chatBox = document.getElementById('chatBox');
        const chatBtn = document.getElementById('btn-open-chat');

        if (chatBox) chatBox.style.display = 'none';
        if (chatBtn) chatBtn.style.display = 'none';

        if (message === "Canceled") {
            if (typeof notify === "function") notify("⚠️ مسافر سفر را لغو کرد", "error");
            setTimeout(() => { location.reload(); }, 2000);
        }
    }
});

// باز و بسته کردن چت
function toggleChat() {
    const box = document.getElementById('chatBox');
    const btn = document.getElementById('btn-open-chat');

    if (box.style.display === 'none' || box.style.display === '') {
        box.style.display = 'block';
        if (btn) btn.className = "btn btn-success rounded-circle shadow-lg d-flex align-items-center justify-content-center";
    } else {
        box.style.display = 'none';
    }
}

// ==========================================
// 9. راه‌اندازی نهایی
// ==========================================
// اطمینان از لود شدن نقشه
if (typeof initMap === "function") {
    initMap();
}
startSignalR();