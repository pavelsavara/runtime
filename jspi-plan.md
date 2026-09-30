# Plan: JSPI-based Lazy Assembly Loading in BrowserHost

## TL;DR

Use the WebAssembly JavaScript Promise Integration (JSPI) API to lazy-load non-core assemblies on-demand in `BrowserHost_ExternalAssemblyProbe` instead of pre-downloading all assemblies before runtime startup. This reduces initial load time by deferring assembly fetches until CoreCLR actually requests them. A single binary is produced that detects JSPI at runtime and falls back to eager loading if unavailable.

## JSPI Summary

**What it is**: A W3C standardized API (phase 4, shipped in Chrome 137+, Firefox 139+) that lets synchronous WASM code transparently call async JS functions.

**API surface** (two elements):
- `new WebAssembly.Suspending(jsFun)` — wraps a JS function that returns a Promise. When used as a WASM import, calling it from WASM suspends the WASM execution until the Promise resolves.
- `WebAssembly.promising(wasmFun)` — wraps a WASM export so it returns a Promise. The calling JS code receives a Promise that resolves when the WASM function completes (including after any suspensions).

**Key constraints:**
- Only WASM frames may exist on the stack between the `promising` export and `Suspending` import (no JS frames in between)
- Cannot suspend JS code — only WASM computations
- The wrapped import function doesn't HAVE to return a Promise — if it returns a non-Promise value, no suspension occurs (synchronous fast path)

**Not Asyncify**: JSPI is NOT the old Emscripten Asyncify. Asyncify instruments the WASM binary to save/restore call stacks (50%+ code size overhead). JSPI uses VM-level stack switching with zero WASM code changes and constant-time overhead (~1μs per suspension).

## Emscripten 3.1.56 Assessment

- **`-sJSPI` flag exists** in 3.1.56 (added in ~3.1.40), but it uses the **old WebAssembly.Function-based API** (Suspender object pattern)
- **Current browsers** (Chrome 137+, Firefox 139+) implement the **new API** (`WebAssembly.Suspending`/`WebAssembly.promising`)
- The old API is **no longer supported** in current browsers
- **Emscripten >= 3.1.61** is needed for the new standardized JSPI API
- **However**: For the "manual wrapping" approach (single binary), no Emscripten `-sJSPI` flag is needed at all — JSPI is applied at the JS level during instantiation

## Existing FOSS Libraries

No existing standalone FOSS library for JSPI wrapping was found. The WordPress/PHP-WASM project uses Emscripten's built-in `-sJSPI` flag. All other known usages are Emscripten-integrated. Creating a standalone "JSPI blocker" helper doesn't help here because the main module's imports/exports must themselves be wrapped — a separate WASM module can't suspend the main module's execution.

## Approach: Manual JSPI Wrapping (Single Binary)

Rather than using Emscripten's `-sJSPI` flag (which would make the binary fail on non-JSPI engines), apply JSPI wrapping at the JavaScript level during WASM instantiation:

1. Detect JSPI at startup: `typeof WebAssembly.Suspending !== 'undefined'`
2. If available: wrap imports/exports during `instantiateMainWasm`
3. If unavailable: current eager-loading behavior (no code changes)

This produces a single binary that works everywhere.

### Architecture

```
Without JSPI (current):
  startup → download ALL assemblies → initializeCoreCLR → BrowserHost_ExecuteAssembly
  BrowserHost_ExternalAssemblyProbe: lookup in loadedAssemblies map (sync)

With JSPI:
  startup → start ALL downloads (core eagerly awaited, non-core throttled at 2 parallel in background)
         → initializeCoreCLR → BrowserHost_ExecuteAssembly (promising)
  BrowserHost_ExternalAssemblyProbe (suspending):
    if in cache (already downloaded in background) → return sync (no suspension)
    if not yet downloaded → await its fetch → register → return (WASM suspended during fetch)
```

## Current Progress

> Last updated from pending changes review.

