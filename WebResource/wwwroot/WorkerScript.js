﻿// @ts-check

/** @type string */
let basePath;

/** @type string */
let jsExecutePath;

/** @type string */
let frameworkDirName;

/** @type string */
let appBinDirName;

/** @type string */
let dotnetJsName;

/** @type string */
let dotnetWasmName;

/** @type string */
let resourceDecoderPath;

/** @type string */
let resourceDecodeMathodName;

/** @type string */
let resourceSuffix;

/** @type string[] */
let dotnetAssemblies;

/** @type boolean */
let useCache;

// When undefined, use browser provided locale string.
let dotnetCulture = undefined;

// When undefined, use browser provided timezone string.
let timeZoneString = undefined;

let timeZoneFileName = "dotnet.timezones.blat";

let decoderModule;

let jsTextDecoderModule;

self.onmessage = (/** @type MessageEvent */ eventArg) => {
    self.onmessage = OnMessageReceived;
    ConfigureThis(eventArg);
    ImportModules().then(() => InitializeRuntime());
}

/**
 * Configure this object from passed setting info.
 * @param {MessageEvent} eventArg
 * @returns {void}
 */
function ConfigureThis(eventArg) {
    const array = new Uint8Array(eventArg.data[0], 0);
    const str = array.length > nativeLen ? nativeDecoder.decode(array) : jsTextDecoderModule.Decode(array);

    /**@type WorkerInitializeSetting */
    const setting = JSON.parse(str);
    basePath = setting.BasePath;
    jsExecutePath = setting.JSExecutePath;
    frameworkDirName = setting.FrameworkDirName;
    appBinDirName = setting.AppBinDirName;
    dotnetJsName = setting.DotnetJsName;
    dotnetWasmName = setting.DotnetWasmName;
    resourceDecoderPath = setting.ResourceDecoderPath;
    resourceDecodeMathodName = setting.ResourceDecodeMathodName;
    resourceSuffix = setting.ResourcePrefix;
    useCache = setting.UseResourceCache;
    dotnetAssemblies = setting.Assemblies;

    if (dotnetCulture == undefined) {
        dotnetCulture = Intl.DateTimeFormat().resolvedOptions().locale;
    }
    if (timeZoneString == undefined) {
        timeZoneString = Intl.DateTimeFormat().resolvedOptions().timeZone;
    }
}

/**
 * Import modules here(by dynamic import).
 * @returns {Promise<void>} 
 */
async function ImportModules() {
    jsTextDecoderModule = await import("./TextDecoder.js");
    if (resourceDecoderPath != null) {
        decoderModule = await import(BuildPath(resourceDecoderPath));
    }
}

/**
 * Invoke initialize logics. This method should call once.
 * @private
 * @returns {void}
 */
function InitializeRuntime() {
    /** @type ModuleType */
    const _Module = {};
    _Module.print = WriteStdOut;
    _Module.printErr = WriteStdError;
    _Module.locateFile = LocateFile;
    _Module.instantiateWasm = InstantiateWasm;
    _Module.preRun = [];
    _Module.postRun = [];
    _Module.preloadPlugins = [];
    _Module.preRun.push(PreRun);
    _Module.postRun.push(PostRun);

    global = globalThis;
    self.Module = _Module;

    self.importScripts(BuildFrameworkPath(dotnetJsName));
}

/**
 * Implements worker's stdout.
 * @private
 * @param {string} message message to write.
 * @returns {void}
 */
function WriteStdOut(message) {
    console.log("workerstdout:" + message);
}

/**
 * Implements worker's stderror.
 * @private
 * @param {any} message message to write
 * @returns {void}
 */
function WriteStdError(message) {
    console.log("workerstderror:" + message);
}

/**
 * Provides custom logic to locate file.
 * @private
 * @param {string} fileName filename about to load.
 * @returns {string} new filepath.
 */
function LocateFile(fileName) {
    if (fileName == "dotnet.wasm") {
        return BuildFrameworkPath(dotnetWasmName);
    }
    return fileName;
}

