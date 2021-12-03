// @ts-check

/** @type string */
let basePath;

/** @type string */
let frameworkDirName;

/** @type string */
let appBinDirName;

/** @type string */
let dotnetJsName;

/** @type string */
let dotnetWasmName;

/** @type string[] */
let dotnetAssemblies;

self.onmessage = (/** @type MessageEvent */ eventArg) => {
    self.onmessage = OnMessageReceived;
    Initialize(eventArg);
}

/**
 * Invoke initialize logics. This method should call once.
 * @private
 * @param {MessageEvent} eventArg
 * @returns {void}
 */
function Initialize(eventArg) {
    const array = new Uint8Array(eventArg.data[0], 0);
    const str = array.length > nativeLen ? nativeDecoder.decode(array) : JSDecoder.Decode(array);

    /**@type WorkerInitOption */
    const option = JSON.parse(str);
    basePath = option.BasePath;
    frameworkDirName = option.FrameworkDirName;
    appBinDirName = option.AppBinDirName;
    dotnetJsName = option.DotnetJsName;
    dotnetWasmName = option.DotnetWasmName;
    dotnetAssemblies = option.Assemblies;

    /** @type ModuleType */
    const _Module = {};
    _Module.print = WriteStdOut;
    _Module.printErr = WriteStdError;
    _Module.locateFile = LocateFile;
    _Module.preRun = [];
    _Module.postRun = [];
    _Module.preloadPlugins = [];
    _Module.preRun.push(PreRun);
    _Module.postRun.push(PostRun);

    global = globalThis;
    self.Module = _Module;

    self.importScripts(BuildPath(dotnetJsName));
}

/**
 * Builds path to fetch.
 * @private
 * @param {string} name fileName which you want to fetch.
 * @returns {string} relative path to file.
 */
function BuildPath(name) {
    return basePath + "/" + frameworkDirName + "/" + name;
}

/**
 * Implements worker's stdout.
 * @private
 * @param {string} message message to write.
 * @returns {void}
 */
function WriteStdOut(message) {
    console.log("worker-stdout:" + message);
}

/**
 * Implements worker's stderror.
 * @private
 * @param {any} message message to write
 * @returns {void}
 */
function WriteStdError(message) {
    console.log("workerstderror:" + message);
}

/**
 * Provides custom logic to locate file.
 * @private
 * @param {string} fileName filename about to load.
 * @returns {string} new filepath.
 */
function LocateFile(fileName) {
    if (fileName == "dotnet.wasm") {
        return BuildPath(dotnetWasmName);
    }
    return fileName;
}

/**
 * Load assembly here.
 * @private
 * @returns {Promise<void>}
 * */
async function PreRun() {
    const mono_wasm_add_assembly = Module.cwrap('mono_wasm_add_assembly', null, ['string', 'number', 'number',]);
    MONO.loaded_files = [];

    dotnetAssemblies.forEach(async (fileName) => {
        const runDependencyId = `blazor:${fileName}`;
        addRunDependency(runDependencyId);
        const result = await fetch(BuildPath(fileName));
        if (result.ok) {
            const arrayBuffer = await result.arrayBuffer();
            const data = new Uint8Array(arrayBuffer);

            const heapAddress = Module._malloc(data.length);
            const heapMemory = new Uint8Array(Module.HEAPU8.buffer, heapAddress, data.length);
            heapMemory.set(data);
            mono_wasm_add_assembly(fileName, heapAddress, data.length);
            MONO.loaded_files.push(fileName);
            removeRunDependency(runDependencyId);
        } else {
            removeRunDependency(runDependencyId);
            console.error("failed to fetch:" + fileName);
        }
    });
}

/**
 * Finalize boot process here.
 * @private
 * @returns {void}
 * */
function PostRun() {
    MONO.mono_wasm_setenv("MONO_URI_DOTNETRELATIVEORABSOLUTE", "true");
    _mono_wasm_load_runtime(appBinDirName, 0);
    MONO.mono_wasm_runtime_is_ready = true;
    InitializeMessagingService();
    postMessage("_init");
}

/**
 * Initialize message dispatch service here. You can call .NET method here.
 * @private
 * @returns {void}
 * */
function InitializeMessagingService() {

}

/**
 * Handles message from parent. 
 * @private
 * @param {MessageEvent} message message from parent
 * @returns {void}
 */
function OnMessageReceived(message) {
    if (message.data.endsWith("Fuga")) {
        Module.mono_call_static_method(message.data, []);
    }
    if (message.data.endsWith("Piyo")) {
        Module.mono_call_static_method(message.data, ["argument from js"]);
    }
}



// これデバッグ用なので消えます
function _postMessage(message) {
    postMessage(message);
}


