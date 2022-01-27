// @ts-check
class Interop {

    defaultGeneralBufferLength = 256;

    dataBufferDefaultLength = 1024;

    /**
     * 
     * @param {boolean} isFromParent
     * @param {number} generalBufferLength
     * @param {string} receiverName
     * @param {string} getReceiverMethodName
     * @param {string} baseAddress
     */
    constructor(isFromParent, generalBufferLength, receiverName, getReceiverMethodName, baseAddress) {
        if (isFromParent) {
            if (generalBufferLength == undefined) {
                generalBufferLength = this.defaultGeneralBufferLength;
            }
            this.generalBufferLength = generalBufferLength;
            this.generalBufferAddr = globalThis.Module._malloc(generalBufferLength);

            this.dotnetReceiverId = 2; // Enum:WorkerContext
            this.dotnetReceiver = globalThis.Module.mono_bind_static_method(receiverName);
        } else {
            if (generalBufferLength == undefined) {
                generalBufferLength = this.defaultGeneralBufferLength;
            }
            this.generalBufferLength = generalBufferLength;
            this.generalBufferAddr = globalThis.Module._malloc(generalBufferLength);

            this.dotnetReceiverId = globalThis.Module.mono_call_static_method(getReceiverMethodName, [this.generalBufferAddr, generalBufferLength]);
            this.dotnetReceiver = globalThis.Module.mono_bind_static_method(receiverName);
        }
        this.baseUrl = baseAddress;
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
     */
    generalBufferAddr;

    /** @type number 
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

    /** @type string
     *  @private
     */
    baseUrl;

    /**
     * 
     * @param {function(any,Transferable[]):void} func poseMesssage function
     */
    StaticCall(func) {
        const buffer = new Int32Array(globalThis.wasmMemory.buffer, this.generalBufferAddr, 7);
        if (buffer[0] < 28) {
            throw new Error();
        }
        const headerPtr = buffer[1];
        const headerLen = buffer[2];
        const methodNamePtr = buffer[3];
        const methodNameLen = buffer[4];

        const array = new Uint8Array(headerLen + methodNameLen);
        const header = new Uint8Array(globalThis.wasmMemory.buffer, headerPtr, headerLen);
        const method = new Uint8Array(globalThis.wasmMemory.buffer, methodNamePtr, methodNameLen);
        array.set(header, 0);
        array.set(method, headerLen);
        const jsonBin = globalThis.wasmMemory.buffer.slice(buffer[5], buffer[5] + buffer[6]);
        func({ t: "SCall", d: [array.buffer, jsonBin] }, [array.buffer, jsonBin]);
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

                this.dotnetReceiver(this.dotnetReceiverId, "SCall", sourceId);
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

                const dataArray_r = new Uint8Array(globalThis.wasmMemory.buffer, this.dataBufferAddr, this.dataBufferLength);
                dataArray_r.set(new Uint8Array(data[0], 0), 0);
                this.dotnetReceiver(this.dotnetReceiverId, "Res", sourceId);
                return;
        }
    }

    /**
    * Return not void result or exception.
    * @param {function(any,Transferable[]):void} func poseMesssage function
    * */
    ReturnResult(func) {
        const bufferArray = new Int32Array(globalThis.wasmMemory.buffer, this.generalBufferAddr, this.generalBufferLength / 4);

        if (bufferArray[0] < 20) {
            throw new Error("Buffer too short.");
        }
        const resultPtr = bufferArray[3];
        const resultLen = bufferArray[4];
        const resultArray = new Uint8Array(globalThis.wasmMemory.buffer, resultPtr, resultLen);

        const payload = new Int32Array(1);
        payload[0] = resultLen + 12;

        const data = new Uint8Array(12 + resultLen);
        data.set(new Uint8Array(payload.buffer, 0, 4), 0);
        data.set(new Uint8Array(globalThis.wasmMemory.buffer, this.generalBufferAddr + 4, 8), 4);
        data.set(resultArray, 12);
        func({ t: "Res", d: [data.buffer] }, [data.buffer]);
    }

