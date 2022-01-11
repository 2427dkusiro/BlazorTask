// @ts-check

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

/** @type string */
let cacheName;

// When undefined, use browser provided locale string.
/** @type string */
let dotnetCulture;

// When undefined, use browser provided timezone string.
/** @type string */
let timeZoneString;

/** @type string */
let timeZoneFileName;

/** @type string */
let messageHandlerMethodFullName

/** @type string */
let createMessageReceiverMethodFullName;

let bufferLength = 256;

/** @type Loader */
let resourceLoader;

self.onmessage = (/** @type MessageEvent */ eventArg) => {
    self.onmessage = OnMessageReceived;
    ConfigureThis(eventArg);
    ImportModules();
    InitializeRuntime();
}

/**
 * Configure this object from passed setting info.
 * @param {MessageEvent} eventArg
 * @returns {void}
 */
function ConfigureThis(eventArg) {
    const array = new Uint8Array(eventArg.data[0], 0);

    /**@type WorkerInitializeSetting */
    const setting = JSON.parse((new TextDecoder()).decode(array));
    basePath = setting.BasePath;
    jsExecutePath = setting.JSExecutePath;
    frameworkDirName = setting.FrameworkDirName;
    appBinDirName = setting.AppBinDirName;
    dotnetJsName = setting.DotnetJsName;
    dotnetWasmName = setting.DotnetWasmName;
    resourceDecoderPath = setting.ResourceDecoderPath;
    resourceDecodeMathodName = setting.ResourceDecodeMathodName;
    resourceSuffix = setting.ResourceSuffix
    useCache = setting.UseResourceCache;
    cacheName = setting.CacheName;
    dotnetCulture = setting.DotnetCulture;
    timeZoneFileName = setting.TimeZoneFileName;
    messageHandlerMethodFullName = setting.MessageHandlerMethodFullName;
    createMessageReceiverMethodFullName = setting.CreateMessageReceiverMethodFullName;
    dotnetAssemblies = setting.Assemblies;

    if (dotnetCulture == undefined) {
        dotnetCulture = Intl.DateTimeFormat().resolvedOptions().locale;
    }
    if (timeZoneString == undefined) {
        timeZoneString = Intl.DateTimeFormat().resolvedOptions().timeZone;
    }
}

/**
 * Import modules here
 * @returns {void} 
 */
function ImportModules() {
    globalThis.importScripts("./WorkerDotnetInterop.js", "./ResourceLoader.js");
    if (resourceDecoderPath != null) {
        globalThis.importScripts(BuildPath(resourceDecoderPath));
    }
    resourceLoader = new Loader(useCache, cacheName, resourceDecoderPath != null, resourceSuffix, resourceDecodeMathodName);
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
            const path = BuildFrameworkPath(dotnetWasmName);
            const promise = resourceLoader.FetchResourceResponce(path);
            compiledInstance = await CompileWasmModule(promise, imports);
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
    const mono_wasm_add_assembly = globalThis.Module.cwrap('mono_wasm_add_assembly', null, ['string', 'number', 'number',]);
    globalThis.MONO.loaded_files = [];

    dotnetAssemblies.forEach(async (fileName) => {
        const runDependencyId = `blazor:${fileName}`;
        globalThis.addRunDependency(runDependencyId); //necessary for await

        const data = await resourceLoader.FetchResourceArray(BuildFrameworkPath(fileName));
        if (data == null) {
            globalThis.removeRunDependency(runDependencyId);
            console.error("failed to fetch:" + fileName);
        } else {
            const heapAddress = globalThis.Module._malloc(data.length);
            const heapMemory = new Uint8Array(globalThis.Module.HEAPU8.buffer, heapAddress, data.length);
            heapMemory.set(data);
            mono_wasm_add_assembly(fileName, heapAddress, data.length);
            globalThis.MONO.loaded_files.push(fileName);
            globalThis.removeRunDependency(runDependencyId);
        }
    });

    await LoadTimezone(timeZoneFileName);
    await LoadICUData(dotnetCulture);
}

let useInvariantCulture = false;

/**
 * Finalize boot process here.
 * @private
 * @returns {void}
 * */