/**
 * see MonoPlatform.ts line:269
 * @param {WebAssembly.Imports} imports
 * @param {function(WebAssembly.Instance):void} successCallback
 */
function InstantiateWasm(imports, successCallback) {
    (async () => {
        /** @type WebAssembly.Instance */
        let compiledInstance;
        try {
            if (!cacheInitializeTryed) {
                await InitializeCache();
            }
            // if cache available or decode required, cannot(or not necessary to) do streaming compile.
            if (cacheAvailable || resourceDecoderPath != null) {
                const responce = await FetchResource(dotnetWasmName);
                compiledInstance = await CompileWasmModuleArrayBuffer(responce.buffer, imports);
            }
            else {
                const dotnetWasmResource = fetch(BuildFrameworkPath(dotnetWasmName));
                compiledInstance = await CompileWasmModule(dotnetWasmResource, imports);
            }
        } catch (ex) {
            console.error(ex.toString());
            throw ex;
        }
        successCallback(compiledInstance);
    })();
    return []; // No exports
};

/**
 * See MonoPlatform.ts line:588
 * @param {Promise<Response>} wasmPromise
 * @param {WebAssembly.Imports} imports
 * @returns {Promise<WebAssembly.Instance>}
 */
async function CompileWasmModule(wasmPromise, imports) {
    // This is the same logic as used in emscripten's generated js. We can't use emscripten's js because
    // it doesn't provide any method for supplying a custom response provider, and we want to integrate
    // with our resource loader cache.

    if (typeof WebAssembly['instantiateStreaming'] === 'function') {
        try {
            const streamingResult = await WebAssembly['instantiateStreaming'](wasmPromise, imports);
            return streamingResult.instance;
        }
        catch (ex) {
            console.info('Streaming compilation failed. Falling back to ArrayBuffer instantiation. ', ex);
        }
    }

    // If that's not available or fails (e.g., due to incorrect content-type header),
    // fall back to ArrayBuffer instantiation
    const arrayBuffer = await wasmPromise.then(r => r.arrayBuffer());
    return await CompileWasmModuleArrayBuffer(arrayBuffer, imports);
}

/**
 * See MonoPlatform.ts line:588
 * @param {ArrayBuffer} arrayBuffer
 * @param {WebAssembly.Imports} imports
 * @returns {Promise<WebAssembly.Instance>}
 */
async function CompileWasmModuleArrayBuffer(arrayBuffer, imports) {
    return (await WebAssembly.instantiate(arrayBuffer, imports)).instance;
}

/**
 * Load assembly here.
 * @private
 * @returns {Promise<void>}
 * */
async function PreRun() {
    const mono_wasm_add_assembly = Module.cwrap('mono_wasm_add_assembly', null, ['string', 'number', 'number',]);
    MONO.loaded_files = [];

    dotnetAssemblies.forEach(async (fileName) => {
        const runDependencyId = `blazor:${fileName}`;
        addRunDependency(runDependencyId); //necessary for await

        const data = await FetchResource(fileName);
        if (data == null) {
            removeRunDependency(runDependencyId);
            console.error("failed to fetch:" + fileName);
        } else {
            const heapAddress = Module._malloc(data.length);
            const heapMemory = new Uint8Array(Module.HEAPU8.buffer, heapAddress, data.length);
            heapMemory.set(data);
            mono_wasm_add_assembly(fileName, heapAddress, data.length);
            MONO.loaded_files.push(fileName);
            removeRunDependency(runDependencyId);
        }
    });

    await LoadTimezone(timeZoneFileName);

    const icuFileName = Module.mono_wasm_get_icudt_name(dotnetCulture);
    console.log(icuFileName);
    addRunDependency(`blazor:icudata`);
    const icuData = await FetchResource(icuFileName);
    if (icuData == null) {
        removeRunDependency(`blazor:icudata`);
        useInvariantCulture = true;
        MONO.mono_wasm_setenv("DOTNET_SYSTEM_GLOBALIZATION_INVARIANT", "1");
        console.warn("Failed to fetch icu data. Fall back to use invariant culture.");
    } else {
        const heapAddress = Module._malloc(icuData.length);
        const heapMemory = new Uint8Array(Module.HEAPU8.buffer, heapAddress, icuData.length);
        heapMemory.set(icuData);
        _mono_wasm_load_icu_data(heapAddress);
        MONO.loaded_files.push(icuFileName);
        removeRunDependency(`blazor:icudata`);
    }
}

