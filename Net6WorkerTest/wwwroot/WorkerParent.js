// @ts-check

/**
 * @typedef EnvironmentSettings
 * @property {string} WorkerScriptUrl
 * @property {string} AssemblyName
 * @property {string} MessageHandlerName
 * */

/**
 * @private
 * @type {Worker[]}
 * */
const workers = [];

/**
 * Configure this script.
 * @param {string} json json encoded settings.
 * @returns {void}
 */
export function Configure(json) {
    /** @type EnvironmentSettings */
    const data = JSON.parse(json);
    workerScriptUrl = data.WorkerScriptUrl;
    dotnetAssemblyName = data.AssemblyName;
    dotnetMessageEventHandler = data.MessageHandlerName;
}

/** @type {string} */
let workerScriptUrl;

/** @type {string} */
let dotnetAssemblyName;

/** @type {string} */
let dotnetMessageEventHandler;

/**
 * Create a new worker then init worker.
 * @param {string} option json serialized init options.
 * @returns {number} unique worker id.
 */
export function CreateWorker(option) {
    const index = workers.length;
    const worker = new Worker(workerScriptUrl);
    worker.onmessage = (message) => { OnMessage(index, message); };
    worker.postMessage(option);
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
    DotNet.invokeMethod(dotnetAssemblyName, dotnetMessageEventHandler, id, event.data);
}

/**
 * Calls static method on worker.
 * @param {Number} id worker id;
 * @param {string} method method(full name) to call.
 * @returns {void}
 */
export function Call(id, method) {
    workers[id].postMessage(method);
}