self.importScripts("./_content/WebResource/SyncWorkerService.js");
self.importScripts("./service-worker-assets.js");
self.addEventListener("install", event => event.waitUntil(OnInstall(event)));
self.addEventListener("activate", event => event.waitUntil(OnActivate(event)));
self.addEventListener("fetch", event => event.respondWith(OnFetch(event)));
self.addEventListener("message", event => OnMessage(event.data));


async function OnInstall(event) {
    console.info("Service worker: Install");

    // Instantly apply service worker change
    event.waitUntil(self.skipWaiting());
}

async function OnActivate(event) {
    console.info("Service worker: Activate");

    // Enable service worker even if first time.
    event.waitUntil(self.clients.claim());
}

async function OnFetch(event) {
    if (IsSpecial(event.request)) {
        let response = await GetSpecialResponse(event.request);
        return response;
    }
    return await fetch(event.request);
}