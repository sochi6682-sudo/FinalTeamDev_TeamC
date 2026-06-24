let serverOnline = true;
let latestShelves = [];
let latestCommands = [];
let latestEquipments = [];

document.addEventListener('DOMContentLoaded', () => {
    startPolling();
    setupEvents();
});

// 5秒ごとに更新
function startPolling() {
    fetchStatus();
    setInterval(fetchStatus, 5000);
}

// サーバーへGET
async function fetchStatus() {
    try {
        const res = await fetch('/api/shelf-system');

        if (!res.ok) {
            handleServerError();
            return;
        }

        const data = await res.json();

        latestShelves = data.shelves ?? [];
        latestCommands = data.commands ?? [];
        latestEquipments = data.status ?? [];

        updateScreensOn200();
    } catch {
        handleServerError();
    }
}

// 200応答時：画面更新
function updateScreensOn200() {
    serverOnline = true;
    hideServerStopOverlay();
    enableAllControls();

    updateInventoryList();
    updateCommandList();
    updateEquipmentStatus();
    updateEquipmentLamps();
}

// サーバー停止時
function handleServerError() {
    serverOnline = false;
    showServerStopOverlay();
    disableAllControls();
}

//＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝
// 搬送指示一覧更新
//＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝

let commandFilter = "all";

function setupEvents() {
    document.querySelectorAll("[data-command-filter]").forEach(button => {
        button.addEventListener("click", () => {
            commandFilter = button.dataset.commandFilter;

            document.querySelectorAll("[data-command-filter]").forEach(x => {
                x.classList.remove("active");
            });

            button.classList.add("active");

            updateCommandList();
        });
    });
}

function updateCommandList() {

    const area = document.getElementById("command-list");

    if (!area) return;

    area.innerHTML = "";

    const filteredCommands = latestCommands.filter(command => {

        if (commandFilter === "all") return true;

        if (commandFilter === "queued")
            return command.commandStatus === 0;

        if (commandFilter === "active")
            return command.commandStatus === 1;

        if (commandFilter === "history")
            return command.commandStatus === 2 ||
                command.commandStatus === 3;

        return true;
    });

    if (filteredCommands.length === 0) {

        const row = document.createElement("tr");

        row.innerHTML = `
        <td colspan="5" class="empty-message">
            該当する搬送指示はありません
        </td>
    `;

        area.appendChild(row);

        return;
    }

    filteredCommands.forEach(command => {


        const row = document.createElement("tr");

        row.innerHTML = `
            <td>${command.commandId}</td>
            <td>${command.carrierId}</td>
            <td>${command.location}</td>
            <td>${command.commandType === 0 ? "入庫" : "出庫"}</td>
            <td class="${getCommandStatusClass(command.commandStatus)}">
                ${getCommandStatusText(command.commandStatus)}
            </td>
        `;


        area.appendChild(row);
    });


}

function getCommandStatusText(status) {
    if (status === 0) return "QUEUED";
    if (status === 1) return "ACTIVE";
    if (status === 2) return "COMPLETE";
    if (status === 3) return "FAILED";
    return "UNKNOWN";
}

function getCommandStatusClass(status) {
    if (status === 0) return "status-queued";
    if (status === 1) return "status-active";
    if (status === 2) return "status-complete";
    if (status === 3) return "status-failed";
    return "";
}

//＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝
// 在庫一覧更新
//＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝
function updateInventoryList() {
    const area = document.getElementById('inventory-list');
    if (!area) return;

    area.innerHTML = '';

    latestShelves.forEach(shelf => {
        const div = document.createElement('div');
        div.textContent =
            `棚: ${shelf.shelfLocation}, CarrierID: ${shelf.carrierId ?? ''}`;
        area.appendChild(div);
    });
}

//＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝
// 設備状態更新
//＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝
function updateEquipmentStatus() {
    const area = document.getElementById('equipment-status');
    if (!area) return;

    area.innerHTML = '';

    latestEquipments.forEach(equipment => {
        const div = document.createElement('div');
        div.className = "status-card";

        div.innerHTML = `
            <h3>${equipment.eqpName}</h3>

            <div class="status-item">
                <span class="status-label">通信状態</span>
                <span class="${equipment.controlState === 'Online'
                ? 'status-online'
                : 'status-offline'}">
                    ${equipment.controlState}
                </span>
            </div>

            <div class="status-item">
                <span class="status-label">設備状態</span>
                <span>${equipment.equipmentStatus}</span>
            </div>

            <div class="status-item">
                <span class="status-label">異常状態</span>
                <span class="${equipment.alarmStatus === 'Alarm'
                ? 'status-alarm'
                : 'status-normal'}">
                    ${equipment.alarmStatus}
                </span>
            </div>
        `;

        area.appendChild(div);
    });
}

// 設備ランプ更新
function updateEquipmentLamps() {
    document.querySelectorAll(".lamp-inbound").forEach(lamp => {
        const shelfId = lamp.dataset.id;
        const shelf = latestShelves.find(s => s.shelfLocation === shelfId);

        if (shelf &&
            shelf.controlState === "ONLINE" &&
            shelf.storedCarrierId == null &&
            !latestCommands.some(c =>
                c.commandType === 0 &&
                c.shelfLocation === shelfId &&
                c.commandStatus === 0)) {
            lamp.classList.add("on");
        } else {
            lamp.classList.remove("on");
        }
    });
}


//＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝
//オーバーレイ
//＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝
function showServerStopOverlay() {
    const overlay = document.getElementById('serverStopOverlay');
    if (!overlay) return;

    overlay.classList.remove('hidden');
}

function hideServerStopOverlay() {
    const overlay = document.getElementById('serverStopOverlay');
    if (!overlay) return;

    overlay.classList.add('hidden');
}

function disableAllControls() {
    document.querySelectorAll('button, input, select')
        .forEach(x => {
            x.disabled = true;
        });

    document.querySelectorAll('a')
        .forEach(x => {
            x.classList.add('disabled');
        });
}

function enableAllControls() {
    document.querySelectorAll('button, input, select')
        .forEach(x => {
            x.disabled = false;
        });

    document.querySelectorAll('a')
        .forEach(x => {
            x.classList.remove('disabled');
        });
}
