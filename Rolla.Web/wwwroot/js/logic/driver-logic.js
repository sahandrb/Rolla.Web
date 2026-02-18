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

    const modalEl = document.getElementById('tripModal');
    // ابتدا چک کن آیا مودال از قبل وجود دارد یا خیر
    let myModal = bootstrap.Modal.getInstance(modalEl);
    if (!myModal) {
        // اگر نبود، یکی جدید بساز
        myModal = new bootstrap.Modal(modalEl);
    }
    myModal.show();
});
async function acceptTrip() {
    try {
        const res = await fetch(`/api/TripApi/accept/${currentOfferId}`, {
            method: 'POST'
        });

        if (res.ok) {
            // ... (کدهای موفقیت قبلی) ...
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
            // ✨ مدیریت همزمانی
            // اگر ریسپانس 400 یا خطا بود
            alert("❌ متاسفانه سفر توسط راننده دیگری رزرو شد.");

            // بستن مودال
            const modalElement = document.getElementById('tripModal');
            const modalInstance = bootstrap.Modal.getInstance(modalElement);
            if (modalInstance) modalInstance.hide();
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


async function sendFinish() {
    if (!confirm("آیا مطمئن هستید؟")) return;

    try {
        const res = await fetch(`/api/TripApi/finish/${activeTripId}`, { method: 'POST' });
        if (res.ok) {
            alert("✅ سفر تمام شد.");

          
            document.getElementById('chatBox').style.display = 'none';
            document.getElementById('btn-open-chat').style.display = 'none';

            location.reload();
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

// وقتی مودال باز می‌شود، دکمه Reject را صدا می‌زنیم
function rejectTrip() {
    if (!currentOfferId) return;

    fetch(`/api/TripApi/reject/${currentOfferId}`, {
        method: 'POST'
    })
        .then(res => {
            if (res.ok) {
                // بستن مودال
                const modalElement = document.getElementById('tripModal');
                const modalInstance = bootstrap.Modal.getInstance(modalElement);
                if (modalInstance) modalInstance.hide();

                // پاک کردن متغیر پیشنهاد فعلی
                currentOfferId = null;
            }
        })
        .catch(err => console.error(err));
}

// مدیریت چت




// دریافت پیام از سیگنال‌آر
connection.on("ReceiveChatMessage", function (senderId, message) {
    const chatMessages = document.getElementById('chatMessages');
    const isMe = connection.connectionId === senderId; // ساده‌سازی شده

    const msgDiv = document.createElement('div');
    msgDiv.className = `mb-2 p-2 rounded ${isMe ? 'bg-light text-end' : 'bg-primary text-white text-start'}`;
    msgDiv.innerHTML = `<strong>${isMe ? 'من' : 'طرف مقابل'}:</strong> <br/> ${message}`;

    chatMessages.appendChild(msgDiv);
    chatMessages.scrollTop = chatMessages.scrollHeight; // اسکرول به پایین

    // اگر چت بسته بود، دکمه را چشمک‌زن کن
    if (document.getElementById('chatBox').style.display === 'none') {
        document.getElementById('btn-open-chat').className = "btn btn-danger rounded-circle shadow";
    }
});

// نمایش دکمه چت وقتی سفر قبول شد
connection.on("TripAccepted", function (data) {
    activeTripId = data.tripId;
    document.getElementById('btn-open-chat').style.display = 'block';
});


// گوش دادن به تغییرات وضعیت سفر (مثل رسیدن پیام لغو از مسافر)
connection.on("ReceiveStatusUpdate", function (message) {
    console.log("وضعیت جدید دریافت شد:", message);

    // اگر سفر تمام یا لغو شد، چت را ببند و مخفی کن
    if (message === "Finished" || message === "Canceled") {
        const chatBox = document.getElementById('chatBox');
        const chatBtn = document.getElementById('btn-open-chat');
        if (chatBox) chatBox.style.display = 'none';
        if (chatBtn) chatBtn.style.display = 'none';

        if (message === "Canceled") {
            alert("⚠️ مسافر سفر را لغو کرد.");
            location.reload(); // بازگشت به حالت عادی
        }
    }
});



async function sendChatMessage() {
    const input = document.getElementById('chatInput');
    const message = input.value.trim();

    // activeTripId همان متغیری است که موقع قبول سفر پر کردیم
    if (!message || !activeTripId) return;

    try {
        await connection.invoke("SendChatMessage", activeTripId, message);
        input.value = "";
    } catch (err) {
        console.error("خطا در ارسال پیام:", err);
    }
}

// گوش دادن به پیام‌های دریافتی
connection.on("ReceiveChatMessage", function (senderId, message) {
    const chatMessages = document.getElementById('chatMessages');

    // تشخیص اینکه پیام از سمت خودمان است یا مسافر
    // چون در راننده هستیم، اگر یوزر آیدی فرستنده با ما یکی نباشد، یعنی مسافر فرستاده
    const isMe = connection.connectionId === senderId;

    const msgDiv = document.createElement('div');
    msgDiv.className = `mb-2 p-2 rounded shadow-sm ${isMe ? 'bg-white text-end ms-5' : 'bg-success text-white text-start me-5'}`;
    msgDiv.style.borderRadius = "15px";

    msgDiv.innerHTML = `<small style="font-size:10px; opacity:0.8;">${isMe ? 'من' : 'مسافر'}</small><br/>${message}`;

    chatMessages.appendChild(msgDiv);
    chatMessages.scrollIntoView({ behavior: 'smooth', block: 'end' });
    chatMessages.scrollTop = chatMessages.scrollHeight;

    // نوتیفیکیشن روی دکمه اگر چت بسته بود
    if (document.getElementById('chatBox').style.display === 'none') {
        document.getElementById('btn-open-chat').className = "btn btn-danger rounded-circle shadow-lg animate-bounce"; // تغییر رنگ به قرمز
    }
});

// نمایش دکمه چت به محض قبول سفر (این کد را می‌توانید داخل تابع acceptTrip هم بگذارید)
// اما چون از سمت سرور هم پیام تایید می‌آید، اینجا مطمئن‌تر است:
connection.on("TripAccepted", function (data) {
    document.getElementById('btn-open-chat').style.display = 'block';
});
// شروع اولیه
initMap();
startSignalR();