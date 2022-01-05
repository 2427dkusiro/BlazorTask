self.importScripts("./_content/WebResource/SyncWorkerService.js");
self.importScripts("./service-worker-assets.js");
self.addEventListener("install", event => event.waitUntil(OnInstall(event)));
self.addEventListener("activate", event => event.waitUntil(OnActivate(event)));
self.addEventListener("fetch", event => event.respondWith(OnFetch(event)));
self.addEventListener("message", event => OnMessage(event.data));

const cacheNamePrefix = "offline-cache-";
const cacheName = "${cacheNamePrefix}${self.assetsManifest.version}";
const offlineAssetsInclude = [/\.dll$/, /\.pdb$/, /\.wasm/, /\.html/, /\.js$/, /\.json$/, /\.css$/, /\.woff$/, /\.png$/, /\.jpe?g$/, /\.gif$/, /\.ico$/, /\.blat$/, /\.dat$/, /\.ico$/];
const offlineAssetsExclude = [/^service-worker\.js$/];

async function OnInstall(event) {
    console.info("Service worker: Install");

    // Instantly apply service worker change
    event.waitUntil(self.skipWaiting());

    // Fetch and cache all matching items from the assets manifest
    const assetsRequests = self.assetsManifest.assets
        .filter(asset => offlineAssetsInclude.some(pattern => pattern.test(asset.url)))
        .filter(asset => !offlineAssetsExclude.some(pattern => pattern.test(asset.url)))
        .map(asset => new Request(asset.url, { integrity: asset.hash, cache: 'no-cache' }));

    await caches.open(cacheName).then(cache => cache.addAll(assetsRequests));
}

async function OnActivate(event) {
    console.info("Service worker: Activate");

    // Enable service worker even if first time.
    event.waitUntil(self.clients.claim());

    const cacheKeys = await caches.keys();
    await Promise.all(cacheKeys
        .filter(key => key.startsWith(cacheNamePrefix) && key !== cacheName)
        .map(key => caches.delete(key)));
}

async function OnFetch(event) {
    if (IsSpecial(event.request)) {
        let response = await GetSpecialResponse(event.request);
        return response;
    }
    return await fetch(event.request);
}

/*
function GetMIMEType(url) {
    if (url.endsWith(".dll") || url.endsWith(".pdb")) {
        return "application/octet-stream";
    }
    if (url.endsWith(".wasm")) {
        return "application/wasm";
    }
    if (url.endsWith(".html")) {
        return "text/html";
    }
    if (url.endsWith(".js")) {
        return "text/javascript";
    }
    if (url.endsWith(".json")) {
        return "application/json";
    }
    if (url.endsWith(".css")) {
        return "text/css";
    }
    if (url.endsWith(".woff")) {
        return "font/woff";
    }
}
*//* Manifest version: /xbO3FqH */
