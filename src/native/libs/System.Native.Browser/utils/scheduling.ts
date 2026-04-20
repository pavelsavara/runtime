// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { _ems_ } from "../../Common/JavaScript/ems-ambient";

let wasmCallChain: Promise<void> | null = null;
let callDepthAsync = 0;

// eslint-disable-next-line prefer-const
let isSuspended = false;

export function serializeWasmCallSync<T>(fn: () => T): T {
    if (_ems_.DOTNET.isAborting) {
        return undefined as any;
    }
    if (isSuspended) {
        _ems_.dotnetApi.exit(1, new Error("Cannot make a synchronous call into the runtime while an async call is in-flight."));
    }
    return fn();
}

export function serializeWasmCall<T>(fn: () => Promise<T> | T): Promise<T> {
    if (_ems_.DOTNET.isAborting) {
        return Promise.reject(new Error("dotnet is aborting"));
    }
    // Sync fast-path: no suspension pending and not nested — invoke directly, skip .then() overhead
    if (callDepthAsync === 0 && !isSuspended) {
        callDepthAsync++;
        let result: Promise<T> | T;
        try {
            result = fn();
        } catch (err) {
            callDepthAsync--;
            wasmCallChain = null;
            return Promise.reject(err);
        }
        // If fn() returned a plain value (no suspension), complete synchronously
        if (!(result instanceof Promise)) {
            callDepthAsync--;
            wasmCallChain = null;
            return Promise.resolve(result);
        }
        // Chain follow-up calls behind this one
        wasmCallChain = result.then(() => { }, () => { });
        result.finally(() => {
            callDepthAsync--;
            if (callDepthAsync === 0) {
                wasmCallChain = null;
            }
        });
        return result;
    }
    // Slow path: a call is suspended or pending — queue behind the chain
    callDepthAsync++;
    const queued = (wasmCallChain ?? Promise.resolve()).then(() => {
        if (_ems_.DOTNET.isAborting) {
            throw new Error("dotnet is aborting");
        }
        return fn();
    });
    wasmCallChain = queued.then(() => { }, () => { });
    queued.finally(() => {
        callDepthAsync--;
        if (callDepthAsync === 0) {
            wasmCallChain = null;
        }
    });
    return queued;
}

export function isSuspensionInFlight(): boolean {
    return isSuspended;
}

export async function runBackgroundTimers(): Promise<void> {
    if (_ems_.ABORT || _ems_.DOTNET.isAborting) {
        // runtime is shutting down
        return;
    }
    try {
        await _ems_.dotnetBrowserUtilsExports.serializeWasmCall(async () => {
            await _ems_._SystemJS_ExecuteTimerCallback();
            await _ems_._SystemJS_ExecuteBackgroundJobCallback();
            await _ems_._SystemJS_ExecuteFinalizationCallback();
            await _ems_._SystemJS_ExecuteDiagnosticServerCallback();
        });
    } catch (error: any) {
        // do not propagate ExitStatus exception
        if (!error || typeof error.status !== "number") {
            _ems_.dotnetApi.exit(1, error);
            throw error;
        }
    }
}

export function abortBackgroundTimers(): void {
    if (_ems_.DOTNET.lastScheduledTimerId) {
        globalThis.clearTimeout(_ems_.DOTNET.lastScheduledTimerId);
        _ems_.runtimeKeepalivePop();
        _ems_.DOTNET.lastScheduledTimerId = undefined;
    }
    if (_ems_.DOTNET.lastScheduledThreadPoolId) {
        globalThis.clearTimeout(_ems_.DOTNET.lastScheduledThreadPoolId);
        _ems_.runtimeKeepalivePop();
        _ems_.DOTNET.lastScheduledThreadPoolId = undefined;
    }
    if (_ems_.DOTNET.lastScheduledFinalizationId) {
        globalThis.clearTimeout(_ems_.DOTNET.lastScheduledFinalizationId);
        _ems_.runtimeKeepalivePop();
        _ems_.DOTNET.lastScheduledFinalizationId = undefined;
    }
    if (_ems_.DOTNET.lastScheduledDiagnosticServerId) {
        globalThis.clearTimeout(_ems_.DOTNET.lastScheduledDiagnosticServerId);
        _ems_.runtimeKeepalivePop();
        _ems_.DOTNET.lastScheduledDiagnosticServerId = undefined;
    }
}

