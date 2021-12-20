// @ts-check
import JSTextDecoder from "./TextDecoder.js";

const defaultGeneralBufferLength = 256;

const dataBufferDefaultLength = 1024;

export class Interop {

    /**
     * 
     * @param {boolean} isFromParent
     * @param {number} generalBufferLength
     * @param {string} receiverName
     * @param {string} getReceiverMethodName
     */
    constructor(isFromParent, generalBufferLength, receiverName, getReceiverMethodName) {
        if (isFromParent) {
            if (generalBufferLength == undefined) {
                generalBufferLength = defaultGeneralBufferLength;
            }
            this.generalBufferLength = generalBufferLength;
            this.generalBufferAddr = globalThis.Module._malloc(generalBufferLength);

            this.dotnetReceiverId = 2; // Enum:WorkerContext
            this.dotnetReceiver = globalThis.Module.mono_bind_static_method(receiverName);
        } else {
            if (generalBufferLength == undefined) {
                generalBufferLength = defaultGeneralBufferLength;
            }
            this.generalBufferLength = generalBufferLength;
            this.generalBufferAddr = globalThis.Module._malloc(generalBufferLength);

            this.dotnetReceiverId = globalThis.Module.mono_call_static_method(getReceiverMethodName, [this.generalBufferAddr, generalBufferLength]);
            this.dotnetReceiver = globalThis.Module.mono_bind_static_method(receiverName);
        }
    }

    /** @type number 
     *  @private
     */
    dotnetReceiverId;

    /** @type function(number,string,number) : void 
     *  @private
     * 
     */
    dotnetReceiver;

    /** @type number 
     *  @private
     */
    generalBufferAddr;

    /** @type number 
     *  @private
     */
    generalBufferLength;

    /** @type number 
     *  @private
     */
    dataBufferAddr;

    /** @type number 
     *  @private
     */
    dataBufferLength;

    /**
     * 
     * @param {function(any,Transferable[]):void} func poseMesssage function
     * @param {number} len length of buffer writen data.
     * @param {number} callId call id.
     */
    SCall(func, len, callId) {
        if (len < 16) {
            throw new Error("Buffer too short.");
        }
        const buffer = new Int32Array(globalThis.wasmMemory.buffer, this.generalBufferAddr, len);
        const methodName = globalThis.wasmMemory.buffer.slice(buffer[0], buffer[0] + buffer[1]);
        const jsonBin = globalThis.wasmMemory.buffer.slice(buffer[2], buffer[2] + buffer[3]);
        func({ t: "SCall", i: callId, d: [methodName, jsonBin] }, [methodName, jsonBin]);
    }

    /**
    * Handles message from parent. 
    * @param {MessageEvent} message message
    * @param {number} sourceId id of message source
    * @returns {void}
    */
    HandleMessage(message, sourceId) {
        /** @type string */
        const type = message.data.t;
        /** @type number */
        const messageId = message.data.i;
        /** @type ArrayBuffer[] */
        const data = message.data.d;

        switch (type) {
            case "Init":
                this.dotnetReceiver(this.dotnetReceiverId, "Init", sourceId);
                return;

            case "SCall":
                const name = new Uint8Array(data[0]);
                const jsonArg = new Uint8Array(data[1]);
                const totalLength = name.length + jsonArg.length;

                const bufferArray_s = new Int32Array(globalThis.wasmMemory.buffer, this.generalBufferAddr, this.generalBufferLength / 4);

                bufferArray_s[0] = 0;
                this._EnsureDataBufferLength(totalLength);
                const dataArray_s = new Uint8Array(globalThis.wasmMemory.buffer, this.dataBufferAddr, this.dataBufferLength);

                dataArray_s.set(name, 0);
                bufferArray_s[1] = this.dataBufferAddr;
                bufferArray_s[2] = name.length;

                dataArray_s.set(jsonArg, name.length);
                bufferArray_s[3] = this.dataBufferAddr + name.length;
                bufferArray_s[4] = jsonArg.length;
                bufferArray_s[0] = 20;

                this.dotnetReceiver(this.dotnetReceiverId, "SCall", messageId);
                return;

            case "Res":
                const array = new Int32Array(data[0], 0, 1);
                const len = array[0];
                this._EnsureDataBufferLength(len);

                const bufferArray_r = new Int32Array(globalThis.wasmMemory.buffer, this.generalBufferAddr, this.generalBufferLength / 4);
                bufferArray_r[0] = 0;
                bufferArray_r[1] = this.dataBufferAddr;
                bufferArray_r[2] = len;
                bufferArray_r[0] = 12;

                const dataArray_r = new Uint8Array(wasmMemory.buffer, this.dataBufferAddr, this.dataBufferLength);
                dataArray_r.set(new Int8Array(data[0], 0), 0);
                this.dotnetReceiver(this.dotnetReceiverId, "Res", sourceId);
                return;
        }
    }