let useInvariantCulture = false;

/**
 * Finalize boot process here.
 * @private
 * @returns {void}
 * */
function PostRun() {
    MONO.mono_wasm_setenv("MONO_URI_DOTNETRELATIVEORABSOLUTE", "true");
    if (!useInvariantCulture) {
        MONO.mono_wasm_setenv('LANG', `${dotnetCulture}.UTF-8`);
    }
    MONO.mono_wasm_setenv("TZ", timeZoneString);
    MONO.mono_wasm_setenv("DOTNET_SYSTEM_GLOBALIZATION_PREDEFINED_CULTURES_ONLY", "1");
    _mono_wasm_load_runtime(appBinDirName, 0);
    MONO.mono_wasm_runtime_is_ready = true;
    InitializeMessagingService();
    postMessage("_init");
}

let receiver;
/**
 * Initialize message dispatch service here. You can call .NET method here.
 * @private
 * @returns {void}
 * */
function InitializeMessagingService() {
    receiver = Module.mono_bind_static_method("[SampleWorkerAssembly]SampleWorkerAssembly.Hoge:HogeFuga");
}

/**
 * Handles message from parent. 
 * @private
 * @param {MessageEvent} message message from parent
 * @returns {void}
 */
function OnMessageReceived(message) {
    const type = message.data.t;
    const data = message.data.d;

    switch (type) {
        case "SCall":
            const name = new Uint8Array(data[0]);
            const arg = new Uint8Array(data[1]);

            const buffer1 = Module._malloc(name.length);
            const array1 = new Uint8Array(Module.HEAPU8.buffer, buffer1, name.length);
            array1.set(name);

            const buffer2 = Module._malloc(arg.length);
            const array2 = new Uint8Array(Module.HEAPU8.buffer, buffer2, arg.length);
            array2.set(arg);

            receiver(buffer1, name.length, buffer2, arg.length);

            return;
    }
}

// #region typedef
// must sync following typedef to dotnet class

/**
 * @typedef WorkerInitializeSetting
 * @property {string} JSExecutePath
 * @property {string} BasePath
 * @property {string} FrameworkDirName
 * @property {string} AppBinDirName
 * @property {string} DotnetJsName
 * @property {string} DotnetWasmName
 * @property {string} ResourceDecoderPath
 * @property {string} ResourceDecodeMathodName
 * @property {string} ResourcePrefix
 * @property {boolean} UseResourceCache
 * @property {string[]} Assemblies
 * */

/**
 * @typedef ModuleType
 * @property {function(string):void} print
 * @property {function(string):void} printErr
 * @property {function(string):string} locateFile
 * @property {function(WebAssembly.Imports,function(WebAssembly.Instance):void):void} instantiateWasm
 * @property {Array<function():Promise<void> | void>} preRun
 * @property {Array<function():Promise<void> | void>} postRun
 * @property {Array<function():Promise<void> | void>} preloadPlugins
 * */

/**
 * @typedef LoadingResource
 * @property {string} name
 * @property {string} url
 * @property {Promise<Response>} response

// #endregion

// #region utility

/**
 * Builds path to fetch.
 * @private
 * @param {string} name fileName which you want to fetch.
 * @returns {string} relative path to file.
 */
function BuildFrameworkPath(name) {
    return jsExecutePath + "/" + frameworkDirName + "/" + name;
}

/**
 * Builds path to fetch.
 * @private
 * @param {string} name fileName which you want to fetch.
 * @returns {string} relative path to file.
 */
function BuildPath(name) {
    return jsExecutePath + "/" + name;
}

/**
 * Load timezone data.
 * @param {string} name File Name 
 * @returns {Promise<void>}
 */