function PostRun() {
    globalThis.MONO.mono_wasm_setenv("MONO_URI_DOTNETRELATIVEORABSOLUTE", "true");
    if (!useInvariantCulture) {
        globalThis.MONO.mono_wasm_setenv('LANG', `${dotnetCulture}.UTF-8`);
    }
    globalThis.MONO.mono_wasm_setenv("TZ", timeZoneString);
    globalThis.MONO.mono_wasm_setenv("DOTNET_SYSTEM_GLOBALIZATION_PREDEFINED_CULTURES_ONLY", "1");
    globalThis._mono_wasm_load_runtime(appBinDirName, 0);
    globalThis.MONO.mono_wasm_runtime_is_ready = true;
    InitializeMessagingService();
    postMessage({ t: "Init" }, null, null);
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
 * @property {string} ResourceSuffix
 * @property {boolean} UseResourceCache
 * @property {string} CacheName
 * @property {string} DotnetCulture
 * @property {string} TimeZoneString
 * @property {string} TimeZoneFileName
 * @property {string} MessageHandlerMethodFullName
 * @property {string} CreateMessageReceiverMethodFullName
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
 * */

// #endregion

// #region utility

/**
 * Builds path to fetch.
 * @private
 * @param {string} fileName file name which you want to fetch.
 * @returns {string} relative path to file.
 */
function BuildFrameworkPath(fileName) {
    return jsExecutePath + "/" + frameworkDirName + "/" + fileName;
}

/**
 * Builds path to fetch.
 * @private
 * @param {string} fileName file name which you want to fetch.
 * @returns {string} relative path to file.
 */
function BuildPath(fileName) {
    return jsExecutePath + "/" + fileName;
}

/**
 * Load ICU data.
 * @param {string} culture Culture name such as 'en-US' 'ja-JP'
 * @returns {Promise<void>}
 */
async function LoadICUData(culture) {
    const icuFileName = globalThis.Module.mono_wasm_get_icudt_name(culture);
    globalThis.addRunDependency(`blazor:icudata`);
    const icuData = await resourceLoader.FetchResourceArray(icuFileName);
    if (icuData == null) {
        globalThis.removeRunDependency(`blazor:icudata`);
        useInvariantCulture = true;
        globalThis.MONO.mono_wasm_setenv("DOTNET_SYSTEM_GLOBALIZATION_INVARIANT", "1");
        console.warn("Failed to fetch icu data. Fall back to use invariant culture.");
    } else {
        const heapAddress = globalThis.Module._malloc(icuData.length);
        const heapMemory = new Uint8Array(globalThis.Module.HEAPU8.buffer, heapAddress, icuData.length);
        heapMemory.set(icuData);
        globalThis._mono_wasm_load_icu_data(heapAddress);
        globalThis.MONO.loaded_files.push(icuFileName);
        globalThis.removeRunDependency(`blazor:icudata`);
    }
}

/**
 * Load timezone data.
 * @param {string} name File Name 
 * @returns {Promise<void>}
 */
// See MonoPlatform.cs line 543
async function LoadTimezone(name) {
    const runDependencyId = `blazor:timezonedata`;
    globalThis.addRunDependency(runDependencyId);

    const data = await resourceLoader.FetchResourceArray(name);

    globalThis.Module['FS_createPath']('/', 'usr', true, true);
    globalThis.Module['FS_createPath']('/usr/', 'share', true, true);
    globalThis.Module['FS_createPath']('/usr/share/', 'zoneinfo', true, true);
    globalThis.MONO.mono_wasm_load_data_archive(data, '/usr/share/zoneinfo/');

    globalThis.removeRunDependency(runDependencyId);
}

// #region Messaging
/** @type Interop */
let interop;

/**
 * Initialize message dispatch service here. You can call .NET method here.
 * @private
 * @returns {void}
 * */
function InitializeMessagingService() {
    interop = new Interop(false, bufferLength, messageHandlerMethodFullName, createMessageReceiverMethodFullName, basePath);
}

/**
 * Handles message from parent. 
 * @private
 * @param {MessageEvent} message message from parent
 * @returns {void}
 */
function OnMessageReceived(message) {
    interop.HandleMessage(message, 0);
    return;
}

/**
 * 
 * @param {number} source
 */
function SCall(source) {
    if (source != 0) {
        console.error("not supported!");
    }
    interop.StaticCall((msg, trans) => globalThis.postMessage(msg, null, trans));
}

/**
 * Return not void result or exception.
 * @param {number} source 
 * */
function ReturnResult(source) {
    if (source != 0) {
        console.error("not supported!");
    }
    interop.ReturnResult((msg, trans) => globalThis.postMessage(msg, null, trans));
}

/**
 * Return void result.
 * @param {number} source
 * */
function ReturnVoidResult(source) {
    if (source != 0) {
        console.error("not supported!");
    }
    interop.ReturnVoidResult((msg, trans) => globalThis.postMessage(msg, null, trans));
}

function AssignSyncCallSourceId() {
    interop.AssignSyncCallSourceId();
}

function WaitSyncCall(id) {
    interop.GetCallSyncResult(id);
}