    /**
    * Return not void result or exception.
    * */
    ReturnResult() {
        const bufferArray = new Int32Array(wasmMemory.buffer, this.generalBufferAddr, this.generalBufferLength / 4);

        if (bufferArray[0] < 20) {
            throw new Error("Buffer too short.");
        }
        const resultPtr = bufferArray[3];
        const resultLen = bufferArray[4];
        const resultArray = new Uint8Array(wasmMemory.buffer, resultPtr, resultLen);

        const payload = new Int32Array(1);
        payload[0] = resultLen + 12;

        const data = new Uint8Array(12 + resultLen);
        data.set(new Uint8Array(payload.buffer, 0, 4), 0);
        data.set(new Uint8Array(wasmMemory.buffer, this.generalBufferAddr + 4, 8), 4);
        data.set(resultArray, 12);
        postMessage({ t: "Res", d: [data.buffer] }, null, [data.buffer]);
    }

    /**
     * Return void result.
     * */
    ReturnVoidResult() {
        const bufferArray = new Int32Array(wasmMemory.buffer, this.generalBufferAddr, this.generalBufferLength / 4);
        if (bufferArray[0] < 12) {
            throw new Error("Buffer too short.");
        }
        const arrayBuf = wasmMemory.buffer.slice(this.generalBufferAddr, this.generalBufferAddr + 12);
        postMessage({ t: "Res", d: [arrayBuf] }, null, [arrayBuf]);
    }

    /**
    * Ensure that argument buffer length is longer than or equals specify length.
    * @private
    * @param {number} requireLength
    */
    _EnsureDataBufferLength(requireLength) {
        if (this.dataBufferAddr == undefined) {
            this.dataBufferAddr = globalThis.Module._malloc(dataBufferDefaultLength);
            this.dataBufferLength = dataBufferDefaultLength;
        }

        if (this.dataBufferLength >= requireLength) {
            return;
        }

        globalThis.Module._free(this.dataBufferAddr);
        while (this.dataBufferLength < requireLength) {
            this.dataBufferLength *= 2;
        }
        this.dataBufferAddr = globalThis.Module._malloc(this.dataBufferLength);
    }
}

const nativeLen = 512; // threathold of using native text decoder(for short string, using js-implemented decoder is faster.)
const nativeDecoder = new TextDecoder();

/**
* Decode UTF-8 string.
* @param {number} ptr pointer to utf-8 string;
* @param {number} len length of string in bytes.
* @returns {string}
*/
export function DecodeUTF8String(ptr, len) {
    const array = new Uint8Array(globalThis.wasmMemory.buffer, ptr, len);
    return len > nativeLen ? nativeDecoder.decode(array) : JSTextDecoder(array);
}

/**
 * Parse Json encorded as UTF-8 Text
 * @param {number} ptr pointer to utf-8 string which is json string.
 * @param {number} len length of json data in bytes.
 * @returns {any}
 */
export function DecodeUTF8AsJSON(ptr, len) {
    const array = new Uint8Array(globalThis.wasmMemory.buffer, ptr, len);
    const str = len > nativeLen ? nativeDecoder.decode(array) : JSTextDecoder(array);
    return JSON.parse(str);
}

/**
 * Parse Json encorded as UTF-8 Text
 * @param {Uint8Array} array
 * @returns {any}
 */
export function DecodeUTFArray8AsJSON(array) {
    const str = array.length > nativeLen ? nativeDecoder.decode(array) : JSTextDecoder(array);
    return JSON.parse(str);
}