    /**
     * Return void result.
     * @param {function(any,Transferable[]):void} func poseMesssage function
     * */
    ReturnVoidResult(func) {
        const bufferArray = new Int32Array(globalThis.wasmMemory.buffer, this.generalBufferAddr, this.generalBufferLength / 4);
        if (bufferArray[0] < 12) {
            throw new Error("Buffer too short.");
        }
        const arrayBuf = globalThis.wasmMemory.buffer.slice(this.generalBufferAddr, this.generalBufferAddr + 12);
        func({ t: "Res", d: [arrayBuf] }, [arrayBuf]);
    }

    AssignSyncCallSourceId() {
        const requestUrl = "_content/WebResource/Dummy.txt";
        const xhr = new XMLHttpRequest();
        const url = new URL(requestUrl, this.baseUrl);
        url.searchParams.set("action", "GetId");
        xhr.open("GET", url.toString(), false);
        xhr.send(null);

        const response = xhr.responseText;
        const bufferArray_r = new Int32Array(globalThis.wasmMemory.buffer, this.generalBufferAddr, this.generalBufferLength / 4);
        bufferArray_r[0] = 0;

        if (response === "6MENWdyDt0p4Qnp9IGYL4OSYj2/Ns9k6uv8yONpN2ph2zNKm+ILRdnvkvl9H7dqFQB+K7aXXDTXo057dUH5vKg") {
            bufferArray_r[1] = -1;
        } else {
            bufferArray_r[1] = parseInt(response);
        }
        bufferArray_r[0] = 8;
    }

    /**
     * Wait sync call and set result to buffer.
     * @param {number} id
     */
    GetCallSyncResult(id) {
        const requestUrl = "_content/WebResource/Dummy.txt";
        const xhr = new XMLHttpRequest();
        xhr.responseType = "arraybuffer";
        const url = new URL(requestUrl, this.baseUrl);
        url.searchParams.set("action", "GetResult");
        url.searchParams.set("id", id.toString());
        xhr.open("GET", url.toString(), false);
        xhr.send(null);

        /** @type ArrayBuffer */
        const response = xhr.response;

        const array = new Int32Array(response, 0, 1);
        const len = array[0];
        this._EnsureDataBufferLength(len);

        const bufferArray_r = new Int32Array(globalThis.wasmMemory.buffer, this.generalBufferAddr, this.generalBufferLength / 4);
        bufferArray_r[0] = 0;
        bufferArray_r[1] = this.dataBufferAddr;
        bufferArray_r[2] = len;
        bufferArray_r[0] = 12;

        const dataArray_r = new Uint8Array(globalThis.wasmMemory.buffer, this.dataBufferAddr, this.dataBufferLength);
        dataArray_r.set(new Uint8Array(response, 0), 0);
    }