| Phase | Status | Notes |
|-------|--------|-------|
| Phase 1: JSPI Detection | **NOT STARTED** | No detection or `jspiAvailable` flag exists |
| Phase 2: Throttled Background Loading | **NOT STARTED** | Downloads still fully parallel; need to throttle to 2 concurrent |
| Phase 3: JSPI Wrapping (types + async callers) | **PARTIALLY DONE** | Type signatures changed, JS callers async — but `instantiateMainWasm` wrapping and `WebAssembly.promising`/`Suspending` not applied |
| Phase 4: Async Assembly Probe | **NOT STARTED** | `BrowserHost_ExternalAssemblyProbe` still synchronous |
| Phase 5: `runMain` Adaptation | **DONE** | `runMain` and `initializeCoreCLR` are async + serialized |
| Phase 6: Call Serializer | **MOSTLY DONE** | Serializer implemented, wired to most call sites; sync call sites marked `TODO-JSPI-THROW-WHEN-SUSPENDED`; `isSuspended` flag declared but never set to `true` |

### Completed changes (in working tree):

- **Call serializer** (`serializeWasmCall`, `serializeWasmCallSync`, `isSuspensionInFlight`) in new file `System.Native.Browser/utils/scheduling.ts`
- **Cross-module wiring**: serializer + `runBackgroundTimers` + `abortBackgroundTimers` moved to `scheduling.ts`, exported via `BrowserUtilsExports` table slots [11-13]
- **Type changes in `ems-ambient.ts`**: `_BrowserHost_ExecuteAssembly`, `_BrowserHost_InitializeDotnet`, timer callbacks, `_SystemInteropJS_CompleteTask`, `_SystemInteropJS_BindAssemblyExports`, `_SystemInteropJS_CallJSExport` all return `Promise`
- **`BrowserHostExports` type** added to `ems-ambient.ts` import and `_ems_.dotnetBrowserHostExports` added
- **`host.ts`**: `initializeCoreCLR()` → `async`, wraps body in `serializeWasmCall`; `runMain()` → `await serializeWasmCall` for `_BrowserHost_ExecuteAssembly`; `stackAlloc` → `_malloc`/`_free` for args surviving suspension
- **`run.ts`**: `initializeCoreCLR()` → `async`, `await`s the host call
- **`invoke-cs.ts`**: `bindFn` split into `bindFnSync` (synchronous exports) and `bindFnAsync` (async/DiscardNoWait exports); async paths use `allocHeapFrame` instead of `allocStackFrame`
- **`managed-exports.ts`**: `invokeJSExport` → `async` + `serializeWasmCall`; new `invokeJSExportSync` (sync + `serializeWasmCallSync`); `callDelegate` → `serializeWasmCallSync`; `completeTask` → `async` + `serializeWasmCall` + `allocHeapFrame`; `bindAssemblyExports` → `async` + `serializeWasmCall` + `allocHeapFrame`
- **`marshaled-types.ts`**: `PromiseHolder.resolve/reject/cancel/completeTaskWrapper` all `async`
- **`cancelable-promise.ts`**: `cancelPromise` → fire-and-forget `.then(() => {}, () => {})`
- **`marshal-to-js.ts`**: delegate closure return type → `Promise<any>`
- **`marshal.ts`**: new `allocHeapFrame()` for frames that survive across suspension
- **Timer scheduling** (`System.Native.Browser/native/scheduling.ts`): all tick functions → `async`, wrapped in `serializeWasmCall`
- **`preventTimerThrottlingTick`** skips `runBackgroundTimers` if `isSuspensionInFlight()`
- **Exit hook** (`System.Native.Browser/native/index.ts`): skips `_BrowserHost_ShutdownDotnet` if suspension in-flight; `abortPosix` reordered to set `ABORT` after action
- **Tests**: `JsExportInt32DiscardNoWait` and `JsImportInt32DiscardNoWait` work with async DiscardNoWait
- **Sample app** (`browser/wwwroot/main.js`): error handler adjusted for `ExitStatus`
- **CMakeLists.txt**: added `scheduling.ts` to `ROLLUP_TS_SOURCES`

### Synchronous call sites (TODO-JSPI-THROW-WHEN-SUSPENDED):

