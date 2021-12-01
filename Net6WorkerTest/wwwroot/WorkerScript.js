// @ts-check

/**
 * @typedef WorkerInitOption
 * @property {string} BasePath
 * @property {string} FrameworkDirName
 * @property {string} AppBinDirName
 * @property {string} DotnetJsName
 * @property {string} DotnetWasmName
 * @property {string[]} Assemblies
 * */

/**
 * @typedef ModuleType
 * @property {function(string):void} print 
 * @property {function(string):void} printErr
 * @property {function(string):string} locateFile
 * @property {Array<function():Promise<void> | void>} preRun
 * @property {Array<function():Promise<void> | void>} postRun
 * @property {Array<function():Promise<void> | void>} preloadPlugins
 * */


let basePath;
let frameworkDirName;
let appBinDirName;
let dotnetJsName;
let dotnetWasmName;

/**
 * @type string[]
 * */
let dotnetAssemblies;

self.onmessage = (/** @type MessageEvent */eventArg) => {
    self.onmessage = OnMessageReceived;
    Initialize(eventArg);
}

/**
 * Invoke initialize logics. This method should call once.
 * @private
 * @param {MessageEvent} eventArg
 * @returns {void}
 */
function Initialize(eventArg) {
    /**@type WorkerInitOption */
    const option = JSON.parse(eventArg.data);
    basePath = option.BasePath;
    frameworkDirName = option.FrameworkDirName;
    appBinDirName = option.AppBinDirName;
    dotnetJsName = option.DotnetJsName;
    dotnetWasmName = option.DotnetWasmName;
    dotnetAssemblies = option.Assemblies;

    /** @type ModuleType */
    const _Module = {};
    _Module.print = WriteStdOut;
    _Module.printErr = WriteStdError;
    _Module.locateFile = LocateFile;
    _Module.preRun = [];
    _Module.postRun = [];
    _Module.preloadPlugins = [];
    _Module.preRun.push(PreRun);
    _Module.postRun.push(PostRun);

    global = globalThis;
    self.Module = _Module;

    self.importScripts(BuildPath(dotnetJsName));
}

/**
 * Builds path to fetch.
 * @private
 * @param {string} name fileName which you want to fetch.
 * @returns {string} relative path to file.
 */
function BuildPath(name) {
    return basePath + "/" + frameworkDirName + "/" + name;
}

/**
 * Implements worker's stdout.
 * @private
 * @param {string} message message to write.
 * @returns {void}
 */
function WriteStdOut(message) {
    console.log("worker-stdout:" + message);
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
        return BuildPath(dotnetWasmName);
    }
    return fileName;
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
        addRunDependency(runDependencyId);
        const result = await fetch(BuildPath(fileName));
        if (result.ok) {
            const arrayBuffer = await result.arrayBuffer();
            const data = new Uint8Array(arrayBuffer);

            const heapAddress = Module._malloc(data.length);
            const heapMemory = new Uint8Array(Module.HEAPU8.buffer, heapAddress, data.length);
            heapMemory.set(data);
            mono_wasm_add_assembly(fileName, heapAddress, data.length);
            MONO.loaded_files.push(fileName);
            removeRunDependency(runDependencyId);
        } else {
            removeRunDependency(runDependencyId);
            console.error("failed to fetch:" + fileName);
        }
    });
}

/**
 * Finalize boot process here.
 * @private
 * @returns {void}
 * */
function PostRun() {
    MONO.mono_wasm_setenv("MONO_URI_DOTNETRELATIVEORABSOLUTE", "true");
    // const load_runtime = Module.cwrap('mono_wasm_load_runtime', null, ['string', 'number']);
    mono_wasm_load_runtime(appBinDirName, 0);
    MONO.mono_wasm_runtime_is_ready = true;
    InitializeMessageingService();
    postMessage("__init");
}

/**
 * Initialize message dispatch service here. You can call .NET method here.
 * @private
 * @returns {void}
 * */
function InitializeMessageingService() {

}

/**
 * Handles message from parent. 
 * @private
 * @param {MessageEvent} message message from parent
 * @returns {void}
 */
function OnMessageReceived(message) {
    if (message.data.endsWith("Fuga")) {
        Module.mono_call_static_method(message.data, []);
    }
    if (message.data.endsWith("Piyo")) {
        Module.mono_call_static_method(message.data, ["argument from js"]);
    }
}


function _postMessage(message) {
    postMessage(message);
}