    /**
    * Ensure that argument buffer length is longer than or equals specify length.
    * @private
    * @param {number} requireLength
    */
    _EnsureDataBufferLength(requireLength) {
        if (this.dataBufferAddr == undefined) {
            this.dataBufferAddr = globalThis.Module._malloc(this.dataBufferDefaultLength);
            this.dataBufferLength = this.dataBufferDefaultLength;
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

class JSTextDecoder {

    /** @type number */
    nativeLen;

    /** @type TextDecoder */
    nativeDecoder;

    constructor() {
        this.nativeLen = 512; // threathold of using native text decoder(for short string, using js-implemented decoder is faster.)
        this.nativeDecoder = new TextDecoder();

        this.fromCharCode = String.fromCharCode;
        this.Object_prototype_toString = ({}).toString;
        this.sharedArrayBufferString = this.Object_prototype_toString.call(self["SharedArrayBuffer"]);
        this.undefinedObjectString = this.Object_prototype_toString();
        this.NativeUint8Array = self.Uint8Array;
        this.patchedU8Array = this.NativeUint8Array || Array;
        this.nativeArrayBuffer = this.NativeUint8Array ? ArrayBuffer : this.patchedU8Array;
        this.arrayBuffer_isView = this.nativeArrayBuffer.isView || function (x) { return x && "length" in x };
        this.arrayBufferString = this.Object_prototype_toString.call(this.nativeArrayBuffer.prototype);
        this.tmpBufferU16 = new (this.NativeUint8Array ? Uint16Array : this.patchedU8Array)(32);
    }

    /**
    * Decode UTF-8 string.
    * @param {number} ptr pointer to utf-8 string;
    * @param {number} len length of string in bytes.
    * @returns {string}
    */
    DecodeUTF8String(ptr, len) {
        const array = new Uint8Array(globalThis.wasmMemory.buffer, ptr, len);
        return len > this.nativeLen ? this.nativeDecoder.decode(array) : this.JSTextDecode(array);
    }

    /**
     * Parse Json encorded as UTF-8 Text
     * @param {number} ptr pointer to utf-8 string which is json string.
     * @param {number} len length of json data in bytes.
     * @returns {any}
     */
    DecodeUTF8AsJSON(ptr, len) {
        const array = new Uint8Array(globalThis.wasmMemory.buffer, ptr, len);
        const str = len > this.nativeLen ? this.nativeDecoder.decode(array) : this.JSTextDecode(array);
        return JSON.parse(str);
    }

    /**
     * Parse Json encorded as UTF-8 Text
     * @param {Uint8Array} array
     * @returns {any}
     */
    DecodeUTFArray8AsJSON(array) {
        const str = array.length > this.nativeLen ? this.nativeDecoder.decode(array) : this.JSTextDecode(array);
        return JSON.parse(str);
    }

    // code from anonyco/FastestSmallestTextEncoderDecoder
    // Creative Commons Zero v1.0 Universal

    fromCharCode;
    Object_prototype_toString;
    sharedArrayBufferString;
    undefinedObjectString;
    NativeUint8Array;
    patchedU8Array;
    nativeArrayBuffer;
    arrayBuffer_isView;
    arrayBufferString;
    tmpBufferU16;

    JSTextDecode(inputArrayOrBuffer) {
        var inputAs8 = inputArrayOrBuffer, asObjectString;
        if (!this.arrayBuffer_isView(inputAs8)) {
            asObjectString = this.Object_prototype_toString.call(inputAs8);
            if (asObjectString !== this.arrayBufferString && asObjectString !== this.sharedArrayBufferString && asObjectString !== this.undefinedObjectString)
                throw TypeError("Failed to execute 'decode' on 'TextDecoder': The provided value is not of type '(ArrayBuffer or ArrayBufferView)'");
            inputAs8 = this.NativeUint8Array ? new this.patchedU8Array(inputAs8) : inputAs8 || [];
        }

        var resultingString = "", tmpStr = "", index = 0, len = inputAs8.length | 0, lenMinus32 = len - 32 | 0, nextEnd = 0, nextStop = 0, cp0 = 0, codePoint = 0, minBits = 0, cp1 = 0, pos = 0, tmp = -1;
        // Note that tmp represents the 2nd half of a surrogate pair incase a surrogate gets divided between blocks
        for (; index < len;) {
            nextEnd = index <= lenMinus32 ? 32 : len - index | 0;
            for (; pos < nextEnd; index = index + 1 | 0, pos = pos + 1 | 0) {
                cp0 = inputAs8[index] & 0xff;
                switch (cp0 >> 4) {
                    case 15:
                        cp1 = inputAs8[index = index + 1 | 0] & 0xff;
                        if ((cp1 >> 6) !== 0b10 || 0b11110111 < cp0) {
                            index = index - 1 | 0;
                            break;
                        }
                        codePoint = ((cp0 & 0b111) << 6) | (cp1 & 0b00111111);
                        minBits = 5; // 20 ensures it never passes -> all invalid replacements
                        cp0 = 0x100; //  keep track of th bit size
                    case 14:
                        cp1 = inputAs8[index = index + 1 | 0] & 0xff;
                        codePoint <<= 6;
                        codePoint |= ((cp0 & 0b1111) << 6) | (cp1 & 0b00111111);
                        minBits = (cp1 >> 6) === 0b10 ? minBits + 4 | 0 : 24; // 24 ensures it never passes -> all invalid replacements
                        cp0 = (cp0 + 0x100) & 0x300; // keep track of th bit size
                    case 13:
                    case 12:
                        cp1 = inputAs8[index = index + 1 | 0] & 0xff;
                        codePoint <<= 6;
                        codePoint |= ((cp0 & 0b11111) << 6) | cp1 & 0b00111111;
                        minBits = minBits + 7 | 0;

                        // Now, process the code point
                        if (index < len && (cp1 >> 6) === 0b10 && (codePoint >> minBits) && codePoint < 0x110000) {
                            cp0 = codePoint;
                            codePoint = codePoint - 0x10000 | 0;
                            if (0 <= codePoint/*0xffff < codePoint*/) { // BMP code point
                                //nextEnd = nextEnd - 1|0;

                                tmp = (codePoint >> 10) + 0xD800 | 0;   // highSurrogate
                                cp0 = (codePoint & 0x3ff) + 0xDC00 | 0; // lowSurrogate (will be inserted later in the switch-statement)

                                if (pos < 31) { // notice 31 instead of 32
                                    this.tmpBufferU16[pos] = tmp;
                                    pos = pos + 1 | 0;
                                    tmp = -1;
                                } else {// else, we are at the end of the inputAs8 and let tmp0 be filled in later on
                                    // NOTE that cp1 is being used as a temporary variable for the swapping of tmp with cp0
                                    cp1 = tmp;
                                    tmp = cp0;
                                    cp0 = cp1;
                                }
                            } else nextEnd = nextEnd + 1 | 0; // because we are advancing i without advancing pos
                        } else {
                            // invalid code point means replacing the whole thing with null replacement characters
                            cp0 >>= 8;
                            index = index - cp0 - 1 | 0; // reset index  back to what it was before
                            cp0 = 0xfffd;
                        }


                        // Finally, reset the variables for the next go-around
                        minBits = 0;
                        codePoint = 0;
                        nextEnd = index <= lenMinus32 ? 32 : len - index | 0;
                    /*case 11:
                    case 10:
                    case 9:
                    case 8:
                        codePoint ? codePoint = 0 : cp0 = 0xfffd; // fill with invalid replacement character
                    case 7:
                    case 6:
                    case 5:
                    case 4:
                    case 3:
                    case 2:
                    case 1:
                    case 0:
                        tmpBufferU16[pos] = cp0;
                        continue;*/
                    default:
                        this.tmpBufferU16[pos] = cp0; // fill with invalid replacement character
                        continue;
                    case 11:
                    case 10:
                    case 9:
                    case 8:
                }
                this.tmpBufferU16[pos] = 0xfffd; // fill with invalid replacement character
            }
            tmpStr += this.fromCharCode(
                this.tmpBufferU16[0], this.tmpBufferU16[1], this.tmpBufferU16[2], this.tmpBufferU16[3], this.tmpBufferU16[4], this.tmpBufferU16[5], this.tmpBufferU16[6], this.tmpBufferU16[7],
                this.tmpBufferU16[8], this.tmpBufferU16[9], this.tmpBufferU16[10], this.tmpBufferU16[11], this.tmpBufferU16[12], this.tmpBufferU16[13], this.tmpBufferU16[14], this.tmpBufferU16[15],
                this.tmpBufferU16[16], this.tmpBufferU16[17], this.tmpBufferU16[18], this.tmpBufferU16[19], this.tmpBufferU16[20], this.tmpBufferU16[21], this.tmpBufferU16[22], this.tmpBufferU16[23],
                this.tmpBufferU16[24], this.tmpBufferU16[25], this.tmpBufferU16[26], this.tmpBufferU16[27], this.tmpBufferU16[28], this.tmpBufferU16[29], this.tmpBufferU16[30], this.tmpBufferU16[31]
            );
            if (pos < 32) tmpStr = tmpStr.slice(0, pos - 32 | 0);//-(32-pos));
            if (index < len) {
                //fromCharCode.apply(0, tmpBufferU16 : NativeUint8Array ?  tmpBufferU16.subarray(0,pos) : tmpBufferU16.slice(0,pos));
                this.tmpBufferU16[0] = tmp;
                pos = (~tmp) >>> 31;//tmp !== -1 ? 1 : 0;
                tmp = -1;

                if (tmpStr.length < resultingString.length) continue;
            } else if (tmp !== -1) {
                tmpStr += this.fromCharCode(tmp);
            }

            resultingString += tmpStr;
            tmpStr = "";
        }

        return resultingString;
    }
}