These call sites are synchronous and cannot be serialized via the async queue. They are marked with `TODO-JSPI-THROW-WHEN-SUSPENDED` — when JSPI wrapping is in place (Phase 3), `serializeWasmCallSync` will throw if called while WASM is suspended:
- `releaseJsOwnedObjectByGcHandle`
- `invokeJSExportSync` (synchronous JSExport calls via `bindFn_0V`, `bindFn_1V`, `bindFn_1R`, `bindFn_2R`, `bindFnSync`)
- `callDelegate` (synchronous delegate invocations)

### `isSuspended` flag:

The `isSuspended` flag in `scheduling.ts` is declared `let isSuspended = false` but never set to `true`. It will be set to `true` inside the async `BrowserHost_ExternalAssemblyProbe` when it needs to trigger a download and suspend WASM (Phase 4).

---

## Steps

### Phase 1: JSPI Detection & Config Plumbing — NOT STARTED

1. **Add JSPI detection** in `loader/bootstrap.ts`
   - Check `typeof WebAssembly.Suspending !== 'undefined' && typeof WebAssembly.promising !== 'undefined'`
   - Store result in a module-level flag (e.g., `jspiAvailable`) accessible from loader and host modules
   - Also check in Node.js (requires `--experimental-wasm-stack-switching` flag)

2. **Expose JSPI availability** through cross-module exchange
   - Add `enableJSPI` to the internal exchange or loader config so both loader and host modules can access it

### Phase 2: Throttled Background Loading — DONE

3. **Throttle non-core downloads to 2 parallel** in `loader/run.ts`
   - All downloads are still triggered at startup (not skipped)
   - Core assemblies (`coreAssembliesPromise`) are eagerly awaited before `initializeCoreCLR`
   - Non-core assemblies, PDBs, satellite resources download in background with max 2 concurrent fetches
   - This leaves network bandwidth for user-initiated requests (e.g., clicking a button that triggers a fetch)
   - `initializeCoreCLR` does NOT wait for non-core downloads — proceeds as soon as core assemblies are ready
   - Non-core downloads register into `loadedAssemblies` as they complete (background)
   - If `BrowserHost_ExternalAssemblyProbe` fires and the assembly is already downloaded → sync cache hit (no suspension)
   - If not yet downloaded → JSPI suspends WASM while the in-flight fetch completes (or starts a new one)

4. **Track deferred asset metadata** — the loader still needs the asset list (URLs, virtual paths) for on-demand fetching. Store a `Map<string, { promise: Promise<Uint8Array>, asset: AssemblyAsset }>` so the async probe can either join an in-flight download or start one.

### Phase 3: JSPI Import/Export Wrapping — PARTIALLY DONE

> The detailed plan for making all JS→managed call chains async (`ts-plan.md`) has been fully consumed and implemented.

**Done:** Type signatures in `ems-ambient.ts` changed to `Promise` returns. All JS callers converted to `async`/`await`. Serializer applied at call sites.

**Remaining:**

5. **Modify `instantiateMainWasm`** in `loader/assets.ts`
   - If JSPI available: before calling `WebAssembly.instantiate`, wrap the `BrowserHost_ExternalAssemblyProbe` import with `new WebAssembly.Suspending(asyncProbeFunction)`
   - Find and replace `imports.env.BrowserHost_ExternalAssemblyProbe` (or whatever namespace Emscripten uses)
   - The async probe function: (a) check `loadedAssemblies` cache, (b) if miss, fetch + register + return true, (c) if unfetchable, return false

6. **Wrap all to-managed WASM exports** with `WebAssembly.promising`
   - After `successCallback(instance, module)`, replace Emscripten-exposed exports with `WebAssembly.promising(...)` wrappers for all C exports that call into managed code
   - This includes `_BrowserHost_ExecuteAssembly`, `_BrowserHost_InitializeDotnet`, `_BrowserHost_ShutdownDotnet`, all `_SystemInteropJS_*` (except `_GetManagedStackTrace`), and all `_SystemJS_Execute*Callback`
   - The JS callers are already converted to `async`/`await`
   - This is required for any suspensions deeper in the call chain to work
   - The `isSuspended` flag is set/cleared by the async probe (Phase 4), not by the promising wrapper itself

### Phase 4: Async Assembly Probe Implementation — NOT STARTED

