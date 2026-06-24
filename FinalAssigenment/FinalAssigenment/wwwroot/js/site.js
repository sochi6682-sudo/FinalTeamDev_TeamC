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
    updateCommandEquipStatus();

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
// 搬送指示送信画面
//＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝

function initCommandSendPage() {

    const page = document.getElementById("page-command-send");

    if (!page) return;

    document.getElementById("btn-inbound")
        .addEventListener("click", sendInboundCommand);

    document.getElementById("btn-outbound")
        .addEventListener("click", sendOutboundCommand);

    updateCommandSend();

    setInterval(updateCommandSend, 5000);
}

//＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝
// 画面更新
//＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝

function updateCommandSend() {

    updateShelfSelect();

    updateCommandEquipStatus();
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

    latestShelves.forEach(shelf => {

        const option =
            document.createElement("option");

        option.value = shelf.shelfLocation;
        option.textContent = shelf.shelfLocation;

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
                commandType: 0,
                shelfLocation: shelfId,
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
                commandType: 1,
                shelfLocation: null,
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
// 設備状態更新
//＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝

function updateCommandEquipStatus() {

    const area =
        document.getElementById("command-equip-status");

    if (!area) return;

    area.innerHTML = "";

    latestEquipments.forEach(equipment => {

        const shelves = latestShelves.filter(x =>
            x.equipmentNo === equipment.eqpName);

        const hasInboundCommand =
            latestCommands.some(command =>
                command.equipmentNo === equipment.eqpName &&
                command.commandType === 0 &&
                (command.commandStatus === 0 ||
                    command.commandStatus === 1));

        const inboundOK =
            equipment.controlState === "Online" &&
            !hasInboundCommand;

        const outboundOK =
            equipment.controlState === "Online" &&
            shelves.some(x => x.carrierId);

        const div = document.createElement("div");

        div.className = "command-equip-row";

        div.innerHTML = `
    <div class="command-equip-name">
        ${equipment.eqpName}
    </div>

    <div class="command-status-box
        ${inboundOK ? "available" : "unavailable"}">
        入庫可能
    </div>

    <div class="command-status-box
        ${outboundOK ? "available" : "unavailable"}">
        出庫可能
    </div>
`;

        area.appendChild(div);
    });
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

// ===============================
// 出庫完了報告
// ===============================
function initOutboundCompletePage() {

    if (!document.getElementById("page-outbound-complete")) return;

    document.getElementById("btn-back").addEventListener("click", () => {
        window.location.href = "/"; // メニュー画面へ戻る
    });

    fetchOutboundData();
    setInterval(fetchOutboundData, 1000);
}

// ===============================
// DB から棚情報・コマンド情報取得
// ===============================
async function fetchOutboundData() {
    const [shelfRes, cmdRes] = await Promise.all([
        fetch("/api/shelf-system/shelves"),
        fetch("/api/shelf-system/commands")
    ]);

    latestShelves = await shelfRes.json();
    latestCommands = await cmdRes.json();

    updateOutboundCompleteList();
}

// ===============================
// 出庫完了報告リスト更新
// ===============================
function updateOutboundCompleteList() {

    const area = document.getElementById("outbound-complete-list");
    area.innerHTML = "";

    // 必ず表示する棚リスト
    const shelfGroups = ["01", "02", "03"];

    shelfGroups.forEach(group => {

        // この棚に属する棚データ（あれば）
        const shelves = latestShelves.filter(s => s.shelfLocation.startsWith(group));

        // 出庫コマンド（あれば）
        const cmd = latestCommands.find(c =>
            c.commandType === 1 &&
            shelves.some(s => s.carrierId === c.carrierId)
        );

        // オフライン判定（設備があれば）
        const eqp = latestEquipments.find(e => e.eqpName.endsWith(group));
        const isOffline = eqp && eqp.controlState !== "Online";

        const div = document.createElement("div");
        div.className = "outbound-item";

        div.innerHTML = `
            <div class="outbound-title">保管棚${group}</div>

            <div class="outbound-row">
                <span class="outbound-label">CommandID:</span>
                <span>${cmd ? cmd.commandId : "-"}</span>
            </div>

            <div class="outbound-row">
                <span class="outbound-label">CarrierID:</span>
                <span>${cmd ? cmd.carrierId : "-"}</span>
            </div>

            <div class="outbound-row">
                <span class="outbound-label">状態:</span>
                <span>
                    <div class="lamp-complete ${isOffline ? "lamp-offline" : ""}"></div>
                </span>
            </div>

            <button class="btn-complete ${(!cmd || isOffline) ? "disabled" : ""}">
                ✓ 払出完了
            </button>
        `;

        // ボタン動作（cmd が無い or オフラインなら無効）
        const btn = div.querySelector(".btn-complete");
        if (cmd && !isOffline) {
            btn.addEventListener("click", () => sendOutboundComplete(cmd, btn));
        }

        area.appendChild(div);
    });
}


// ===============================
// 払出完了 POST
// ===============================
async function sendOutboundComplete(cmd, btn) {

    if (!cmd) return;

    const res = await fetch("/api/shelf-system/outbound-complete", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ commandId: cmd.commandId })
    });

    if (res.ok) {
        btn.classList.add("disabled");
        btn.textContent = "完了済み";
    }
}

// ===============================
document.addEventListener("DOMContentLoaded", initOutboundCompletePage);


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

    const filteredShelves = latestShelves.filter(shelf => {
        const location = String(shelf.shelfLocation ?? shelf.location ?? "");
        return location.startsWith(inventoryEqpFilter);
    });

    area.innerHTML = `
        <div class="rack-title">
            保管棚 ${inventoryEqpFilter}
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
            card.className = hasStock ? "shelf-card stock-exists" : "shelf-card stock-empty";

            card.innerHTML = `
                <div class="shelf-location">棚番号：${location}</div>

                <div class="shelf-stock-badge">
                    ${hasStock ? "在荷あり" : "在荷なし"}
                </div>

                <div class="shelf-row">
                    <span>CarrierID</span>
                    <strong>${hasStock ? carrierId : "-"}</strong>
                </div>

                <div class="shelf-row">
                    <span>入庫日時</span>
                    <strong class="datetime">2026/06/24<br>09:15</strong>
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
