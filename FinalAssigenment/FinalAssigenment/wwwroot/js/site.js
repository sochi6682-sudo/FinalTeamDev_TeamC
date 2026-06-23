const { hide } = require("@popperjs/core");

let servarOnline = true;
let latestShelves = [];
let latestCommands = [];
let latestEquipments = [];

window.onload = () => {
    startPolling();
    setupEvents();
};

//5秒ごとに更新
function startPolling() {
    setInterval(fetchStatus, 5000);
}

//サーバーへGET
async function fetchStatus() {
    try {
        const res = await fetch('/api/shlf-system/');
        if (!res.ok) {
            handleServerError();
            return;
        }

        const data = await res.json();
        latestShelves = data.shelves;
        latestCommands = data.commands;
        latestEquipments = data.equipments;

        updateScreensOn200();
    } catch {
        handleServerError();
    }
}

//200応答時：画面更新
function updateScreensOn200() {
    servarOnline = true;
    hideServerStopOverlay();
    enableAllControls();

    updateInventoryList();
    updateCommandList();
    updateEquipmentStatus();
    updateEquipmentLamps();
}

//サーバー停止時
function handleServerError() {
    servarOnline = false;
    showServerStopOverlay();
    disableAllControls();
}

//搬送指示一覧更新
function updateCommandList() {
    const area = document.getElementById('command-list');
    area.innerHTML = '';

    latestCommands.forEach(command => {
        const div = document.createElement('div');
        div.textContent = `Command ID: ${command.id}, Status: ${command.status}`;
        area.appendChild(div);
    });
}

//在庫一覧更新
function updateInventoryList() {
    const area = document.getElementById('inventory-list');
    area.innerHTML = '';

    latestShelves.forEach(shelf => {
        const div = document.createElement('div');
        div.textContent = `Shelf ID: ${shelf.id}, Quantity: ${shelf.quantity}`;
        area.appendChild(div);
    });
}

//設備状態更新
function updateEquipmentStatus() {
    const area = document.getElementById('equipment-status');
    area.innerHTML = '';

    latestEquipments.forEach(equipment => {
        const div = document.createElement('div');
        div.innerHTML = `
            <h3>${eq.eqpName}</h3>
            ONLINE: ${eq.controlState}<br>
            ACTIVE: ${eq.equipmentStatus}<br>
            ALARM: ${eq.alarmStatus}<br>
        `;
        area.appendChild(div);
    });
}
//設備ランプ更新
function updateEquipmentLamps() {
    document.querySelectorAll(".lamp-inbound").forEach(l => {
        const shelfId = l.dataset.id;
        const shelf = latestShelves.find(s => s.shelfLocation === shelfId);

        if (shelf &&
            shelf.controlState === "ONLINE" &&
            shelf.storedCarrierId == null &&
            !latestCommands.some(c => c.commandType === 0 && c.shelfLocation === shelfId && c.commandStatus === 0)) {
            l.classList.add("on");
        } else {
            l.classList.remove("on");
        }
    });
}

//POST：入庫

