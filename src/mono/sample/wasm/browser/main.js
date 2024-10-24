// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { dotnet, exit } from './_framework/dotnet.js'

function displayMeaning(meaning) {
    document.getElementById("out").innerHTML = `${meaning}`;
}

try {
    const { setModuleImports, INTERNAL, Module } = await dotnet
        .withElementOnExit()
        .create();

    setModuleImports("main.js", {
        Sample: {
            Test: {
                displayMeaning
            }
        }
    });
    globalThis.mono_wasm_download_heap = INTERNAL.mono_wasm_download_heap;
    globalThis.mono_wasm_load_heap = INTERNAL.mono_wasm_load_heap;
    globalThis.mono_wasm_perform_heapshot = INTERNAL.mono_wasm_perform_heapshot;
}
catch (err) {
    exit(2, err);
}
