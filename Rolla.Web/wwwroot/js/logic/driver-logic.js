// wwwroot/js/logic/driver-logic.js

const connection = new signalR.HubConnectionBuilder()
    .withUrl("/rideHub")
    .withAutomaticReconnect([0, 2000, 10000, 30000]) // بازتلاش هوشمند
    .build();

let isOnline = false;
let locationInterval;

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

            // به روز رسانی مارکر خود راننده روی نقشه
            if (userMarker) map.removeLayer(userMarker);
            userMarker = L.marker([latitude, longitude]).addTo(map);
            map.setView([latitude, longitude]);

            // ارسال به سرور (بافر)
            connection.invoke("UpdateDriverLocation", latitude, longitude, null)
                .catch(err => console.error(err));

        }, err => console.error(err), { enableHighAccuracy: true });
    }, 3000); // هر 3 ثانیه
}

function stopSendingLocation() {
    clearInterval(locationInterval);
}

// شروع
initMap(); // از map-base.js می‌آید
startSignalR();