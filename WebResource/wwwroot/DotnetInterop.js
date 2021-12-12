// @ts-check
import JSTextDecoder from "./TextDecoder.js";

const defaultGeneralBufferLength = 256;

const dataBufferDefaultLength = 1024;

export class Interop {

    /**
     * Create a new instance of Interop class.
     * @param {number} generalBufferLength
     * @param {string} getReceiverMethodName
     * @param {number} receiverId
     * @param {string} receiverName
     */
    constructor(generalBufferLength, getReceiverMethodName, receiverId, receiverName) {
        if (getReceiverMethodName == null) {
            if (generalBufferLength == undefined) {
                generalBufferLength = defaultGeneralBufferLength;
            }
            this.generalBufferLength = generalBufferLength;
            this.generalBufferAddr = globalThis.Module._malloc(generalBufferLength);

            this.dotnetReceiverId = receiverId;
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

    /** @type number */
    dotnetReceiverId;

    /** @type function(number,string,number) : void */
    dotnetReceiver;

    /** @type number */
    generalBufferAddr;

    /** @type number */
    generalBufferLength;

    /** @type number */
    dataBufferAddr;

    /** @type number */
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
        const data = message.data.d;

        switch (type) {
            case "Init":
                this.dotnetReceiver(this.dotnetReceiverId, "Init", sourceId);
                return;

            case "SCall":
                const name = new Uint8Array(data[0]);
                const jsonArg = new Uint8Array(data[1]);
                const totalLength = name.length + jsonArg.length;

                const bufferArray = new Int32Array(globalThis.wasmMemory.buffer, this.generalBufferAddr, this.generalBufferLength / 4);

                bufferArray[0] = 0;
                this._EnsureDataBufferLength(totalLength);
                const dataArray = new Uint8Array(globalThis.wasmMemory.buffer, this.dataBufferAddr, this.dataBufferLength);

                dataArray.set(name, 0);
                bufferArray[1] = this.dataBufferAddr;
                bufferArray[2] = name.length;

                dataArray.set(jsonArg, name.length);
                bufferArray[3] = this.dataBufferAddr + name.length;
                bufferArray[4] = jsonArg.length;
                bufferArray[0] = 20;

                this.dotnetReceiver(this.dotnetReceiverId, "SCall", messageId);
                return;

            case "Res":
                const array = new Int32Array(data);
                console.log(array);
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

        const data = new Uint8Array(8 + resultLen);
        data.set(bufferArray.subarray(1, 2), 0);
        data.set(wasmMemory.buffer.slice(resultPtr, resultPtr + resultLen), 8);

        postMessage({ t: "Res", d: data.buffer }, null, [data.buffer]);
    }

    /**
     * Return void result.
     * */
    ReturnVoidResult() {
        const bufferArray = new Int32Array(wasmMemory.buffer, this.generalBufferAddr, 1);
        if (bufferArray[0] < 12) {
            throw new Error("Buffer too short.");
        }
        const arrayBuf = wasmMemory.buffer.slice(this.generalBufferAddr + 4, this.generalBufferAddr + 12);
        postMessage({ t: "Res", d: arrayBuf }, null, [arrayBuf]);
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