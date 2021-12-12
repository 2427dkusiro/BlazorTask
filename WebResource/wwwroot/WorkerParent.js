// @ts-check
import { Interop } from "./DotnetInterop.js";
import { DecodeUTF8AsJSON, DecodeUTF8String } from "./DotnetInterop.js";

/**
 * @typedef EnvironmentSettings
 * @property {string} WorkerScriptPath
 * @property {string} MessageReceiverFullName
 * @property {number} MessageReceiverId
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
    workerScriptUrl = settings.WorkerScriptPath;
    dotnetMessageReceiverId = settings.MessageReceiverId;
    dotnetMessageRecieverFullName = settings.MessageReceiverFullName;
    interop = new Interop(bufferLen, null, dotnetMessageReceiverId, dotnetMessageRecieverFullName);
    return interop.generalBufferAddr;
}

/** @type string */
let workerScriptUrl;

let dotnetMessageReceiverId;

/** @type string */
let dotnetMessageRecieverFullName;

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

export function SCall(workerId, len, callId) {
    interop.SCall((msg, trans) => workers[workerId].postMessage(msg, trans), len, callId);
}