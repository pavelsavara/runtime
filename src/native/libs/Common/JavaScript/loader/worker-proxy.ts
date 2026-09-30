// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// -----------------------------------------------------------
// Main-thread worker proxy for withWorker() mode.
//
// Creates shared WebAssembly.Memory, spawns a dedicated worker
// running the same dotnet.js module, and returns a RuntimeAPI
// that proxies runMain/runMainAndExit to the worker via shared
// memory signaling.
//
// Full JSImport/JSExport interop is deferred to a future phase.
// -----------------------------------------------------------

import type { LoaderConfig, RuntimeAPI } from "./types";

import {
    SignalState,
    CommandId,
    OFFSET_WAKEUP_MAIN,
    OFFSET_COMMAND_SIGNAL,
    OFFSET_COMMAND_ID,
    OFFSET_COMMAND_RESULT,
    wakeWorker,
} from "./shared-signaling";

import type { WorkerInitMessage, WorkerToMainMessage } from "./worker-runtime";
import { scriptUrl } from "./bootstrap";
import { createPromiseCompletionSource } from "./promise-completion-source";

const DEFAULT_TIMEOUT_MS = 30_000;

export async function createWorkerProxy(config: LoaderConfig): Promise<RuntimeAPI> {

    const worker = new globalThis.Worker(scriptUrl, { type: "module" });

    // Post init message with config
    const initMsg: WorkerInitMessage = {
        type: "init",
        config,
    };
    worker.postMessage(initMsg);
    const { promise: apiPromise, resolve, reject } = createPromiseCompletionSource<RuntimeAPI>();
    let timeoutId: ReturnType<typeof setTimeout> | undefined;

    const timeoutMs = config.workerOptions?.timeoutMs ?? DEFAULT_TIMEOUT_MS;
    if (timeoutMs > 0) {
        timeoutId = setTimeout(() => {
            //TODO worker.terminate();
            reject(new Error(`withWorker(): worker did not respond within ${timeoutMs}ms`));
        }, timeoutMs);
    }

    worker.onmessage = (ev: MessageEvent<WorkerToMainMessage>): void => {
        const msg = ev.data;

        if (msg.type === "ready") {
            if (timeoutId !== undefined) {
                clearTimeout(timeoutId);
            }

            const signalView = new Int32Array(msg.signalBuffer);
            const runtimeBuildInfo = msg.runtimeBuildInfo as RuntimeAPI["runtimeBuildInfo"];
            const api = buildProxyRuntimeAPI(config, signalView, runtimeBuildInfo, worker);
            resolve(api);
        } else if (msg.type === "error") {
            if (timeoutId !== undefined) {
                clearTimeout(timeoutId);
            }
            reject(new Error(`withWorker(): worker initialization failed: ${msg.message}`));
        }
    };

    worker.onerror = (ev: ErrorEvent): void => {
        if (timeoutId !== undefined) {
            clearTimeout(timeoutId);
        }
        reject(new Error(`withWorker(): worker error: ${ev.message}`));
    };

    return await apiPromise;
}

// -----------------------------------------------------------
// Build the proxy RuntimeAPI returned to the caller
//
// Only runMain, runMainAndExit, exit, getConfig, and
// runtimeBuildInfo are functional. All other APIs throw
// "not supported in worker proxy mode" for now.
// -----------------------------------------------------------