7. **Implement async `BrowserHost_ExternalAssemblyProbe`** in `host/assets.ts`
   - New function `asyncBrowserHostExternalAssemblyProbe(pathPtr, outDataStartPtr, outSize)`:
     a. Check `loadedAssemblies` map → if found, return sync (no Promise, no suspension)
     b. If not found: set `isSuspended = true`, look up asset metadata by path → if found, await its fetch → register bytes → set out params → set `isSuspended = false` → return true (as Promise)
     c. If unknown path → return false (sync)
   - The "no Promise on cache hit" is crucial: JSPI only suspends when the import returns a Promise. Cache hits are zero-overhead.
   - Setting `isSuspended` is what causes `serializeWasmCallSync` to throw if any synchronous managed-entry call happens during the suspension
   - Need access to the fetch/download infrastructure from the loader module

8. **Bridge loader download infrastructure to host module**
   - The host module needs to call `fetchDll()` or equivalent to download an assembly on-demand
   - Maintain a download registry: `Map<string, { promise: Promise<Uint8Array>, asset: AssemblyAsset }>` mapping virtual paths to in-flight or pending downloads
   - When the async probe fires for a not-yet-cached assembly:
     - If an in-flight background download exists → join its promise (no duplicate fetch)
     - If no download started yet → start a new fetch
   - Expose `fetchAndRegisterAssembly(path)` that joins or starts a download, registers in `loadedAssemblies`, and resolves

### Phase 5: `runMain` Adaptation — DONE

9. **Update `runMain()` to handle promising export**
   - `_BrowserHost_ExecuteAssembly` now returns a Promise
   - `runMain` already `await`s the result via `serializeWasmCall`
   - `initializeCoreCLR` is async and serialized
   - `stackAlloc` replaced with `_malloc`/`_free` for args surviving suspension boundary

## Relevant Files

- `src/native/libs/Common/JavaScript/host/assets.ts` — `BrowserHost_ExternalAssemblyProbe`, `registerDllBytes`, `instantiateWebcilModule`, `loadedAssemblies` map
- `src/native/libs/Common/JavaScript/host/host.ts` — `runMain()`, `initializeCoreCLR()`, `_BrowserHost_ExecuteAssembly` calls
- `src/native/libs/Common/JavaScript/host/index.ts` — exports, `BrowserHost_ExternalAssemblyProbe` re-export, module initialization
- `src/native/libs/Common/JavaScript/loader/run.ts` — `createRuntime()` with all assembly download orchestration
- `src/native/libs/Common/JavaScript/loader/assets.ts` — `fetchDll()`, `instantiateMainWasm()`, `wasmMemoryPromiseController`, `nativeModulePromiseController`
- `src/native/libs/Common/JavaScript/loader/bootstrap.ts` — `validateWasmFeatures()` — add JSPI detection here
- `src/native/libs/Common/JavaScript/types/ems-ambient.ts` — type declarations for `_BrowserHost_ExecuteAssembly` etc.
- `src/native/corehost/browserhost/browserhost.cpp` — native `BrowserHost_ExternalAssemblyProbe` declaration, `host_runtime_contract`
- `src/native/corehost/browserhost/CMakeLists.txt` — Emscripten link flags
- `eng/Versions.props` — `EmsdkVersion=3.1.56`

## Verification

1. Build the browser host and verify it compiles without errors
2. Test with JSPI-capable browser (Chrome 137+): app should start with only core assemblies pre-loaded; non-core assemblies fetched on-demand when CoreCLR probes
3. Test with non-JSPI browser (e.g., older Safari): app should fall back to eager loading with no behavior change
4. Test with Node.js: requires `--experimental-wasm-stack-switching` flag
5. Verify cache hit path: second probe for same assembly should have zero JSPI overhead (sync return)
6. Verify error path: probe for non-existent assembly returns false without suspension
7. Network dev tools: verify non-core assemblies are NOT downloaded until after `BrowserHost_ExecuteAssembly` begins
8. Re-entrancy: trigger a `[JSExport]` call (e.g., button click) while an assembly fetch is in progress — verify the call is queued and executes after the fetch completes, not concurrently
9. Timer coalescing: verify timer ticks fired during WASM suspension are deferred and drain correctly after the suspension resolves