async function LoadTimezone(name){
    const runDependencyId = `blazor:timezonedata`;
    addRunDependency(runDependencyId);

    const data = await FetchResource(name);

    Module['FS_createPath']('/', 'usr', true, true);
    Module['FS_createPath']('/usr/', 'share', true, true);
    Module['FS_createPath']('/usr/share/', 'zoneinfo', true, true);
    MONO.mono_wasm_load_data_archive(data, '/usr/share/zoneinfo/');

    removeRunDependency(runDependencyId);
}

/** @type Cache */
let resourceCache;

/** @type readonly Request[] */
let resourceCacheKeys;

const cachePrefix = "blazor-resources-";

/**
 * Fetch resource by configured way.
 * @param {string} fileName
 * @returns {Promise<Uint8Array | Int8Array>}
 */
async function FetchResource(fileName) {
    if (!cacheInitializeTryed) {
        await InitializeCache();
    }
    if (cacheAvailable) {
        const cacheResponce = await SearchCache(fileName);
        if (cacheResponce != null) {
            return new Uint8Array(await cacheResponce.arrayBuffer());
        }
    }

    /** @type Response */
    let responce;

    /** @type Uint8Array | Int8Array */
    let data;
    if (resourceDecoderPath != null) {
        responce = await fetch(BuildFrameworkPath(fileName) + resourceSuffix);
    } else {
        responce = await fetch(BuildFrameworkPath(fileName));
    }
    if (responce.ok) {
        const arrayBuffer = await responce.arrayBuffer();

        if (resourceDecoderPath != null) {
            return decoderModule[resourceDecodeMathodName](new Int8Array(arrayBuffer));
        } else {
            return new Uint8Array(arrayBuffer);
        }
    } else {
        if (resourceDecoderPath != null) {
            console.warn("failed to fetch encoded resource. Fall back to fetch not encoded.");
            responce = await fetch(BuildFrameworkPath(fileName));
            if (responce.ok) {
                return new Uint8Array(await responce.arrayBuffer());
            }
        }
    }
    return null;
}

let cacheAvailable = false;
let cacheInitializeTryed = false;

/**
 * Initialize cache system
 * @returns {Promise<void>}
 * */
async function InitializeCache() {
    cacheInitializeTryed = true;
    const targetCacheKey = cachePrefix + basePath;

    if (useCache) {
        if (resourceCache == null) {
            const keys = await caches.keys();
            for (let i = 0; i < keys.length; i++) {
                if (keys[i].startsWith(targetCacheKey)) {
                    resourceCache = await caches.open(keys[i]);
                    break;
                }
            }
            if (resourceCache == null) {
                return;
            }
        }
        if (resourceCacheKeys == null) {
            resourceCacheKeys = await resourceCache.keys();
            if (resourceCacheKeys == null || resourceCacheKeys.length == 0) {
                cacheAvailable = false;
                return;
            } else {
                cacheAvailable = true;
                return;
            }
        }
    } else {
        cacheAvailable = false;
    }
}

/**
 * Search resource from resource cache. If cache is not hit, returns null.
 * @param {string} fileName filename to serach.
 * @returns {Promise<Response>}
 */
async function SearchCache(fileName) {
    let key;
    for (let i = 0; i < resourceCacheKeys.length; i++) {
        if (resourceCacheKeys[i].url.includes(fileName)) {
            key = resourceCacheKeys[i];
        }
    }
    //TODO: should I check the integrity of cache?
    return await resourceCache.match(key);
}

const dotnetArrayOffset = 16; // offset of dotnet array from reference to binary data in bytes.
const nativeLen = 512; // threathold of using native text decoder(for short string, using js-implemented decoder is faster.)
const nativeDecoder = new TextDecoder();
/**
 * Parse Json encorded as UTF-8 Text
 * @param {number} ptr pointer to utf-8 string which is json serialized init options.
 * @param {number} len length of json data in bytes.
 * @returns {any}
 */
function DecodeUTF8JSON(ptr, len) {
    const array = new Uint8Array(wasmMemory.buffer, ptr + dotnetArrayOffset, len);
    const str = len > nativeLen ? nativeDecoder.decode(array) : jsTextDecoderModule.Decode(array);
    return JSON.parse(str);
}