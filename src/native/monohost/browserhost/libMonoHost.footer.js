//! Licensed to the .NET Foundation under one or more agreements.
//! The .NET Foundation licenses this file to you under the MIT license.

/**
 * This is root of **Emscripten library** that would become part of `dotnet.native.js`
 * It implements the Mono host and JS related to runtime hosting.
 */

/* eslint-disable no-undef */
function libMonoHostFactory() {
    const exports = {};
    libMonoHost(exports);

    let explicitDeps = [
        "wasm_load_icu_data",
        "BrowserHost_AddAssembly",
        "BrowserHost_CreateHostContract",
        "BrowserHost_InitializeDotnet",
        "BrowserHost_ExecuteAssembly"
    ];
    let commonDeps = [
        "$DOTNET",
        "$DOTNET_INTEROP",
        "$ENV",
        "$FS",
        "$libMonoHostFn",
        ...explicitDeps
    ];
    const mergeMonoHost = {
        $MONO_HOST: {
            selfInitialize: () => {
                if (typeof dotnetInternals !== "undefined") {
                    MONO_HOST.dotnetInternals = dotnetInternals;

                    const exports = {};
                    libMonoHostFn(exports);
                    exports.dotnetInitializeModule(dotnetInternals);
                    MONO_HOST.assignExports(exports, MONO_HOST);
                }
            },
        },
        $libMonoHostFn: libMonoHost,
        $MONO_HOST__postset: "MONO_HOST.selfInitialize()",
        $MONO_HOST__deps: commonDeps,

        GetDotNetRuntimeContractDescriptor: () => { throw new Error("GetDotNetRuntimeContractDescriptor is not implemented in browserhost"); },

        // ----------------------------------------------------------------
        // driver.c / corebindings.c — misc
        // ----------------------------------------------------------------
        mono_wasm_trace_logger: (logDomain, logLevel, message, fatal, userData) => { console.log("STUB: mono_wasm_trace_logger"); },
        mono_wasm_set_entrypoint_breakpoint: (assemblyName, methodToken) => { console.log("STUB: mono_wasm_set_entrypoint_breakpoint"); },

        // ----------------------------------------------------------------
        // mini-wasm-debugger.c
        // ----------------------------------------------------------------
        mono_wasm_add_dbg_command_received: (resOk, id, buffer, bufferLen) => { },
        mono_wasm_asm_loaded: (asmName, assemblyData, assemblyLen, pdbData, pdbLen) => { },
        mono_wasm_debugger_log: (level, message) => { },
        mono_wasm_fire_debugger_agent_message_with_data: (data, len) => { },
        mono_wasm_fire_debugger_agent_message_with_data_to_pause: (data, len) => { },

        // ----------------------------------------------------------------
        // jiterpreter — stubs (jiterpreter is not used in browserhost)
        // ----------------------------------------------------------------
        mono_interp_tier_prepare_jiterpreter: (frame, method, ip, traceIndex, startOfBody, sizeOfBody, isVerbose, presetFunctionPointer) => 0,
        mono_interp_record_interp_entry: (fnPtr) => { },
        mono_interp_jit_wasm_entry_trampoline: (imethod, method, argumentCount, paramTypes, unbox, hasThis, hasReturn, defaultImplementation) => 0,
        mono_interp_jit_wasm_jit_call_trampoline: (method, rmethod, cinfo, argOffsets, catchExceptions) => { },
        mono_interp_invoke_wasm_jit_call_trampoline: (thunk, retSp, sp, ftndesc, thrown) => { },
        mono_interp_flush_jitcall_queue: () => { },
        mono_wasm_free_method_data: (method, imethod, traceIndex) => { },

        // ----------------------------------------------------------------
        // eventpipe
        // ----------------------------------------------------------------
        mono_wasm_profiler_now: () => (typeof performance !== "undefined" ? performance.now() : Date.now()),
        mono_wasm_profiler_record: (method, start) => { },

        ds_rt_websocket_create: (url) => -1,
        ds_rt_websocket_send: (clientSocket, buffer, bytesToWrite) => -1,
        ds_rt_websocket_poll: (clientSocket) => -1,
        ds_rt_websocket_recv: (clientSocket, buffer, bytesToRead) => -1,
        ds_rt_websocket_close: (clientSocket) => -1,

        SystemJS_GetCurrentProcessId: () => 42,
        SystemJS_ExecuteFinalizationCallback: () => { console.log("STUB: SystemJS_ExecuteFinalizationCallback"); },
    };

    let assignExportsBuilder = "";
    let explicitImportsBuilder = "";
    for (const exportName of Reflect.ownKeys(exports)) {
        const name = String(exportName);
        if (name === "dotnetInitializeModule" || name === "runtimeFlavor") continue;
        if (exports.runtimeFlavor === "Mono" && name === "BrowserHost_ExternalAssemblyProbe") continue;
        mergeMonoHost[name] = () => "dummy";
        assignExportsBuilder += `_${String(name)} = exports.${String(name)};\n`;
    }
    for (const importName of explicitDeps) {
        explicitImportsBuilder += `_${importName}();\n`;
    }
    mergeMonoHost.$MONO_HOST.assignExports = new Function("exports", assignExportsBuilder);
    mergeMonoHost.$MONO_HOST.explicitImports = new Function(explicitImportsBuilder);

    autoAddDeps(mergeMonoHost, "$MONO_HOST");
    addToLibrary(mergeMonoHost);
}

libMonoHostFactory();
