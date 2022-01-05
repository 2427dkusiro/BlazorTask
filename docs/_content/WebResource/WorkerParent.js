// @ts-check
import { Interop } from "./DotnetInterop.js";
import { JSTextDecoder } from "./DotnetInterop.js";

/**
 * @typedef EnvironmentSettings
 * @property {string} WorkerScriptPath
 * @property {string} MessageReceiverFullName
 * @property {string} BasePath
 * */
/*
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
 * @type {JSTextDecoder}
 * */
let textDecoder;

/**
 * @private
 * @type {Interop}
 * */
let interop;

/**
 * Configure this script.
 * @param {number} jsonPtr
 * @param {number} jsonLen
 * @param {number} bufferLen
 * @returns {number}
 */
export function Configure(jsonPtr, jsonLen, bufferLen) {
    textDecoder = new JSTextDecoder();

    /** @type EnvironmentSettings */
    const settings = textDecoder.DecodeUTF8AsJSON(jsonPtr, jsonLen);
    const _workerScriptUrl = settings.WorkerScriptPath;
    if (workerScriptUrl != undefined && workerScriptUrl != _workerScriptUrl) {
        throw new Error("Different worker script url was passed.");
    }
    workerScriptUrl = _workerScriptUrl;
    const dotnetMessageRecieverFullName = settings.MessageReceiverFullName;
    if (interop != undefined) {
        console.error("Interop overwrite.");
    }
    interop = new Interop(true, bufferLen, dotnetMessageRecieverFullName, null, settings.BasePath);
    return interop.generalBufferAddr;
}

/** @type string */
let workerScriptUrl;

/**
 * Create a new worker then init worker.
 * @param {number} ptr pointer to utf-8 string which is json serialized init options.
 * @param {number} len length of json data in bytes.
 * @param {number} id worker id.
 */
export function CreateWorker(ptr, len, id) {
    const worker = new Worker(workerScriptUrl);
    worker.onmessage = (message) => interop.HandleMessage(message, id);

    const arrayBuffer = wasmMemory.buffer.slice(ptr, ptr + len);
    worker.postMessage([arrayBuffer], [arrayBuffer]);
    InsertAt(worker, id);
}

/**
 * Insert worker to specified index.
 * @private
 * @param {Worker} elem
 * @param {number} index
 */
function InsertAt(elem, index) {
    if (workers.length <= index) {
        const diff = index - workers.length + 1;
        for (let i = 0; i < diff; i++) {
            workers.push(undefined);
        }
    }
    if (workers[index] != undefined) {
        throw new Error("Worker already exists.");
    }
    workers[index] = elem;
}

export function TerminateWorker(id) {
    workers[id].terminate();
    workers[id] = undefined;
}

export function SCall(workerId) {
    interop.StaticCall((msg, trans) => workers[workerId].postMessage(msg, trans));
}

/**
 * Return not void result or exception.
 * @param {number} source 
 * */
export function ReturnResult(source) {
    interop.ReturnResult((msg, trans) => workers[source].postMessage(msg, trans));
}

/**
 * Return void result.
 * @param {number} source
 * */
export function ReturnVoidResult(source) {
    interop.ReturnVoidResult((msg, trans) => workers[source].postMessage(msg, trans));
}

export function AssignSyncCallSourceId() {
    interop.AssignSyncCallSourceId();
}

export function ReturnResultSync() {
    interop.ReturnResult((msg, trans) => navigator.serviceWorker.controller.postMessage(msg, trans));
}

export function ReturnVoidResultSync() {
    interop.ReturnVoidResult((msg, trans) => navigator.serviceWorker.controller.postMessage(msg, trans));
}