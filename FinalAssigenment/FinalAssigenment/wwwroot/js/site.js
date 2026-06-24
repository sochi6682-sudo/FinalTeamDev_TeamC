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

// ===============================
// 搬送指示送信
// ===============================
function initCommandSendPage() {

    if (!document.getElementById("page-command-send")) return;

    document.getElementById("btn-inbound").addEventListener("click", sendInboundCommand);
    document.getElementById("btn-outbound").addEventListener("click", sendOutboundCommand);

    document.getElementById("carrierIdSelect").addEventListener("change", onCarrierSelected);

    fetchLatestData();
    setInterval(fetchLatestData, 1000);
}

// ===============================
// DB から最新情報取得
// ===============================
async function fetchLatestData() {
    const [equipRes, shelfRes, cmdRes] = await Promise.all([
        fetch("/api/shelf-system/equipments"),
        fetch("/api/shelf-system/shelves"),
        fetch("/api/shelf-system/commands")
    ]);

    latestEquipments = await equipRes.json();
    latestShelves = await shelfRes.json();
    latestCommands = await cmdRes.json();

    updateCarrierSelect();
    updateCommandEquipStatus();
    onCarrierSelected();
}

// ===============================
// キャリアIDプルダウン更新
// ===============================
function updateCarrierSelect() {
    const select = document.getElementById("carrierIdSelect");
    const current = select.value;

    select.innerHTML = `<option value="">選択してください</option>`;

    latestShelves.forEach(s => {
        if (s.carrierId) {
            const opt = document.createElement("option");
            opt.value = s.carrierId;
            opt.textContent = s.carrierId;
            select.appendChild(opt);
        }
    });

    if (current) select.value = current;
}

// ===============================
// キャリア選択 → 棚ID自動表示
// ===============================
function onCarrierSelected() {
    const carrierId = document.getElementById("carrierIdSelect").value;
    const shelfSelect = document.getElementById("shelfIdSelect");

    shelfSelect.innerHTML = `<option value="">自動選択</option>`;

    if (!carrierId) return;

    const shelf = latestShelves.find(s => s.carrierId === carrierId);

    if (shelf) {
        const opt = document.createElement("option");
        opt.value = shelf.shelfLocation;
        opt.textContent = shelf.shelfLocation;
        shelfSelect.appendChild(opt);
        shelfSelect.value = shelf.shelfLocation;
    }
}

// ===============================
// 入庫指示
// ===============================
async function sendInboundCommand() {
    const carrierId = document.getElementById("carrierIdSelect").value;
    const shelfId = document.getElementById("shelfIdSelect").value;

    if (!carrierId || !shelfId) {
        showResult("キャリアID と 棚ID を選択してください");
        return;
    }

    const res = await fetch("/api/shelf-system/command", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
            commandType: 0,
            shelfLocation: shelfId,
            carrierId: carrierId
        })
    });

    showResult(res.ok ? "入庫指示を送信しました" : "入庫指示に失敗しました");
}

// ===============================
// 出庫指示
// ===============================
async function sendOutboundCommand() {
    const carrierId = document.getElementById("carrierIdSelect").value;

    if (!carrierId) {
        showResult("キャリアID を選択してください");
        return;
    }

    const res = await fetch("/api/shelf-system/command", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
            commandType: 1,
            shelfLocation: null,
            carrierId: carrierId
        })
    });

    showResult(res.ok ? "出庫指示を送信しました" : "出庫指示に失敗しました");
}

// ===============================
// 結果表示
// ===============================
function showResult(msg) {
    document.getElementById("resultArea").textContent = msg;
}

// ===============================
// 簡易設備状態（入庫可能 / 出庫可能ランプ）
// ===============================
function updateCommandEquipStatus() {
    const area = document.getElementById("command-equip-status");
    area.innerHTML = "";

    latestEquipments.forEach(eqp => {
        const eqpShelves = latestShelves.filter(s => s.equipmentNo === eqp.eqpName);

        const hasQueuedInbound = latestCommands.some(c =>
            c.equipmentNo === eqp.eqpName &&
            c.commandType === 0 &&
            (c.commandStatus === 0 || c.commandStatus === 1)
        );
        const inboundOK = eqp.controlState === "Online" && !hasQueuedInbound;

        const hasStored = eqpShelves.some(s => s.carrierId !== null);
        const outboundOK = eqp.controlState === "Online" && hasStored;

        const div = document.createElement("div");
        div.className = "command-equip-row";

        div.innerHTML = `
            <div class="command-equip-name">${eqp.eqpName}</div>
            <div class="command-equip-lamp-block">
                <span>入庫</span>
                <div class="lamp-mini ${inboundOK ? "on" : "off"}"></div>
            </div>
            <div class="command-equip-lamp-block">
                <span>出庫</span>
                <div class="lamp-mini ${outboundOK ? "on" : "off"}"></div>
            </div>
        `;

        area.appendChild(div);
    });
}

// ===============================
document.addEventListener("DOMContentLoaded", initCommandSendPage);

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
