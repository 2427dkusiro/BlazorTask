// @ts-check

const spPath = "_content/WebResource/Dummy.txt";
let id = 1;

/**
 * Return true if passed request is special.
 * @param {Request} request
 * @returns {boolean}
 */
function IsSpecial(request) {
    let url = new URL(request.url);
    if (url.pathname.endsWith(spPath)) {
        return true;
    } else {
        return false;
    }
}

async function GetSpecialResponse(request) {
    let url = new URL(request.url);
    const action = url.searchParams.get("action");
    if (action == "GetResult") {
        const id = url.searchParams.get("id");
        const value = await GetMessage(id, 30000);
        const response = new Response(value, { status: 200 });
        return response;
    }
    if (action == "GetId") {
        const response = new Response((id++).toString(), { status: 200 });
        return response;
    }
}

/** @type Map<number,ArrayBuffer> */
const responceTable = new Map();
const waitUnit = 200;

async function GetMessage(id, timeout) {
    const count = timeout == -1 ? Number.MAX_VALUE : timeout / waitUnit + 1;
    for (let i = 0; i < count; i++) {
        if (responceTable.has(id)) {
            const value = responceTable.get(id);
            responceTable.delete(id);
            return value;
        }
        await Delay(waitUnit);
    }
    return null;
}

/**
 * Handle special message.
 * @param {MessageEvent} message message
 */
function OnMessage(message) {
    /** @type ArrayBuffer */
    const buffer = message.data.d;
    const array = new Int32Array(buffer, 0, 8);
    const id = array[1];
    responceTable.set(id, buffer);
}

/**
 * Return a promise which will be resolved in specified millseconds. (like Task.Delay)
 * @param {number} ms time to wait.
 */
function Delay(ms) {
    return new Promise(resolve => setTimeout(resolve, ms));
}