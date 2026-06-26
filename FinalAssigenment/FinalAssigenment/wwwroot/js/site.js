let serverOnline = true;
let latestShelves = [];
let latestCommands = [];
let latestEquipments = [];

document.addEventListener('DOMContentLoaded', () => {

    // 仮データ
    latestShelves = [
        {
            shelfLocation: "101",
            carrierId: "CAR000001",
            equipmentNo: "自動保管棚01"
        },
        {
            shelfLocation: "201",
            carrierId: "CAR000002",
            equipmentNo: "自動保管棚02"
        },
        {
            shelfLocation: "301",
            carrierId: null,
            equipmentNo: "自動保管棚03"
        }
    ];

    latestEquipments = [
        {
            eqpName: "自動保管棚01",
            controlState: "ONLINE"
        },
        {
            eqpName: "自動保管棚02",
            controlState: "ONLINE"
        },
        {
            eqpName: "自動保管棚03",
            controlState: "OFFLINE"
        }
    ];

    latestCommands = [
        {
            commandId: 1,
            carrierId: "CAR000001",
            equipmentNo: "自動保管棚01",
            commandType: 0,
            commandStatus: 0
        }
    ];

    updateShelfSelect();

    // API接続する場合
    startPolling();

    // イベント設定
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
        latestEquipments = data.states ?? [];

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
    updateOutboundReport();
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
// 搬送指示送信画面
//＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝

function initCommandSendPage() {

    const page = document.getElementById("page-command-send");

    if (!page) return;

    document.getElementById("btn-inbound")
        .addEventListener("click", sendInboundCommand);

    document.getElementById("btn-outbound")
        .addEventListener("click", sendOutboundCommand);

    updateShelfSelect();

}



//＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝
// キャリアID更新
//＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝

function updateShelfSelect() {
    const select =
        document.getElementById("shelfIdSelect");

    if (!select) return;

    select.innerHTML =
        '<option value="">選択してください</option>';
    const eqpList = ["EQP01", "EQP02", "EQP03"];
    eqpList.forEach(eqpName => {
        const option = document.createElement("option");

        option.value = eqpName;
        option.textContent = eqpName;

        select.appendChild(option);
    });
}

//＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝
// 入庫指示
//＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝

async function sendInboundCommand() {

    const carrierId =
        document.getElementById("carrierIdInput").value;

    const shelfId =
        document.getElementById("shelfIdSelect").value;

    if (!carrierId || !shelfId) {

        showResult("キャリアIDと棚IDを選択してください");

        return;
    }

    const response = await fetch(
        "/api/shelf-system/command",
        {
            method: "POST",
            headers: {
                "Content-Type": "application/json"
            },
            body: JSON.stringify({
                commandType: 1,
                eqpName: shelfId,
                carrierId: carrierId
            })
        });

    if (response.ok) {
        showResult("入庫指示を送信しました");
    }
    else {
        showResult("入庫指示に失敗しました");
    }
}

//＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝
// 出庫指示
//＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝

async function sendOutboundCommand() {

    const carrierId =
        document.getElementById("carrierIdInput").value;
    const shelfId =
        document.getElementById("shelfIdSelect").value;
    if (!carrierId) {

        showResult("キャリアIDを選択してください");

        return;
    }

    const response = await fetch(
        "/api/shelf-system/command",
        {
            method: "POST",
            headers: {
                "Content-Type": "application/json"
            },
            body: JSON.stringify({
                commandType: 0,
                eqpName: shelfId,
                carrierId: carrierId
            })
        });

    if (response.ok) {
        showResult("出庫指示を送信しました");
    }
    else {
        showResult("出庫指示に失敗しました");
    }
}



//＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝
// 結果表示
//＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝

function showResult(message) {

    const area =
        document.getElementById("resultArea");

    if (!area) return;

    area.textContent = message;
}

//＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝

document.addEventListener(
    "DOMContentLoaded",
    initCommandSendPage);

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
                command.commandStatus === 3 || 
                command.commandStatus === 4;

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
    if (status === 0) return "実行待ち";
    if (status === 1) return "実行中";
    if (status === 2) return "正常完了";
    if (status === 3) return "異常完了";
    if (status === 4) return "払出完了";
    return "UNKNOWN";
}

function getCommandStatusClass(status) {
    if (status === 0) return "status-queued";
    if (status === 1) return "status-active";
    if (status === 2) return "status-complete";
    if (status === 3) return "status-failed";
    if (status === 4) return "status-complete";
    return "";
}




// ===============================
// 出庫完了報告
// ===============================
function initOutboundReportPage() {
    const page = document.getElementById("page-outbound-report"); if (!page) return; updateOutboundReport();
}
document.addEventListener("DOMContentLoaded", initOutboundReportPage);

function updateOutboundReport() {

    const area = document.getElementById("outbound-report-list");
    if (!area) return;
    area.innerHTML = "";
    const eqpNames = ["EQP01", "EQP02", "EQP03"];
    eqpNames.forEach(eqpName => {
        const shelf = latestShelves.find(s => {
            const locStr = String(s.shelfLocation || "");
            if (eqpName === "EQP01") return locStr.startsWith("1");
            if (eqpName === "EQP02") return locStr.startsWith("2");
            if (eqpName === "EQP03") return locStr.startsWith("3");
        });

        const command = latestCommands.find(x =>
            x.eqpName === eqpName &&
            x.commandType === 0 &&
            x.commandStatus === 2
        );

        const completed = command != null;

        const div = document.createElement("div");
        div.className = "outbound-item";
        div.innerHTML = `
            <div class="outbound-info">

                <div class="outbound-shelf">
                    ${eqpName}
                </div>

                <div class="outbound-data">
                    搬送指示ID<br>
                    ${command ? command.commandId : "----"}
                </div>

                <div class="outbound-data">
                    キャリアID<br>
                    ${command ? command.carrierId : "----"}
                </div>

            </div>

            <div class="outbound-action">

                <div class="status-lamp ${completed ? "on" : "off"}">
                </div>

                <button
                    class="complete-button ${completed ? "completed" : "waiting"}"
                    ${completed ? "" : "disabled"}
                    onclick="completeOutbound('${command ? command.commandId : ""}',
                    '${command ? command.carrierId : ""}', 
                    '${eqpName}')">
                    √ 払出完了
                </button>

            </div>
        `;

        area.appendChild(div);
    });
}
async function completeOutbound(commandId, carrierId, eqpName, buttonElement) {
    if (commandId === 0) {
        return;
    }

    try {
        const response = await fetch('/api/shelf-system/unload', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                CommandId: commandId,
                CarrierId: carrierId,
                EqpName: eqpName
            })
        });
        if (response.status === 200) {
            console.log("払出完了報告に成功しました。");
            if (buttonElement) {
                buttonElement.disabled = true;
                buttonElement.classList.remove("waiting");
                buttonElement.classList.add("completed");

                const row = buttonElement.closest(".outbound-item");
                if (row) {
                    const infoArea = row.querySelector(".outbound-info");

                    if (infoArea) {
                        infoArea.children[1].innerHTML = "CommandID<br>----";
                        infoArea.children[2].innerHTML = "CarrierID<br>----";
                    }
                }
            }
        }
        else if (response.status === 400) {
            console.log("【Warn】400 JSONでPOSTするEqpNameが空白");
        }
        else if (response.status === 404) {
            console.log("【Warn】404 EqpNameが存在しない");
        }
        else if (response.status === 500) {
            console.log("【Erroe】サーバー内部エラーが発生しました。");
        }
    } catch (error) {
        console.error("[Error] サーバ―へ払出完了報告失敗:", error);
    }
}

//＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝
// 在庫一覧更新
//＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝
let inventoryEqpFilter = "1";

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

    const inventoryFilter = document.getElementById("inventory-eqp-filter");
    if (inventoryFilter) {
        inventoryFilter.addEventListener("change", () => {
            inventoryEqpFilter = inventoryFilter.value;
            updateInventoryList();
        });
    }
}

function updateInventoryList() {
    const area = document.getElementById("inventory-rack-area");
    if (!area) return;

    area.innerHTML = "";

    const selectedEqpName = `EQP${inventoryEqpFilter.padStart(2, "0")}`;

    const filteredShelves = latestShelves.filter(shelf => {
        const location = String(shelf.shelfLocation ?? shelf.location ?? "");
        return location.startsWith(inventoryEqpFilter);
    });

    area.innerHTML = `
        <div class="rack-title">
            ${selectedEqpName}
        </div>

        <div class="rack-grid" id="rack-grid">
        </div>
    `;

    const grid = document.getElementById("rack-grid");

    for (let height = 1; height <= 3; height++) {
        for (let column = 1; column <= 2; column++) {
            const location =
                `${inventoryEqpFilter}${String(column).padStart(2, "0")}${String(height).padStart(2, "0")}`;

            const shelf = filteredShelves.find(s => {
                const shelfLocation = String(s.shelfLocation ?? s.location ?? "");
                return shelfLocation === location;
            });

            const carrierId =
                shelf?.storedCarrierId ??
                shelf?.StoredCarrierId ??
                "";

            const storedAt =
                shelf?.storageAt ??
                shelf?.StorageAt ??
                "";

            const hasStock = carrierId !== "";

            const card = document.createElement("div");
            card.className = hasStock
                ? "shelf-card stock-exists"
                : "shelf-card stock-empty";

            card.innerHTML = `
                <div class="shelf-location">${location}</div>

                <div class="shelf-stock-badge">
                    ${hasStock ? "在荷あり" : "在荷なし"}
                </div>

                <div class="shelf-row">
                    <span>キャリアID</span>
                    <strong>${hasStock ? carrierId : "-"}</strong>
                </div>

                <div class="shelf-row">
                    <span>入庫日時</span>
                    <strong class="datetime">
                        ${hasStock ? formatDateTime(storedAt).replace(" ", "<br>") : "-"}
                    </strong>
                </div>
            `;

            grid.appendChild(card);
        }
    }
}