// #region typedef

/**
 * @typedef WorkerInitOption
 * @property {string} BasePath
 * @property {string} FrameworkDirName
 * @property {string} AppBinDirName
 * @property {string} DotnetJsName
 * @property {string} DotnetWasmName
 * @property {string[]} Assemblies
 * */

/**
 * @typedef ModuleType
 * @property {function(string):void} print
 * @property {function(string):void} printErr
 * @property {function(string):string} locateFile
 * @property {Array<function():Promise<void> | void>} preRun
 * @property {Array<function():Promise<void> | void>} postRun
 * @property {Array<function():Promise<void> | void>} preloadPlugins
 * */

// #endregion

// #region utility

// must sync following to parent
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
    const array = new Uint8Array(wasmMemory.buffer, ptr + dotnetArrayOffset, len);
    const str = len > nativeLen ? nativeDecoder.decode(array) : JSDecoder.Decode(array);
    return JSON.parse(str);
}

const JSDecoder = {
    // code from anonyco/FastestSmallestTextEncoderDecoder
    // Creative Commons Zero v1.0 Universal

    /**
     * Decode UTF-8 Encoded binary to JS string.
     * @param {any} inputArrayOrBuffer
     * @returns {string}
     */
    Decode: function Decode(inputArrayOrBuffer) {
        let fromCharCode = String.fromCharCode;
        let Object_prototype_toString = ({}).toString;
        let sharedArrayBufferString = Object_prototype_toString.call(self["SharedArrayBuffer"]);
        let undefinedObjectString = Object_prototype_toString();
        let NativeUint8Array = self.Uint8Array;
        let patchedU8Array = NativeUint8Array || Array;
        let nativeArrayBuffer = NativeUint8Array ? ArrayBuffer : patchedU8Array;
        let arrayBuffer_isView = nativeArrayBuffer.isView || function (x) { return x && "length" in x };
        let arrayBufferString = Object_prototype_toString.call(nativeArrayBuffer.prototype);
        let tmpBufferU16 = new (NativeUint8Array ? Uint16Array : patchedU8Array)(32);

        let inputAs8 = inputArrayOrBuffer, asObjectString;
        if (!arrayBuffer_isView(inputAs8)) {
            asObjectString = Object_prototype_toString.call(inputAs8);
            if (asObjectString !== arrayBufferString && asObjectString !== sharedArrayBufferString && asObjectString !== undefinedObjectString)
                throw TypeError("Failed to execute 'decode' on 'TextDecoder': The provided value is not of type '(ArrayBuffer or ArrayBufferView)'");
            inputAs8 = NativeUint8Array ? new patchedU8Array(inputAs8) : inputAs8 || [];
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
                                    tmpBufferU16[pos] = tmp;
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
                        tmpBufferU16[pos] = cp0; // fill with invalid replacement character
                        continue;
                    case 11:
                    case 10:
                    case 9:
                    case 8:
                }
                tmpBufferU16[pos] = 0xfffd; // fill with invalid replacement character
            }
            tmpStr += fromCharCode(
                tmpBufferU16[0], tmpBufferU16[1], tmpBufferU16[2], tmpBufferU16[3], tmpBufferU16[4], tmpBufferU16[5], tmpBufferU16[6], tmpBufferU16[7],
                tmpBufferU16[8], tmpBufferU16[9], tmpBufferU16[10], tmpBufferU16[11], tmpBufferU16[12], tmpBufferU16[13], tmpBufferU16[14], tmpBufferU16[15],
                tmpBufferU16[16], tmpBufferU16[17], tmpBufferU16[18], tmpBufferU16[19], tmpBufferU16[20], tmpBufferU16[21], tmpBufferU16[22], tmpBufferU16[23],
                tmpBufferU16[24], tmpBufferU16[25], tmpBufferU16[26], tmpBufferU16[27], tmpBufferU16[28], tmpBufferU16[29], tmpBufferU16[30], tmpBufferU16[31]
            );
            if (pos < 32) tmpStr = tmpStr.slice(0, pos - 32 | 0);//-(32-pos));
            if (index < len) {
                //fromCharCode.apply(0, tmpBufferU16 : NativeUint8Array ?  tmpBufferU16.subarray(0,pos) : tmpBufferU16.slice(0,pos));
                tmpBufferU16[0] = tmp;
                pos = (~tmp) >>> 31;//tmp !== -1 ? 1 : 0;
                tmp = -1;

                if (tmpStr.length < resultingString.length) continue;
            } else if (tmp !== -1) {
                tmpStr += fromCharCode(tmp);
            }

            resultingString += tmpStr;
            tmpStr = "";
        }

        return resultingString;
    }
}