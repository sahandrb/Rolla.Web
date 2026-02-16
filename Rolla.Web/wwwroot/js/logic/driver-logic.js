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

function showTripInfoPanel() {
    // تغییر پنل سمت راست یا پایین
    const statusDiv = document.querySelector('.card-body');
    statusDiv.innerHTML = `
        <h4 class="text-success">🚀 در سفر</h4>
        <p>در حال حرکت به سمت مسافر...</p>
        <button class="btn btn-primary w-100 mb-2" onclick="openWaze()">مسیریابی (Waze)</button>
        <button class="btn btn-warning w-100">رسیدم به مبدا</button>
    `;
}

function openWaze() {
    // اینجا باید مختصات مسافر رو داشته باشیم (فعلا هاردکد شده)
    window.open("https://waze.com/ul?ll=35.71,51.41&navigate=yes");
}

// شروع اولیه
initMap();
startSignalR();