## Decisions

- **Single binary** approach chosen over build-time `-sJSPI` flag to support all engines
- **Core assemblies always eager-loaded** (they're needed for CoreCLR initialization before `BrowserHost_ExecuteAssembly`)
- **Manual JSPI wrapping** at JS level during instantiation (no Emscripten `-sJSPI` flag)
- Emscripten upgrade to >= 3.1.61 accepted but not strictly required for this approach
- **ICU data, core VFS** always eager-loaded (needed before runtime init)
- Non-core assemblies, PDBs, satellite resources download in background with **2 concurrent** fetch limit (preserves bandwidth for user requests)
- Downloads are NOT skipped — they start immediately but are not awaited before `initializeCoreCLR`
- **Call serializer** (Promise chain) for re-entrancy protection — all managed-entry calls serialized through a single queue; timer ticks and other callbacks deferred while WASM is suspended
- **MT out of scope** — multi-threaded browser mode solves the same problems differently and does not need JSPI-based serialization

### Phase 6: Re-Entrancy Protection (Call Serializer) — MOSTLY DONE

> **Problem:** When WASM is suspended (e.g., fetching a DLL during `BrowserHost_ExternalAssemblyProbe`), the JS event loop resumes. User interactions (button clicks calling `[JSExport]`), timer ticks, `completeTask` callbacks, FinalizationRegistry releases, and delegate invocations can all attempt to re-enter managed code while it is suspended. The managed runtime (Mono/CoreCLR in single-threaded browser mode) is **not** designed for interleaved execution — thread-statics, GC state, locks, and execution engine metadata would be corrupted.

10. **Implement a JS-level call serializer** — **DONE** (in `System.Native.Browser/utils/scheduling.ts`)

    Implementation: `serializeWasmCall<T>`, `serializeWasmCallSync<T>`, `isSuspensionInFlight()` with fast-path optimization (skips `.then()` when `callDepthAsync === 0 && !isSuspended`).

    **Serializer applied to:** (**DONE**)
    - `invokeJSExport` — async path via `serializeWasmCall`
    - `invokeJSExportSync` — sync path via `serializeWasmCallSync`
    - `callDelegate` — via `serializeWasmCallSync`
    - `completeTask` / `completeTaskWrapper` — via `serializeWasmCall` (async)
    - `bindAssemblyExports` — via `serializeWasmCall` (async)
    - Timer ticks (`SystemJS_ScheduleTimerTick`, `SystemJS_ScheduleBackgroundJobTick`, `SystemJS_ScheduleFinalizationTick`) — via `serializeWasmCall`
    - `runBackgroundTimers` — via `serializeWasmCall`
    - `preventTimerThrottlingTick` — skips if `isSuspensionInFlight()`
    - `initializeCoreCLR` — via `serializeWasmCall`
    - `runMain` (via `_BrowserHost_ExecuteAssembly`) — via `serializeWasmCall`
    - Exit hook (`_BrowserHost_ShutdownDotnet`) — skips if `isSuspensionInFlight()`

    **NOT serialized (by design):** `getManagedStackTrace` (stays synchronous, excluded from JSPI)

    **Synchronous call sites (throw-when-suspended):**
    - `releaseJsOwnedObjectByGcHandle` — marked `TODO-JSPI-THROW-WHEN-SUSPENDED`
    - `invokeJSExportSync` — marked `TODO-JSPI-THROW-WHEN-SUSPENDED`
    - `callDelegate` — marked `TODO-JSPI-THROW-WHEN-SUSPENDED`
    - These use `serializeWasmCallSync` which will `exit(1)` if called while `isSuspended === true`

    **Remaining work:**
    - The `isSuspended` flag is never set to `true` — will be set by async `BrowserHost_ExternalAssemblyProbe` (Phase 4) when it triggers a download

---

## Relevant Files

- `src/native/libs/Common/JavaScript/host/assets.ts` — `BrowserHost_ExternalAssemblyProbe`, `registerDllBytes`, `instantiateWebcilModule`, `loadedAssemblies` map
- `src/native/libs/Common/JavaScript/host/host.ts` — `runMain()`, `initializeCoreCLR()`, `_BrowserHost_ExecuteAssembly` calls
- `src/native/libs/Common/JavaScript/host/index.ts` — exports, `BrowserHost_ExternalAssemblyProbe` re-export, module initialization
- `src/native/libs/Common/JavaScript/loader/run.ts` — `createRuntime()` with all assembly download orchestration
- `src/native/libs/Common/JavaScript/loader/assets.ts` — `fetchDll()`, `instantiateMainWasm()`, `wasmMemoryPromiseController`, `nativeModulePromiseController`
- `src/native/libs/Common/JavaScript/loader/bootstrap.ts` — `validateWasmFeatures()` — add JSPI detection here
- `src/native/libs/Common/JavaScript/types/ems-ambient.ts` — type declarations for `_BrowserHost_ExecuteAssembly` etc.
- `src/native/libs/Common/JavaScript/types/exchange.ts` — `BrowserUtilsExports`, `BrowserHostExports` types
- `src/native/libs/Common/JavaScript/cross-module/index.ts` — cross-module exchange table mapping
- `src/native/libs/System.Native.Browser/utils/scheduling.ts` — **NEW**: call serializer (`serializeWasmCall`, `serializeWasmCallSync`, `isSuspensionInFlight`)
- `src/native/libs/System.Native.Browser/native/scheduling.ts` — timer tick scheduling (async + serialized)
- `src/native/libs/System.Runtime.InteropServices.JavaScript.Native/interop/invoke-cs.ts` — `bindFnSync`, `bindFnAsync`, bound function variants
- `src/native/libs/System.Runtime.InteropServices.JavaScript.Native/interop/managed-exports.ts` — `invokeJSExport`, `invokeJSExportSync`, `completeTask`, `bindAssemblyExports`, `callDelegate`
- `src/native/libs/System.Runtime.InteropServices.JavaScript.Native/interop/marshal.ts` — `allocHeapFrame()`
- `src/native/libs/System.Runtime.InteropServices.JavaScript.Native/interop/marshaled-types.ts` — `PromiseHolder` (async resolve/reject/cancel)
- `src/native/corehost/browserhost/browserhost.cpp` — native `BrowserHost_ExternalAssemblyProbe` declaration, `host_runtime_contract`
- `src/native/corehost/browserhost/CMakeLists.txt` — Emscripten link flags
- `eng/Versions.props` — `EmsdkVersion=3.1.56`

---

## Risks & Open Questions

1. **JS frames risk**: Emscripten's glue code may add JS wrapper functions around WASM imports/exports (signature adapters, assertion checks). If any JS frames exist between the promising export and suspending import, JSPI will trap. With `-sWASM_BIGINT=1` and simple function signatures, this is unlikely but needs validation. Upgrading Emscripten reduces this risk.

2. **Reentrancy** (addressed by Phase 6): When WASM is suspended via JSPI, the JS event loop resumes and any JS event handler can attempt to re-enter managed code. This is fundamentally different from nested re-entrancy (which the runtime handles) — it is *interleaved* execution on the same thread with different stacks. The call serializer (Phase 6) queues all managed-entry calls behind the pending suspension, preventing concurrent managed execution. Duplicate assembly fetches within `BrowserHost_ExternalAssemblyProbe` should be deduplicated with a pending-fetch map.

3. **TPA list**: CoreCLR's Trusted Platform Assemblies list is built from ALL assemblies at startup. If assemblies aren't downloaded yet, they're still in the TPA list. Need to verify CoreCLR doesn't validate TPA entries at init time, only when probing.

4. **`initializeCoreCLR` timing**: Currently called after `coreAssembliesPromise` resolves. With JSPI, it must still wait for core assemblies but NOT for app assemblies.

5. **Webcil format**: Some assemblies use `.wasm` extension (Webcil format) requiring `instantiateWebcilModule`. The async probe needs to handle both raw DLL and Webcil formats.

6. **Stack depth**: Long chains of assembly loading (A loads B which loads C) could create deep JSPI suspension chains. JSPI handles this (multiple suspensions after the first are transparent to the caller), but needs testing.
