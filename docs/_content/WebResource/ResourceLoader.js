//@ts-check

class Loader {
    /** @type Cache */
    ResourceCache;

    /** @type readonly Request[] */
    ResourceCacheKeys;

    /** @type boolean */
    IsCacheAvailable;

    /** @type boolean */
    IsCacheInitializeTryed;

    /** @type boolean */
    UseCache

    /** @type string */
    CacheName;

    /** @type boolean */
    UseResourceDecoder;

    /** @type string */
    ResourceSuffix;

    /** @type string */
    ResourceDecodeMathodName;

    /**
     * Create a new instance of this class.
     * @param {boolean} _useCache
     * @param {string} _cacheName
     * @param {boolean} _useResourceDecoder
     * @param {string} _resourceSuffix
     * @param {string} _resourceDecodeMethodName
     */
    constructor(_useCache, _cacheName, _useResourceDecoder, _resourceSuffix, _resourceDecodeMethodName) {
        this.IsCacheAvailable = false;
        this.IsCacheInitializeTryed = false;
        this.UseCache = _useCache;
        this.CacheName = _cacheName;
        this.UseResourceDecoder = _useResourceDecoder;
        this.ResourceSuffix = _resourceSuffix;
        this.ResourceDecodeMathodName = _resourceDecodeMethodName;
    }

    /**
     * Fetch resource by configured way.
     * @param {string} filePath
     * @returns {Promise<Uint8Array | Int8Array>}
     */
    async FetchResourceArray(filePath) {
        const cache = await this.FetchFromCache(filePath);
        if (cache != null) {
            const buffer = await cache.arrayBuffer();
            return new Uint8Array(buffer, 0);
        }

        const encoded = await this.FetchFromEncoded(filePath);
        if (encoded != null) {
            return encoded;
        }

        const base = await this.Fetch(filePath);
        if (base != null) {
            const buffer = await base.arrayBuffer();
            return new Uint8Array(buffer, 0);
        }

        throw new Error("Failed to fetch '" + filePath + "'.");
    }

    /**
     * Fetch resource by configured way.
     * @param {string} filePath
     * @returns {Promise<Response>}
     */
    async FetchResourceResponce(filePath) {
        const cache = await this.FetchFromCache(filePath);
        if (cache != null) {
            return cache;
        }

        const encoded = await this.FetchFromEncoded(filePath);
        if (encoded != null) {
            const type = this.GetMIMEType(this.GetFileName(filePath));
            return new Response(encoded.buffer, { status: 200, headers: { "content-type": type } });
        }

        const base = await this.Fetch(filePath);
        if (base != null) {
            return base;
        }

        throw new Error("Failed to fetch '" + filePath + "'.");
    }


    /**
     * Fetch resource from cache. If not found, returns null.
     * @param {string} filePath
     * @returns {Promise<Response>}
     * @private
     */
    async FetchFromCache(filePath) {
        if (!this.IsCacheInitializeTryed) {
            await this.InitializeCache(this.UseCache, this.CacheName);
        }
        if (this.IsCacheAvailable) {
            const cacheresponse = await this.SearchCache(filePath);
            return cacheresponse;
        }
        return null;
    }

    /**
     * Fetch resource from encoded resource. If encoder not configured, returns null.
     * @param {string} filePath
     * @returns {Promise<Uint8Array | Int8Array>}
     * @private
     */
    async FetchFromEncoded(filePath) {
        if (!this.UseResourceDecoder) {
            return null;
        }
        if (!filePath.includes("/_framework/")) {
            return null;
        }
        const response = await fetch(filePath + this.ResourceSuffix);
        if (!response.ok) {
            console.warn("failed to fetch encoded resource. Fall back to fetch not encoded.");
            return null;
        }
        const array = await response.arrayBuffer();
        const func = globalThis[this.ResourceDecodeMathodName]();
        const result = func.call(null, new Int8Array(array));

        const toString = Object.prototype.toString;
        const typeString = toString.call(result);
        if (typeString !== "[object Uint8Array]" && typeString !== "[object Int8Array]") {
            console.warn("assertion failed: resource decoder returns unexpected type '" + typeString + "'.");
        }
        return result;
    }

    async Fetch(filePath) {
        const response = await fetch(filePath);
        if (!response.ok) {
            return null;
        }
        return response;
    }

    /**
     * Initialize cache system
     * @param _useCache {boolean}
     * @param _cacheName {string}
     * @returns {Promise<void>}
     * @private
     * */
    async InitializeCache(_useCache, _cacheName) {
        this.IsCacheInitializeTryed = true;

        if (_useCache) {
            if (this.ResourceCache == null) {
                const keys = await caches.keys();
                for (let i = 0; i < keys.length; i++) {
                    if (keys[i] === _cacheName) {
                        this.ResourceCache = await caches.open(keys[i]);
                        break;
                    }
                }
                if (this.ResourceCache == null) {
                    console.warn("cache '" + _cacheName + "' is not exists.");
                    return;
                }
            }
            if (this.ResourceCacheKeys == null) {
                this.ResourceCacheKeys = await this.ResourceCache.keys();
                if (this.ResourceCacheKeys == null || this.ResourceCacheKeys.length == 0) {
                    return;
                } else {
                    this.IsCacheAvailable = true;
                    return;
                }
            }
        }
    }

    /**
     * Search resource from resource cache. If cache is not hit, returns null.
     * @param {string} filePath file path to serach.
     * @returns {Promise<Response>}
     * @private
     */
    async SearchCache(filePath) {
        let key;
        for (let i = 0; i < this.ResourceCacheKeys.length; i++) {
            if (this.ResourceCacheKeys[i].url.startsWith(filePath + ".sha256-")) {
                key = this.ResourceCacheKeys[i];
            }
        }
        //TODO: should I check the integrity of cache?
        return await this.ResourceCache.match(key);
    }

    /**
     * Get filename with extention from url string.
     * @param {string} url
     * @returns {string}
     * @private
     */
    GetFileName(url) {
        let filename_ex = url.substring(url.lastIndexOf("/"));
        if (filename_ex === "") {
            filename_ex = "index.html";
        }
        return filename_ex;
    }

    GetMIMEType(fileName) {
        if (fileName.endsWith(".dll") || fileName.endsWith(".pdb")) {
            return "application/octet-stream";
        }
        if (fileName.endsWith(".wasm")) {
            return "application/wasm";
        }
        if (fileName.endsWith(".html")) {
            return "text/html";
        }
        if (fileName.endsWith(".js")) {
            return "text/javascript";
        }
        if (fileName.endsWith(".json")) {
            return "application/json";
        }
        if (fileName.endsWith(".css")) {
            return "text/css";
        }
        if (fileName.endsWith(".woff")) {
            return "font/woff";
        }
    }
}