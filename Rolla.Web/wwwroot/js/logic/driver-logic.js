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

// ۴. پذیرش سفر (نسخه نهایی)
async function acceptTrip() {
    try {
        const res = await fetch(`/api/TripApi/accept/${currentOfferId}`, {
            method: 'POST'
        });

        if (res.ok) {
            alert("✅ سفر قبول شد! حالا باید به سمت مسافر بروید.");

            // مخفی کردن مودال
            const modalElement = document.getElementById('tripModal');
            const modalInstance = bootstrap.Modal.getInstance(modalElement);
            modalInstance.hide();

            // فعال کردن حالت "در سفر"
            isWorkingOnTrip = true;
            activeTripId = currentOfferId;

            // به راننده بگوییم در گروه سفر در SignalR عضو شود
            await connection.invoke("JoinTripGroup", activeTripId);

        } else {
            alert("❌ متاسفانه سفر منقضی شده یا توسط راننده دیگری گرفته شده است.");
        }
    } catch (err) {
        console.error("Error accepting trip:", err);
    }
}

// شروع اولیه
initMap();
startSignalR();