function formatDateTime(value) {
    if (!value) return "-";

    const date = new Date(value);
    if (isNaN(date.getTime())) return value;

    const yyyy = date.getFullYear();
    const mm = String(date.getMonth() + 1).padStart(2, "0");
    const dd = String(date.getDate()).padStart(2, "0");
    const hh = String(date.getHours()).padStart(2, "0");
    const mi = String(date.getMinutes()).padStart(2, "0");

    return `${yyyy}/${mm}/${dd} ${hh}:${mi}`;
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

            <div class="status-row">
                <span class="status-title">通信状態</span>
                <span class="state-chip ${equipment.controlState === 1 ? 'chip-online' : 'chip-off'}">ON-LINE</span>
                <span class="state-chip ${equipment.controlState === 0 ? 'chip-offline' : 'chip-off'}">OFF-LINE</span>
            </div>

            <div class="status-row">
                <span class="status-title">設備状態</span>
                <span class="state-chip ${equipment.equipmentStatus === 1 ? 'chip-active' : 'chip-off'}">ACTIVE</span>
                <span class="state-chip ${equipment.equipmentStatus === 0 ? 'chip-idle' : 'chip-off'}">IDLE</span>
            </div>

            <div class="status-row">
                <span class="status-title">異常状態</span>
                <span class="state-chip ${equipment.alarmStatus === 0 ? 'chip-normal' : 'chip-off'}">NO ALARM</span>
                <span class="state-chip ${equipment.alarmStatus === 1 ? 'chip-alarm' : 'chip-off'}">ALARM</span>
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