function buildProxyRuntimeAPI(
    config: LoaderConfig,
    signalView: Int32Array,
    runtimeBuildInfo: RuntimeAPI["runtimeBuildInfo"],
    worker: Worker,
): RuntimeAPI {
    function sendCommandAndWait(commandId: CommandId): Promise<number> {
        // Write command to signaling buffer
        Atomics.store(signalView, OFFSET_COMMAND_ID, commandId);
        Atomics.store(signalView, OFFSET_COMMAND_SIGNAL, SignalState.PENDING);
        wakeWorker(signalView);

        // Wait asynchronously for the worker to complete the command
        return waitForCommandCompletion(signalView);
    }

    const notSupported = (name: string) => (): never => {
        throw new Error(`${name}() is not supported in withWorker() proxy mode`);
    };

    const api = {
        // Functional APIs
        runMain: (_mainAssemblyName?: string, _args?: string[]): Promise<number> => {
            return sendCommandAndWait(CommandId.RUN_MAIN);
        },
        runMainAndExit: (_mainAssemblyName?: string, _args?: string[]): Promise<number> => {
            return sendCommandAndWait(CommandId.RUN_MAIN);
        },
        exit: (code: number, _reason?: any): void => {
            Atomics.store(signalView, OFFSET_COMMAND_RESULT, code);
            Atomics.store(signalView, OFFSET_COMMAND_ID, CommandId.EXIT);
            Atomics.store(signalView, OFFSET_COMMAND_SIGNAL, SignalState.PENDING);
            wakeWorker(signalView);
            worker.terminate();
        },
        getConfig: (): LoaderConfig => config,
        runtimeBuildInfo,

        // Stubs — not yet supported in proxy mode
        INTERNAL: {},
        Module: {} as any,
        runtimeId: 0,
        setEnvironmentVariable: notSupported("setEnvironmentVariable"),
        getAssemblyExports: notSupported("getAssemblyExports"),
        setModuleImports: notSupported("setModuleImports"),
        invokeLibraryInitializers: notSupported("invokeLibraryInitializers"),

        // Memory APIs — not supported in proxy mode
        setHeapB32: notSupported("setHeapB32"),
        setHeapU8: notSupported("setHeapU8"),
        setHeapU16: notSupported("setHeapU16"),
        setHeapU32: notSupported("setHeapU32"),
        setHeapI8: notSupported("setHeapI8"),
        setHeapI16: notSupported("setHeapI16"),
        setHeapI32: notSupported("setHeapI32"),
        setHeapI52: notSupported("setHeapI52"),
        setHeapU52: notSupported("setHeapU52"),
        setHeapI64Big: notSupported("setHeapI64Big"),
        setHeapF32: notSupported("setHeapF32"),
        setHeapF64: notSupported("setHeapF64"),
        getHeapB32: notSupported("getHeapB32"),
        getHeapU8: notSupported("getHeapU8"),
        getHeapU16: notSupported("getHeapU16"),
        getHeapU32: notSupported("getHeapU32"),
        getHeapI8: notSupported("getHeapI8"),
        getHeapI16: notSupported("getHeapI16"),
        getHeapI32: notSupported("getHeapI32"),
        getHeapI52: notSupported("getHeapI52"),
        getHeapU52: notSupported("getHeapU52"),
        getHeapI64Big: notSupported("getHeapI64Big"),
        getHeapF32: notSupported("getHeapF32"),
        getHeapF64: notSupported("getHeapF64"),
        localHeapViewI8: notSupported("localHeapViewI8"),
        localHeapViewI16: notSupported("localHeapViewI16"),
        localHeapViewI32: notSupported("localHeapViewI32"),
        localHeapViewI64Big: notSupported("localHeapViewI64Big"),
        localHeapViewU8: notSupported("localHeapViewU8"),
        localHeapViewU16: notSupported("localHeapViewU16"),
        localHeapViewU32: notSupported("localHeapViewU32"),
        localHeapViewF32: notSupported("localHeapViewF32"),
        localHeapViewF64: notSupported("localHeapViewF64"),

        // Diagnostics APIs — not supported in proxy mode
        collectCpuSamples: notSupported("collectCpuSamples"),
        collectMetrics: notSupported("collectMetrics"),
        collectGcDump: notSupported("collectGcDump"),
        connectDSRouter: notSupported("connectDSRouter"),
    } as unknown as RuntimeAPI;

    return api;
}

// -----------------------------------------------------------
// Async wait for command completion using Atomics.waitAsync
//
// Waits on OFFSET_WAKEUP_MAIN for notifications from the worker.
// On each wakeup, checks whether the command signal has moved
// from PENDING to DONE or ERROR.
// -----------------------------------------------------------

function waitForCommandCompletion(signalView: Int32Array): Promise<number> {
    return new Promise<number>((resolve, reject) => {
        function check(): void {
            const signal = Atomics.load(signalView, OFFSET_COMMAND_SIGNAL);
            if (signal === SignalState.DONE) {
                const result = Atomics.load(signalView, OFFSET_COMMAND_RESULT);
                Atomics.store(signalView, OFFSET_COMMAND_SIGNAL, SignalState.IDLE);
                resolve(result);

                return;
            }
            if (signal === SignalState.ERROR) {
                const result = Atomics.load(signalView, OFFSET_COMMAND_RESULT);
                Atomics.store(signalView, OFFSET_COMMAND_SIGNAL, SignalState.IDLE);
                reject(new Error(`Worker command failed with exit code ${result}`));

                return;
            }

            // Not yet complete — wait for next wakeup
            const lastSeen = Atomics.load(signalView, OFFSET_WAKEUP_MAIN);
            const waitResult = Atomics.waitAsync(signalView, OFFSET_WAKEUP_MAIN, lastSeen);
            if (waitResult.async) {
                waitResult.value.then(check);
            } else {
                // "not-equal" — value already changed, check immediately
                setTimeout(check, 0);
            }
        }

        check();
    });
}
