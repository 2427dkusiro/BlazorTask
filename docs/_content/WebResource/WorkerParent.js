// @ts-check
import JSTextDecoder from "./TextDecoder.js";

/**
 * @typedef EnvironmentSettings
 * @property {string} WorkerScriptPath
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
 * @private
 * @type {Int32Array[]}
 * */
const buffers = [];

/**
 * Configure this script.
 * @param {number} jsonPtr
 * @param {number} jsonLen
 * @returns {number}
 */
export function Configure(jsonPtr, jsonLen, bufferLen) {
    /** @type EnvironmentSettings */
    const data = DecodeUTF8JSON(jsonPtr, jsonLen);
    workerScriptUrl = data.WorkerScriptPath
    dotnetAssemblyName = data.AssemblyName;
    dotnetMessageEventHandler = data.MessageHandlerName;
    dotnetInitializedHandler = data.InitializedHandlerName;

    const buffer = Module._malloc(bufferLen);
    return buffer;
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
    buffers.push(undefined);
    return index;
}

/**
 * Allocate buffer to call worker 
 * @param {any} id worker id
 * @param {any} len buffer length in byte
 * @returns {number} buffer address or null pointer(0).
 */
export function AllocBuffer(id, len) {
    if (buffers[id] === undefined) {
        const addr = Module._malloc(len);
        const array = new Int32Array(wasmMemory.buffer, addr, len / 4);
        buffers[id] = array;
        return addr;
    } else {
        return 0;
    }
}

export function SCall(workerId, len, callId) {
    if (len < 16) {
        throw new Error();
    }
    const buffer = buffers[workerId];
    const methodName = wasmMemory.buffer.slice(buffer[0], buffer[0] + buffer[1]);
    const jsonBin = wasmMemory.buffer.slice(buffer[2], buffer[2] + buffer[3]);
    workers[workerId].postMessage({ t: "SCall", d: [methodName, jsonBin] }, [methodName, jsonBin]);
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