// @ts-check
import { Interop } from "./DotnetInterop.js";
import { DecodeUTF8AsJSON, DecodeUTF8String } from "./DotnetInterop.js";

/**
 * @typedef EnvironmentSettings
 * @property {string} WorkerScriptPath
 * @property {string} MessageReceiverFullName
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
    /** @type EnvironmentSettings */
    const settings = DecodeUTF8AsJSON(jsonPtr, jsonLen);
    const _workerScriptUrl = settings.WorkerScriptPath;
    if (workerScriptUrl != undefined && workerScriptUrl != _workerScriptUrl) {
        throw new Error("Different worker script url was passed.");
    }
    workerScriptUrl = _workerScriptUrl;
    const dotnetMessageRecieverFullName = settings.MessageReceiverFullName;
    if (interop != undefined) {
        console.error("Interop overwrite.");
    }
    interop = new Interop(true, bufferLen, dotnetMessageRecieverFullName, null);
    return interop.generalBufferAddr;
}

/** @type string */
let workerScriptUrl;

/**
 * Create a new worker then init worker.
 * @param {number} ptr pointer to utf-8 string which is json serialized init options.
 * @param {number} len length of json data in bytes.
 * @returns {number} unique worker id.
 */
export function CreateWorker(ptr, len) {
    const index = workers.length;
    const worker = new Worker(workerScriptUrl);
    worker.onmessage = (message) => interop.HandleMessage(message, index);

    const arrayBuffer = wasmMemory.buffer.slice(ptr, ptr + len);
    worker.postMessage([arrayBuffer], [arrayBuffer]);
    workers.push(worker);
    return index;
}

export function TerminateWorker(id) {
    workers[id].terminate();
    workers[id] = undefined;
}

export function SCall(workerId, len, callId) {
    interop.SCall((msg, trans) => workers[workerId].postMessage(msg, trans), len, callId);
}