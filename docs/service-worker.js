//@ts-check

self.importScripts("./_content/WebResource/SyncWorkerService.js", "./_content/WebResource/ResourceLoader.js", "workerDecode.min.js");

self.importScripts("./service-worker-assets.js");
self.addEventListener("install", event => event.waitUntil(OnInstall(event)));
self.addEventListener("activate", event => event.waitUntil(OnActivate(event)));
self.addEventListener("fetch", event => event.respondWith(OnFetch(event)));
self.addEventListener("message", event => OnMessage(event));

const cacheNamePrefix = "offline-cache-";
const cacheName = `${cacheNamePrefix}${self.assetsManifest.version}`;
const offlineAssetsInclude = [/\.dll$/, /\.pdb$/, /\.wasm/, /\.html/, /\.js$/, /\.json$/, /\.css$/, /\.woff$/, /\.png$/, /\.jpe?g$/, /\.gif$/, /\.ico$/, /\.blat$/, /\.dat$/, /\.ico$/];
const offlineAssetsExclude = [/^service-worker\.js$/];

const blazorCacheName = "blazor-resources-/BlazorTask/";
const resourceSuffix = ".br";
const resourceDecoderMethodName = "BrotliDecode";
let loader;

async function OnInstall(event) {
    console.info("Service worker: Install");
    if (loader === undefined) {
        loader = new Loader(true, blazorCacheName, true, resourceSuffix, resourceDecoderMethodName);
    }

    // Instantly apply service worker change
    event.waitUntil(self.skipWaiting());

    const cacheStore = await caches.open(cacheName);
    /** @type Array */
    const assets = self.assetsManifest.assets;
    /** @type Promise<void>[] */
    const promises = Array(assets.length);

    for (let i = 0; i < assets.length; i++) {
        const asset = assets[i];
        if (!offlineAssetsInclude.some(x => x.test(asset.url))) {
            continue;
        }
        if (offlineAssetsExclude.some(x => x.test(asset.url))) {
            continue;
        }
        const request = new Request(asset.url, { integrity: asset.hash, cache: "no-cache" });
        /** @type Promise<Response> */
        const response = loader.FetchResourceResponce(asset.url);
        promises[i] = PutCache(cacheStore, request, response);
    }
    await Promise.all(promises);
}

/**
 * 
 * @param {Cache} store
 * @param {Request} request
 * @param {Promise<Response>} promise
 */
async function PutCache(store, request, promise) {
    const response = await promise;
    await store.put(request, response);
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
    if (loader === undefined) {
        loader = new Loader(true, blazorCacheName, true, resourceSuffix, resourceDecoderMethodName);
    }

    /** @type Request */
    const request = event.request;
    if (IsSpecial(event.request)) {
        let response = await GetSpecialResponse(request);
        return response;
    }
    return await loader.FetchResourceResponce(request.url);
}/* Manifest version: Kb/kFkjK */
