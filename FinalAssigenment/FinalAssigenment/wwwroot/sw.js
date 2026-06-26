self.addEventListener("install", event => {
    console.log("Service Worker Install");
    self.skipWaiting();
});

self.addEventListener("activate", event => {
    console.log("Service Worker Activate");
    event.waitUntil(self.clients.claim());
});

self.addEventListener("fetch", event => {
    // 今回はキャッシュ処理なし
});