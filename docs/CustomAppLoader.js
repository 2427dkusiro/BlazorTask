import { BrotliDecode } from './decode.min.js';

Blazor.start({
    loadBootResource: function (type, name, defaultUri, integrity) {
        // this means app is in debug mode.
        if (location.hostname === "localhost" && location.port !== "5500") {
            const span = document.getElementById("ProgressLoadMode");
            if (span.textContent === "") {
                span.textContent = "Brotli圧縮無効";
                span.className += " text-danger";
            }
            return;
        }

        if (type !== 'dotnetjs' && location.hostname !== 'localhost') {
            return (async function () {
                const response = await fetch(defaultUri + '.br', { cache: 'no-cache' });
                if (!response.ok) {
                    throw new Error(response.statusText);
                }
                const originalResponseBuffer = await response.arrayBuffer();
                const originalResponseArray = new Int8Array(originalResponseBuffer);
                const decompressedResponseArray = BrotliDecode(originalResponseArray);

                // integrity check code
                /*
                if (integrity != "") {
                    const digest = await crypto.subtle.digest("sha-256", decompressedResponseArray);
                    const bytes = new Uint8Array(digest);
                    var binary = "";
                    var len = bytes.byteLength;
                    for (var i = 0; i < len; i++) {
                        binary += String.fromCharCode(bytes[i]);
                    }
                    const digestString = window.btoa(binary).replace("/", "\/");

                    const computedHash = "sha256-" + digestString;
                    if (integrity !== computedHash) {
                        console.error("Failed to find a valid digest for resource '" + name + "' with computed SHA-256 integrity '" + computedHash + "'. The resource has been blocked.");
                        return null;
                    }
                }
                */

                const contentType = type ===
                    'dotnetwasm' ? 'application/wasm' : 'application/octet-stream';
                return new Response(decompressedResponseArray,
                    { headers: { 'content-type': contentType } });
            })();
        }
    }
});