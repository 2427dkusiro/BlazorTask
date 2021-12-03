// @ts-check
import JSTextDecoder from "./TextDecoder.js";

/**
 * @typedef EnvironmentSettings
 * @property {string} WorkerScriptUrl
 * @property {string} AssemblyName
 * @property {string} MessageHandlerName
 * @property {string} InitializedHandlerName
 * */

/**
 * @private
 * @type {Worker[]}
 * */
const workers = [];

/**
 * Configure this script.
 * @param {number} ptr
 * @param {number} len
 * @returns {void}
 */
export function Configure(ptr, len) {
    /** @type EnvironmentSettings */
    const data = DecodeUTF8JSON(ptr, len);
    workerScriptUrl = data.WorkerScriptUrl;
    dotnetAssemblyName = data.AssemblyName;
    dotnetMessageEventHandler = data.MessageHandlerName;
    dotnetInitializedHandler = data.InitializedHandlerName;
}

/** @type {string} */
let workerScriptUrl;

/** @type {string} */
let dotnetAssemblyName;

/** @type {string} */
let dotnetMessageEventHandler;

/** @type {string} */
let dotnetInitializedHandler;

/**
 * Create a new worker then init worker.
 * @param {number} ptr pointer to utf-8 string which is json serialized init options.
 * @param {number} len length of json data in bytes.
 * @returns {number} unique worker id.
 */
export function CreateWorker(ptr, len) {
    const index = workers.length;
    const worker = new Worker(workerScriptUrl);
    worker.onmessage = (message) => OnMessage(index, message);

    const array = new Uint8Array(wasmMemory.buffer, ptr, len);
    const array2 = new Uint8Array(array);
    worker.postMessage([array2.buffer], [array2.buffer]);
    workers.push(worker);
    return index;
}

/**
 * Handles messge from worker.
 * @private
 * @param {MessageEvent} event
 * @param {Number} id worker id
 * @returns {void}
 */
function OnMessage(id, event) {
    if (event.data.startsWith("_")) {
        switch (event.data) {
            case "_init":
                DotNet.invokeMethod(dotnetAssemblyName, dotnetInitializedHandler, id);
                break;
        }
    }
    DotNet.invokeMethod(dotnetAssemblyName, dotnetMessageEventHandler, id, event.data);
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
    const array = new Uint8Array(wasmMemory.buffer, ptr, len);
    const str = len > nativeLen ? nativeDecoder.decode(array) : JSTextDecoder(array);
    return JSON.parse(str);
}

/**
 * Calls static method on worker.
 * @param {Number} id worker id;
 * @param {string} method method(full name) to call.
 * @returns {void}
 */
export function _Call(id, method) {
    workers[id].postMessage(method);
}