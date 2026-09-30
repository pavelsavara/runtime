# Plan: `dotnet.withWorker().create()` — Run CoreCLR WASM on a Dedicated Web Worker

## Goal

Enable the .NET CoreCLR WASM runtime to start on a dedicated web worker, controlled from the main/UI thread via the existing builder pattern:

```javascript
import { dotnet } from './_framework/dotnet.js'

const runtime = await dotnet
    .withWorker({ timeoutMs: 30000 })
    .withConfig({ mainAssemblyName: "MyApp" })
    .create();

const exitCode = await runtime.runMainAndExit();
```

The main thread gets a **full `RuntimeAPI`** backed by SharedArrayBuffer — synchronous heap access, JSImport/JSExport, everything works. Synchronous interop calls (JSExport) use spin-wait that also services incoming JSImport requests. Async methods (`runMain`, `runMainAndExit`) use `Atomics.waitAsync` for non-blocking operation.

**Requirements**: COOP/COEP headers, SharedArrayBuffer support in browser. Feature detection at `withWorker()` time — throws immediately if `crossOriginIsolated === false` or `SharedArrayBuffer` / `Atomics.waitAsync` are unavailable.

## Architecture

### Build Strategy: Fully Single-Threaded + Post-Link Patch

The build is **fully single-threaded** from both the VM and Emscripten perspective. No `-pthread`, no `-sSHARED_MEMORY=1`, no threaded sysroot, no MT libraries. The standard non-threaded Emscripten sysroot is used as-is.

SharedArrayBuffer support is achieved via two post-build mechanisms:

1. **`-sIMPORTED_MEMORY=1`**: The wasm binary imports its linear memory from JS instead of defining it internally. In normal (non-worker) mode, the Emscripten JS glue creates a regular `WebAssembly.Memory`. In worker mode, `instantiateWasm` creates a `WebAssembly.Memory({shared: true})` backed by `SharedArrayBuffer`.

2. **Patch the `shared` bit on `dotnet.native.wasm`**: The wasm binary's memory import descriptor says `shared: false` (because the build is ST). A post-link build step flips the single bit in the wasm binary header to `shared: true`. This is required because `WebAssembly.instantiate` validates that the imported memory's shared flag matches the wasm module's declaration. The patch is a single byte change at a known offset in the memory import section.

This approach has **zero impact on non-worker builds**. The standard Emscripten JS glue creates a normal `ArrayBuffer`-backed memory and everything works as before. The `shared: true` bit in the wasm binary is harmless — the spec allows instantiating a `shared`-declared import with a non-shared memory.

### Two Phases

**Phase A — Startup (postMessage-based)**:
`HostBuilder.create()` overrides `instantiateWasm` to create `WebAssembly.Memory({shared: true})`, spawns worker, sends memory + config via `MessageChannel`, waits for "ready".

**Phase B — Runtime (SharedArrayBuffer-based)**:
Main thread has direct access to WASM linear memory (it created the memory, so it already has the SharedArrayBuffer). JSImport calls go through shared memory signaling. JSExport calls use spin-wait on main thread (servicing incoming JSImport during the wait). Async methods use `Atomics.waitAsync`. `dotnet.runtime.js` runs on main thread with a different init path.

### High-Level Flow

```
Main Thread (UI)                          Dedicated Worker
─────────────────                         ────────────────
dotnet.js loaded as ES module
  ↓
user calls .withWorker({ timeoutMs })
  → feature-detects SharedArrayBuffer,
    Atomics.waitAsync, crossOriginIsolated
  → sets flag + options in HostBuilder
  ↓
user calls .create()
  → reads memory params from config
    (initial/maximum from boot config)
  → overrides instantiateWasm to create
    WebAssembly.Memory({shared: true})
    backed by SharedArrayBuffer
  → creates TypedArray views immediately
  → creates new Worker("dotnet.js", {type:"module"})
  → creates MessageChannel
  → posts config + wasmMemory + port2
    to worker (memory.buffer is SAB,
    transferable via postMessage)
  → starts timeout timer (timeoutMs)
                                          dotnet.js loaded in worker context
                                            ↓
                                          detects: ENVIRONMENT_IS_WEB_WORKER
                                            ↓
                                          awaits init message (gated Promise)
                                            ↓
                                          receives init message with:
                                            config + wasmMemory
                                            ↓
                                          overrides instantiateWasm to use
                                            the pre-created shared memory
                                          bootstraps runtime:
                                            dotnetInitializeModule()
                                            initPolyfillsAsync()
                                            HostBuilder.withConfig(config)
                                            .create() → real RuntimeAPI
                                            ↓
                                          Emscripten uses provided shared
                                            WebAssembly.Memory (no new alloc)
                                            ↓
                                          allocates signaling buffer (new SAB)
                                          allocates ring buffer frame slots
                                            ↓
                                          posts "ready" + signalBuffer
                                            + runtimeBuildInfo
  ↓
  receives signalBuffer
  clears timeout timer
  loads dotnet.runtime.js on main thread
    with shared-memory init path
  registers setModuleImports locally
  starts JSImport listener (Atomics.waitAsync)
  resolves RuntimeAPI (full, not proxy)
                                          Worker event loop running,
                                            listening for JSExport requests
                                            and commands on signaling buffer

═══════════ Phase B: SharedArrayBuffer runtime ═══════════

user calls runtime.runMainAndExit() [async]
  → writes command to signaling buffer
  → Atomics.notify → wakes worker
  → Atomics.waitAsync on command result
  → event loop stays alive for JSImport
                                          wakes from Atomics.wait
                                          reads command
                                          calls real runtime.runMain()
                                          (may trigger JSImport calls…)
                                          writes result to command slot
                                          Atomics.store + Atomics.notify
  ← waitAsync resolves, reads result
  returns exit code

user calls sync JSExport method
  → writes request to JSExport ring
  → Atomics.notify → wakes worker
  → spin-waits on result, servicing
    any incoming JSImport requests
                                          wakes, reads JSExport request
                                          executes C# method
                                          (may trigger JSImport calls…)
                                          writes result + Atomics.notify
  ← spin-wait sees result
  ← returns value

C# calls [JSImport] on worker
                                          writes {promise_id, fn_handle,
                                            args_ptr} to JSImport ring
                                          Atomics.store(PENDING)
                                          Atomics.wait on that slot
  ↓
  Atomics.waitAsync resolves (or
    spin-wait loop detects PENDING)
  refreshes TypedArray views if needed
  reads args from shared memory
  calls JS function on main thread
  writes result to shared memory
  Atomics.store(DONE) + Atomics.notify
  re-arms Atomics.waitAsync
                                          wakes, matches promise_id
                                          reads result, returns to C#
```

### Self-Detection in dotnet.js

The current `dotnet.ts` entry point runs eagerly on import. We restructure it to gate on a Promise when running inside a dedicated worker:

```typescript
import { ENVIRONMENT_IS_WEB_WORKER, ENVIRONMENT_IS_SIDECAR } from "./per-module";

dotnetInitializeModule();
await initPolyfillsAsync();

let _dotnet: DotnetHostBuilder | undefined;
if (ENVIRONMENT_IS_WEB_WORKER && !ENVIRONMENT_IS_SIDECAR) {
    // Dedicated worker spawned by withWorker(): wait for init message.
    // ENVIRONMENT_IS_SIDECAR workers (Emscripten pthreads) continue normal startup.
    const initMsg = await waitForInitMessage();
    await bootstrapWorkerRuntime(initMsg);
    // dotnet export is undefined in worker context
} else {
    _dotnet = new HostBuilder() as DotnetHostBuilder;
    _dotnet.withConfig(/*! dotnetBootConfig */{});
}
export const dotnet = _dotnet;
export { exit };
```

`waitForInitMessage()` returns a Promise that resolves when `self.onmessage` receives the `"init"` message. `bootstrapWorkerRuntime` receives the pre-created shared `WebAssembly.Memory` from the main thread and overrides `instantiateWasm` to use it during Emscripten initialization. The top-level `await` blocks module evaluation until the worker is fully bootstrapped.

`ENVIRONMENT_IS_SIDECAR` workers (Mono/Emscripten pthreads that set `globalThis.dotnetSidecar`) continue normal startup with a `HostBuilder`, since they are not our withWorker-spawned workers.

### Startup Protocol (MessageChannel, Phase A only)

Used only during startup. After SharedArrayBuffer is established, all communication moves to shared memory. The main thread enforces a configurable timeout (default 30s) — if no `"ready"` message arrives within `timeoutMs`, the worker is terminated and `create()` rejects.

**Main → Worker messages:**

| type | payload | description |
|------|---------|-------------|
| `"init"` | `{ config: LoaderConfig, wasmMemory: WebAssembly.Memory }` | Bootstrap the runtime with this config and pre-created shared memory |

**Worker → Main messages:**

| type | payload | description |
|------|---------|-------------|
| `"ready"` | `{ runtimeBuildInfo, signalBuffer: SharedArrayBuffer }` | Runtime initialized, here's the signaling buffer |
| `"error"` | `{ message, stack }` | Runtime failed to initialize |

Note: The WASM linear memory (`SharedArrayBuffer`) is NOT sent in the `"ready"` message — the main thread already has it because it created the `WebAssembly.Memory` upfront. Only the signaling buffer (a separate small `SharedArrayBuffer` allocated by the worker) needs to be sent back.

### Shared Memory Layout

After startup, two shared buffers are available:

**1. WASM Linear Memory** (`WebAssembly.Memory` with `shared: true`):
- **Created in `instantiateWasm`** on the main thread (or worker, depending on who boots first): `new WebAssembly.Memory({initial, maximum, shared: true})`
- The wasm binary is built with `-sIMPORTED_MEMORY=1` and has its `shared` bit patched post-link, so `WebAssembly.instantiate` accepts the shared memory import
- Memory params (`initial`, `maximum`) read from boot config (matching `-sINITIAL_MEMORY` / `-sMAXIMUM_MEMORY`)
- Sent to worker via init message; worker's `instantiateWasm` uses the pre-created shared memory
- Main thread creates TypedArray views immediately (no need to wait for worker)
- Contains all WASM heap, stack, managed objects
- JSMarshalerArguments frames are allocated here (by worker via `stackAlloc` or ring buffer)
- **Views must be refreshed after memory growth** (see Memory View Refresh below)

**2. Signaling Buffer** (separate `SharedArrayBuffer`, ~4KB):
- Fixed-layout control structure for cross-thread coordination
- Allocated by worker before posting "ready"
- Contains ring buffers for JSImport and JSExport requests, each with promise IDs for re-entrant matching

```
Signaling Buffer Layout (Int32Array view):
Offset  Name                      Description
──────  ────                      ───────────

Control:
[0]     version                   Layout version (for forward compatibility)
[1]     memory_generation         Incremented by worker after WebAssembly.Memory.grow()
                                  Main thread checks after every wakeup/proxied call

Unified Wakeup:
[2]     wakeup_main               Monotonic counter — worker increments + Atomics.notify
                                  to wake main thread (commands, JSImport, JSExport done)
[3]     wakeup_worker             Monotonic counter — main increments + Atomics.notify
                                  to wake worker (commands, JSExport requests)

Commands:
[4]     command_signal            Command signaling (IDLE=0, PENDING=1, DONE=2, ERROR=3)
[5]     command_id                Command identifier (RUN_MAIN=1, EXIT=2, ...)
[6]     command_result            Command result value
[7]     command_promise_id        Promise ID for async command completion

JSImport Ring (worker writes → main reads, N entries):
[8]     jsimport_head             Next slot to write (worker increments atomically)
[9]     jsimport_tail             Next slot to read (main increments atomically)
[10..10+N*4-1]                    N entries × 4 Int32 each:
          [+0] signal               Per-entry signal (IDLE/PENDING/DONE/ERROR)
          [+1] promise_id           Identifies which Atomics.wait to wake
          [+2] fn_handle            JSImport function handle
          [+3] args_ptr             Pointer to JSMarshalerArguments in WASM memory

JSExport Ring (main writes → worker reads, M entries):
[10+N*4]   jsexport_head          Next slot to write (main increments atomically)
[10+N*4+1] jsexport_tail          Next slot to read (worker increments atomically)
[10+N*4+2..+M*4-1]               M entries × 4 Int32 each:
          [+0] signal               Per-entry signal (IDLE/PENDING/DONE/ERROR)
          [+1] promise_id           Identifies which promise to resolve on completion
          [+2] method_handle        JSExport method handle
          [+3] args_ptr             Pointer to JSMarshalerArguments in WASM memory

Frame Pointers:
[...]   ring_buffer_frame_ptrs    Pre-allocated arg frame pointers for main→worker calls
```

**Ring buffer semantics**: Head advances on write, tail advances on read. Both use `Atomics.add` for lock-free concurrent access. Buffer is circular with capacity N (or M). If `head - tail >= capacity`, the ring is full — fall back to proxied `_malloc`.

**Promise IDs**: Each cross-thread call is assigned a monotonically increasing `promise_id`. The responder writes the result and echoes the `promise_id` back. The requester matches the returned `promise_id` to resolve the correct Promise (or unblock the correct `Atomics.wait`). This enables re-entrant calls — a JSImport handler on main can trigger a JSExport that goes back to worker, and the worker can serve it while its outer JSImport call is still waiting.

### JSImport Flow (C# on worker → JS on main thread)

C# code on the worker calls `[JSImport]` which must execute a JS function on the main thread. Supports re-entrancy via per-entry promise IDs.

1. **Worker**: C# triggers JSImport → marshals args into `JSMarshalerArguments` on WASM stack (shared memory)
2. **Worker**: Allocates next slot from JSImport ring (`Atomics.add(jsimport_head, 1) % N`)
3. **Worker**: Writes `{promise_id, fn_handle, args_ptr}` to that ring entry
4. **Worker**: `Atomics.store(entry.signal, PENDING)` + `Atomics.notify(entry.signal)`
5. **Worker**: `Atomics.wait(entry.signal, PENDING)` — blocks until main thread sets DONE or ERROR
6. **Main**: `Atomics.waitAsync` on the ring's head signal resolves (or spin-wait loop during synchronous JSExport detects PENDING entry)
7. **Main**: Refreshes TypedArray views if `memory_generation` changed
8. **Main**: Reads `{promise_id, fn_handle, args_ptr}` from the ring entry
9. **Main**: Reads arg values from shared WASM memory via TypedArray views
10. **Main**: Calls actual JS function from `importedModules` map
11. **Main**: Writes result back to the same `JSMarshalerArguments` slots in shared memory
12. **Main**: `Atomics.store(entry.signal, DONE)` + `Atomics.notify(entry.signal)`
13. **Main**: Re-arms `Atomics.waitAsync` on the next ring position
14. **Worker**: Wakes from wait, matches `promise_id`, reads result, returns to C#

### JSExport Flow (JS on main thread → C# on worker)

Main thread calls a C# `[JSExport]` method. Two variants: synchronous (spin-wait) and async (Promise-based).

#### Synchronous JSExport (default for non-async C# methods)

1. **Main**: User calls exported C# method (via `getAssemblyExports()`)
2. **Main**: Allocates arg frame from ring buffer (pre-allocated pointers)
3. **Main**: Marshals JS args into shared WASM memory at that frame pointer
4. **Main**: Allocates next slot from JSExport ring, assigns `promise_id`
5. **Main**: Writes `{promise_id, method_handle, args_ptr}` to ring entry
6. **Main**: `Atomics.store(entry.signal, PENDING)` + `Atomics.notify(entry.signal)`
7. **Main**: **Spin-waits with JSImport servicing**:
   ```
   while (Atomics.load(entry.signal) === PENDING) {
       serviceJSImportQueue();      // handle any incoming JSImport calls
       refreshViewsIfNeeded();      // check memory_generation
   }
   ```
8. **Worker**: Wakes from `Atomics.wait`, reads request from JSExport ring
9. **Worker**: Calls C# method — may trigger nested JSImport calls back to main
10. **Worker**: Writes result to shared memory, `Atomics.store(entry.signal, DONE)` + `Atomics.notify(entry.signal)`
11. **Main**: Spin-wait exits, refreshes views, reads result, returns to caller

The spin-wait loop services incoming JSImport requests, preventing deadlocks when C# calls back into JS during execution.

#### Async JSExport (for async C# methods or explicit opt-in)

Same as above through step 6, then:

7. **Main**: Returns `Promise` to caller, uses `Atomics.waitAsync(entry.signal, PENDING)` for notification
8–10. Same as synchronous
11. **Main**: `waitAsync` resolves → refreshes views → reads result → resolves Promise

Async variant keeps the main thread event loop alive. JSImport calls are handled normally via `Atomics.waitAsync` listeners.

### Re-Entrancy Model

Re-entrancy is fully supported through the ring buffer + promise ID design:

```
Main Thread                               Worker
───────────                               ──────
calls JSExport A (promise_id=1)
  spin-waits, servicing JSImport...
                                          executes C# method A
                                          calls [JSImport] B (promise_id=10)
                                          Atomics.wait on JSImport ring entry
  ↓ spin-wait detects JSImport entry
  handles JSImport B
    (could trigger JSExport C, promise_id=2)
    spin-waits for C, servicing JSImport...
                                          wakes from JSImport B wait
                                          (still inside method A)
                                          sees JSExport C request
                                          executes C
                                          returns C result
  ← C result arrives, inner spin-wait exits
  writes JSImport B result
  Atomics.notify
                                          JSImport B returns
                                          method A continues/finishes
                                          writes JSExport A result
  ← A result arrives, outer spin-wait exits
  returns A result to caller
```

Each in-flight call has its own ring buffer entry and promise ID. The spin-wait loop on main thread services all pending JSImport entries, regardless of nesting depth.

### Memory View Refresh

When `WebAssembly.Memory.grow()` is called on the worker, all existing `TypedArray` views over the `SharedArrayBuffer` become stale (their `byteLength` won't reflect the new size, and they may be detached).

**Worker side**: After `Memory.grow()`, the worker increments `Atomics.add(signalBuffer, MEMORY_GENERATION_OFFSET, 1)`.

**Main thread side**: Checks `memory_generation` at these points:
- After every `Atomics.waitAsync` wakeup (JSImport listener)
- On each iteration of a spin-wait loop
- After every proxied `_malloc`/`_free` call
- After receiving any `postMessage` from worker

```typescript
let knownGeneration = 0;
function refreshViewsIfNeeded(): void {
    const current = Atomics.load(signalBuffer, MEMORY_GENERATION_OFFSET);
    if (current !== knownGeneration) {
        knownGeneration = current;
        // Re-create all TypedArray views on the SharedArrayBuffer
        wasmMemoryViews = createViews(wasmSharedBuffer);
    }
}
```

### Ring Buffer for Cross-Thread Arg Frames

The main thread can't call `stackAlloc` or `_malloc` (WASM functions on worker). For JSExport calls initiated from the main thread, we need pre-allocated memory.

**Ring buffer** of N frame slots (e.g., 16), each pointing to a pre-allocated region in WASM heap:
- Worker allocates N × `JavaScriptMarshalerArgSize * MAX_ARGS` bytes via `_malloc` during startup
- Pointers stored in signaling buffer
- Main thread picks next free slot via atomic counter
- Each slot is large enough for a typical call (e.g., 8 args × 32 bytes = 256 bytes)

**Fallback**: If ring buffer is exhausted (all slots in-flight), fall back to proxied `_malloc`:
- Main thread writes malloc request to a signaling slot
- Worker wakes, calls `_malloc`, writes pointer back
- Main thread spin-waits for the pointer (with JSImport servicing)

### dotnet.runtime.js on Main Thread

The same `dotnet.runtime.js` module loads on the main thread with a different init path:

**Normal init (worker)**: `dotnetInitializeModule(internals)` — wires up `getAssemblyExports`, `setModuleImports`, JSImport/JSExport bindings, all backed by local WASM functions.

**Shared-memory init (main thread)**: `dotnetInitializeModule(internals)` detects shared-memory mode and:
- `setModuleImports(name, imports)` → stores in local `importedModules` map (same as today)
- `getAssemblyExports(name)` → binds JSExport functions that use spin-wait + shared memory (with JSImport servicing) instead of `invokeJSExport`
- `setHeap*` / `getHeap*` → creates TypedArray views on SharedArrayBuffer (refreshed via `memory_generation` check)
- `stackAlloc` / `stackRestore` → not available (uses ring buffer instead)
- `_malloc` / `_free` → proxied to worker via signaling (with JSImport servicing during spin-wait)
- JSImport handler → listens on signaling buffer, calls local JS functions, writes results

### RuntimeAPI Shape on Main Thread

After `create()` resolves, the main thread gets a **real RuntimeAPI** (not a proxy):

**Works directly via shared memory:**
- All `MemoryAPIType` methods (`setHeap*`, `getHeap*`, `localHeapView*`) — TypedArray on SharedArrayBuffer (refreshed on access if `memory_generation` changed)
- `getConfig()` — local copy
- `runtimeBuildInfo` — received during startup

**Works via shared memory signaling (async — keeps event loop alive):**
- `runMain()` / `runMainAndExit()` — command sent via signaling, `Atomics.waitAsync` for result (event loop stays alive for JSImport calls during execution)
- `exit()` — command sent via signaling

**Works via shared memory signaling (sync spin-wait with JSImport servicing):**
- `getAssemblyExports()` — returns object with methods that use spin-wait JSExport path (spin-wait services incoming JSImport during wait)
- `setModuleImports()` — stores locally, JS functions called when worker triggers JSImport

**Not supported initially:**
- `DiagnosticsAPIType` methods
- `invokeLibraryInitializers`
- `Module` property (Emscripten module lives on worker)

## Files to Change

### New files

| File | Purpose |
|------|---------|
| `src/native/libs/Common/JavaScript/loader/worker-proxy.ts` | Main-thread side: spawns worker, sets up shared memory, returns full RuntimeAPI |
| `src/native/libs/Common/JavaScript/loader/worker-runtime.ts` | Worker side: bootstraps runtime from init message, runs signaling event loop |
| `src/native/libs/Common/JavaScript/loader/shared-signaling.ts` | Shared constants, signaling buffer layout, ring buffer helpers, wait/notify/spin-wait-with-service helpers |

These files are imported from existing entry points (`host-builder.ts` → `worker-proxy.ts`, `dotnet.ts` → `worker-runtime.ts`). They are tree-shaken out of non-worker builds. No new rollup entry points or rollup config changes needed.

### Modified files (planned → actual)

| File | Change | Status |
|------|--------|--------|
| `src/native/libs/Common/JavaScript/types/public-api.ts` | Add `withWorker(options?)` to `DotnetHostBuilder` interface, add `WorkerOptions` type | ✅ Done |
| `src/native/libs/Common/JavaScript/loader/host-builder.ts` | Add `withWorker()` method, branch `create()` for worker mode | ✅ Done |
| `src/native/libs/Common/JavaScript/loader/dotnet.ts` | Restructure: gate on init-message Promise when `ENVIRONMENT_IS_WEB_WORKER` | ✅ Done |
| `src/native/libs/Common/JavaScript/loader/dotnet.d.ts` | Add `withWorker()` to declarations, add `WorkerOptions` type | ✅ Done |
| `src/native/libs/Common/JavaScript/loader/bootstrap.ts` | Feature detection for SharedArrayBuffer, crossOriginIsolated, Atomics.waitAsync | ✅ Done (not originally in plan) |
| `src/native/libs/Common/JavaScript/loader/config.ts` | Merge `workerOptions` in `mergeLoaderConfig()` | ✅ Done (not originally in plan) |
| `src/native/libs/Common/JavaScript/loader/assets.ts` | Webcil loading with shared memory support | ✅ Done (not originally in plan) |
| `src/native/libs/Common/JavaScript/host/assets.ts` | `instantiateWebcilModule()` accepts shared memory | ✅ Done (not originally in plan) |
| `src/native/libs/Common/JavaScript/cross-module/index.ts` | Cross-module wiring for worker bootstrap | ✅ Done (not originally in plan) |
| `src/native/libs/Common/JavaScript/types/ems-ambient.ts` | Emscripten ambient type additions | ✅ Done (not originally in plan) |
| `src/native/libs/Common/JavaScript/types/exchange.ts` | Exchange type additions | ✅ Done (not originally in plan) |
| `src/native/libs/Common/JavaScript/CMakeLists.txt` | Add new .ts files to ROLLUP_TS_SOURCES | ✅ Done |
| `src/native/libs/System.Runtime.InteropServices.JavaScript.Native/interop/utils.ts` | Interop utils changes | ✅ Done |
| `src/native/libs/System.Runtime.InteropServices.JavaScript.Native/interop/web-socket.ts` | WebSocket changes | ✅ Done |
| `src/native/libs/System.Native.Browser/utils/index.ts` | Browser utils changes | ✅ Done |
| `src/native/libs/System.Native.Browser/utils/strings.ts` | String utils changes | ✅ Done |
| `eng/native.wasm.targets` | `PatchWasmSharedMemory` MSBuild target | ✅ Done (not originally in plan) |
| `src/tasks/WasmBuildTasks/WasmMemorySetShared.cs` | Post-link wasm shared-bit patcher | ✅ Done (new file, not originally in plan) |
| `src/tasks/Microsoft.NET.WebAssembly.Webcil/WebcilWasmWrapper.cs` | Shared memory variant of webcil wasm prefix | ✅ Done (not originally in plan) |
| `src/tasks/Microsoft.NET.WebAssembly.Webcil/WebcilConverter.cs` | Pass shared memory flag to wrapper | ✅ Done |
| `src/tasks/Microsoft.NET.Sdk.WebAssembly.Pack.Tasks/ConvertDllsToWebCil.cs` | Pass shared memory flag | ✅ Done |
| `src/mono/nuget/.../Microsoft.NET.Sdk.WebAssembly.Browser.targets` | Wire `WasmEnableSharedMemory` property | ✅ Done |
| `src/mono/sample/wasm/browser/wwwroot/main.js` | Worker variant of the sample | ✅ Done |
| `src/mono/sample/wasm/browser/Wasm.Browser.Sample.csproj` | Enable shared memory | ✅ Done |
| `src/mono/sample/wasm/Directory.Build.targets` | Build targets tweak | ✅ Done |
| `src/native/libs/System.Runtime.InteropServices.JavaScript.Native/interop/index.ts` | Shared-memory init path for main thread | ❌ Not done yet |

### NOT changed (explicitly)

- `rollup.config.js` — no new entry point; new .ts files are imported from existing entry points
- Mono-specific files — CoreCLR only
- Sidecar/pthread infrastructure — independent concept, not relevant

## Implementation Steps

### Phase 1: API surface
- [x] Add `withWorker(options?: WorkerOptions): DotnetHostBuilder` to the public interface in `types/public-api.ts`
- [x] Add `WorkerOptions` type: `{ timeoutMs?: number }` (default 30000)
- [x] Add `withWorker()` method to `HostBuilder` class in `host-builder.ts`
  - Feature-detect `crossOriginIsolated`, `SharedArrayBuffer`, `Atomics.waitAsync` — throw immediately if missing (in `bootstrap.ts`)
- [x] Update `dotnet.d.ts` declarations

### Phase 2: Shared memory infrastructure
- [x] Create `shared-signaling.ts`:
  - Signaling buffer layout constants and offsets
  - Signal states enum (IDLE, PENDING, DONE, ERROR)
  - Ring buffer helpers (allocate slot, advance head/tail)
  - `spinWaitWithJSImportService()` — stub for Phase 4 spin-wait
  - `MemoryViewManager` — tracks memory growth via `memory_generation` counter
  - Promise ID generation (monotonically increasing counter)
- [x] Add post-link build step to patch the `shared` bit in `dotnet.native.wasm`
  - `WasmMemorySetShared.cs` MSBuild task scans wasm binary for memory import/definition sections
  - Sets bit 0x02 (shared flag) on the limits flags byte
  - Handles both imported memory and locally-defined/exported memory
  - Wired via `PatchWasmSharedMemory` target in `eng/native.wasm.targets`
  - Webcil wasm wrappers also patched via `WebcilWasmWrapper.cs` shared prefix variant

### Phase 3: Worker-side runtime bootstrap
- [x] Create `worker-runtime.ts`:
  - `waitForInitMessage()` — returns Promise gated on `self.onmessage`, receives `{ config, wasmMemory }`
  - `bootstrapWorkerRuntime(initMsg)`:
    - Merges config from main thread, creates HostBuilder, calls `create()`
    - Gets shared `wasmMemory` via `dotnetNativeBrowserExports.getWasmMemory()`
    - Allocates signaling buffer (`new SharedArrayBuffer`) with ring buffer layout
    - Allocates 16 ring buffer frame slots (256 bytes each) via WASM heap
    - Memory.grow hook: commented out / WIP
    - Posts "ready" + signalBuffer + runtimeBuildInfo back to main
    - Enters `runWorkerEventLoop()` — async loop using `Atomics.waitAsync`
  - `runWorkerEventLoop()`: processes commands (RUN_MAIN, EXIT) and JSExport ring (stubbed)
- [x] Modify `dotnet.ts` — restructure: gate on init-message Promise when `ENVIRONMENT_IS_WEB_WORKER && !ENVIRONMENT_IS_SIDECAR`

### Phase 4: Main-thread worker proxy (minimal — runMain/runMainAndExit only)
- [x] Create `worker-proxy.ts`:
  - Hardcoded memory size defaults matching CMakeLists (`initial: 2048 pages = 128MB`, `maximum: 32768 pages = 2GB`)
    - TODO: Make configurable via `LoaderConfig` or boot config from build system
  - Overrides `instantiateWasm` to create `WebAssembly.Memory({initial, maximum, shared: true})` — main thread owns the memory
  - Creates TypedArray views on the SharedArrayBuffer immediately
  - Spawns worker via `new Worker(import.meta.url, {type: "module"})`, posts init message
  - Starts timeout timer (`timeoutMs`), terminates worker on timeout
  - Receives signalBuffer from worker "ready" message
  - Uses `Atomics.waitAsync` on `OFFSET_WAKEUP_MAIN` for non-blocking notification
  - Builds RuntimeAPI with `runMain`/`runMainAndExit` via command signaling (Atomics.waitAsync for result)
  - Returns RuntimeAPI
  - Full JSImport/JSExport interop deferred to future phase
- [x] `host-builder.ts` `create()` branches into `createWorkerProxy()` when `loaderConfig.workerOptions` is set
- [ ] JSExport binding: main thread reuses pre-allocated ring buffer frame slots (from signaling buffer) instead of `stackAlloc`/`stackRestore`. Unused slots from ring buffer; no round-trip to worker for common cases.

**GC handle note**: The existing `gc-handles.ts` handle table is sufficient for cross-thread use. GCHandles of Task may need pre-allocation in a future phase. For `runMain()` we can ignore this.

**Memory size TODO**: Currently hardcoded to match the CMake flags. Future work: expose `initialMemory`/`maximumMemory` in `LoaderConfig` so the build system can flow these values.

### Phase 5: Sample app
- [x] Modify `src/mono/sample/wasm/browser/wwwroot/main.js` to demonstrate `withWorker()` usage
- [x] Add `<WasmEnableSharedMemory>true</WasmEnableSharedMemory>` to `Wasm.Browser.Sample.csproj`

### Phase 6: Verify
- [ ] Build JS bundles via rollup
- [ ] Run sample in browser with COOP/COEP headers
- [ ] Confirm managed hello world runs on worker
- [ ] Confirm console output appears in worker console
- [ ] Confirm SharedArrayBuffer is accessible from main thread
- [ ] Run sample in Node.js (console-node) to verify non-worker path unbroken

## Promise Repository (Startup Phase Only)

Used only during the `create()` startup handshake (Phase A). After SharedArrayBuffer is established, all communication moves to shared memory signaling.

### Structure

```typescript
interface PendingCall {
    resolve: (value: any) => void;
    reject: (reason: any) => void;
}

interface PendingPromises {
    nextId: number;
    pending: Map<number, PendingCall>;
}

function createPendingPromises(): PendingPromises {
    return { nextId: 1, pending: new Map() };
}

function createPending(state: PendingPromises): { id: number; promise: Promise<any> } {
    const id = state.nextId++;
    const promise = new Promise<any>((resolve, reject) => {
        state.pending.set(id, { resolve, reject });
    });
    return { id, promise };
}

function resolvePending(state: PendingPromises, id: number, value: any): void {
    const entry = state.pending.get(id);
    if (entry) {
        state.pending.delete(id);
        entry.resolve(value);
    }
}

function rejectPending(state: PendingPromises, id: number, reason: any): void {
    const entry = state.pending.get(id);
    if (entry) {
        state.pending.delete(id);
        entry.reject(reason);
    }
}

function rejectAllPending(state: PendingPromises, reason: any): void {
    for (const [, entry] of state.pending) {
        entry.reject(reason);
    }
    state.pending.clear();
}
```

### Scope

- Main thread creates one PendingPromises for the `create()` call
- Worker posts `"ready"` or `"error"` → resolves/rejects the pending create promise
- After `create()` resolves, PendingPromises is no longer used
- All subsequent communication goes through shared memory signaling

## Design Decisions

| Decision | Rationale |
|----------|-----------|
| Require SharedArrayBuffer + COOP/COEP | Enables direct shared memory access, synchronous JSImport/JSExport, full RuntimeAPI on main thread |
| Main thread creates shared `WebAssembly.Memory` | `instantiateWasm` creates `new WebAssembly.Memory({initial, maximum, shared: true})` in worker mode. Main thread has the SharedArrayBuffer immediately — no round-trip wait. Worker's `instantiateWasm` uses the pre-created shared memory. |
| Fully single-threaded build + post-link shared-bit patch | Build uses standard ST Emscripten sysroot — no `-pthread`, no `-sSHARED_MEMORY`, no MT libraries, no threading stubs. The `shared` bit in `dotnet.native.wasm`'s memory import is patched post-link. This is safe because (a) non-atomic instructions produce correct results when there's no concurrent access, and (b) the WASM runtime engine doesn't enforce atomics on instructions — only `wasm-ld` checked at link time, which we bypass. Zero impact on non-worker builds. |
| `-sIMPORTED_MEMORY=1` + `instantiateWasm` override | Makes wasm binary import memory from JS. Our `instantiateWasm` callback creates the memory — normal `ArrayBuffer` for non-worker, `SharedArrayBuffer` for worker mode. Single build artifact serves both modes. |
| Single `dotnet.js` bundle, self-detecting | Avoids new rollup entry point, simpler deployment. Worker detects `ENVIRONMENT_IS_WEB_WORKER` and gates on init message. |
| `MessageChannel` for startup only | Phase A handshake only. Phase B uses shared memory exclusively. |
| Configurable timeout for startup | `withWorker({ timeoutMs })` — rejects `create()` if worker doesn't respond. Default 30s. Prevents silent hangs. |
| Feature-detect at `withWorker()` time | Throws immediately if `crossOriginIsolated`, `SharedArrayBuffer`, or `Atomics.waitAsync` are missing. Clear error messages. |
| Worker fetches its own resources | Workers have `fetch()`, simpler than transferring ArrayBuffers from main thread |
| `dotnet.runtime.js` on main thread | Same module, different init path. Reuses marshaling code, JSImport lookup, type converters. |
| Spin-wait with JSImport servicing | `Atomics.wait` not available on main thread. Spin-wait services incoming JSImport requests, preventing deadlocks when C# calls back into JS. |
| Async `runMain`/`runMainAndExit` | Uses `Atomics.waitAsync` — event loop stays alive for JSImport calls during managed execution. |
| `Atomics.waitAsync` for JSImport on main | Non-blocking notification when worker triggers JSImport. Re-armed after each wakeup. |
| Ring buffers with promise IDs | Supports re-entrant calls (JSExport→JSImport→JSExport nesting). Each in-flight call has its own slot and promise ID. |
| Ring buffer for cross-thread arg frames | Main thread can't call `stackAlloc`/`_malloc`. Pre-allocated slots avoid round-trip for common cases. |
| Fallback to proxied `_malloc` | Ring buffer exhaustion → request worker to malloc via signaling. Rare path. Spin-wait with JSImport servicing during wait. |
| `memory_generation` counter | Worker increments after `Memory.grow()`. Main thread refreshes TypedArray views after every wakeup/proxied call. |
| `setModuleImports` stores locally | JS functions can't be serialized. Main thread holds them, worker triggers JSImport via signaling. |
| `import.meta.url` for worker URL | Worker loads same `dotnet.js`, self-detects role. See Open Questions for importmap/fingerprinting concerns. |
| Sidecar workers continue normal startup | `ENVIRONMENT_IS_SIDECAR` workers (Emscripten pthreads with `globalThis.dotnetSidecar`) skip the withWorker bootstrap and create a normal `HostBuilder`. Only non-sidecar workers enter `waitForInitMessage()`. |
| Unified wakeup slots | Instead of waiting on individual signal slots, both sides use a dedicated wakeup counter (`wakeup_main`, `wakeup_worker`). Any command write, ring buffer write, or memory growth notifies the appropriate slot. Simplifies the wait logic — single `Atomics.waitAsync` per side. |
| Async worker event loop | Worker uses `Atomics.waitAsync` + `setTimeout` for the event loop when no synchronous call is on the stack. Keeps the JS event loop alive so `runMainAndExit` Promises, microtasks, and `setTimeout` callbacks resolve normally. Falls back to `Atomics.wait` only inside synchronous JSExport dispatch. |

## Constraints & Requirements

- **Browser**: COOP/COEP headers required (`Cross-Origin-Opener-Policy: same-origin`, `Cross-Origin-Embedder-Policy: require-corp`)
- **Feature detection**: `withWorker()` checks `crossOriginIsolated`, `SharedArrayBuffer`, `Atomics.waitAsync` — throws with clear message if missing
- **Atomics.waitAsync**: Chrome 87+, Firefox 127+, Safari 16.4+. Required for non-blocking JSImport and async command handling.
- **Spin-wait on main thread**: Blocks UI during synchronous JSExport calls. Spin-wait loop services incoming JSImport to prevent deadlocks. User must accept the UI-blocking tradeoff for sync calls.

## Open Questions

| Question | Context |
|----------|---------|
| **`import.meta.url` + importmaps + fingerprinted resources** | The plan assumes `new Worker(import.meta.url, {type:"module"})` works. This needs to be validated with importmaps (which may remap the URL) and fingerprinted resource URLs (e.g., `dotnet.abc123.js`). If broken, may need to accept worker URL as a parameter. |
| **Shared-bit patch: exact byte offset** | The `shared` flag is in the memory import's `limits` field in the wasm binary. The offset depends on the import section layout (number/length of other imports). Need a robust parser that finds the memory import by name rather than hardcoding an offset. A simple wasm section walker (~50 lines) suffices. |
| **Non-shared memory with shared-declared import** | The spec says a `shared`-declared memory import can be satisfied by a non-shared `WebAssembly.Memory`. Need to validate this holds in all target engines (V8, SpiderMonkey, JavaScriptCore). If any engine rejects it, the non-worker path would need to also create shared memory (requiring COOP/COEP always) or we'd need two wasm files. |
| **Console forwarding from worker** | Worker `console.log`/`console.error` output appears in browser devtools under the worker context, not the main page. Users may want consolidated output. Options: (a) keep as-is (devtools shows worker output separately), (b) forward via `postMessage` during Phase A and/or a signaling slot during Phase B, (c) make configurable. |
| **CSP `worker-src` directive** | If the page's Content-Security-Policy has a restrictive `worker-src` directive (e.g., `worker-src 'none'`), `new Worker(import.meta.url)` will fail regardless of `import.meta.url` correctness. This is an edge case but should be documented. |

## Lessons Learned: Linking with SharedArrayBuffer (single-threaded)

### The Problem

`wasm-ld --shared-memory` requires **every** linked `.o` file to declare `atomics` and `bulk-memory` in its `target_features` custom section. Our code compiles with those flags, but Emscripten's system libraries (libc, libcompiler_rt, etc.) ship in two sysroots — non-threaded (`wasm32-emscripten/`) and threaded (`wasm32-emscripten-threads/`). The default linker picks the non-threaded variants.

### How the Linker Knows

Each `.o` file contains a binary **`target_features` custom section** listing which CPU features it was compiled with. The linker reads this from every input `.o` and `.a` member. When `--shared-memory` is active, it checks that every object has both `atomics` and `bulk-memory`. This check is in LLVM's `lld/wasm/Writer.cpp` and is a hard error — there is **no flag to suppress it** (`--no-check-features` does not exist for this).

The features section is metadata only — flipping the bit does not rewrite `i32.load` → `i32.atomic.load`. For single-threaded use (no concurrent access), non-atomic instructions produce correct results on shared memory. The WASM runtime engine doesn't enforce atomics on access — only the linker objects.

### Approaches Tried

| # | Approach | Result | Issue |
|---|----------|--------|-------|
| 1 | `-matomics` compile only | **Failed** | Linker still picks non-threaded sysroot |
| 2 | `-matomics -mbulk-memory` link options only | **Failed** | Same — sysroot selection is path-based, not flag-based |
| 3 | `-pthread` (compile + link) | **Worked** | Rejected — pulls in full pthreads/web-worker infrastructure |
| 4 | `-nostdlib` + explicit `-mt` lib names + stubs | **Worked** | Fragile: manual lib names, version-dependent, 5 dummy stubs needed, duplicate symbol conflicts with PAL |

### Key Findings

- **Sysroot selection** is driven by `-pthread` or Emscripten's `-sSHARED_MEMORY=1` flag. Simply adding `-matomics` to CFLAGS/LDFLAGS does NOT switch the sysroot.
- **`-mt` library naming**: Emscripten's threaded sysroot uses suffix conventions: `-lc-mt`, `-ldlmalloc-mt`, `-lcompiler_rt-mt`, `-lc++-mt-except`, `-lc++abi-mt-except`, `-lunwind-mt-except`.
- **Duplicate symbols**: `libc-mt.a` defines `pthread_setschedparam` which conflicts with `libcoreclrpal.a(stubs.cpp.o)`. Requires removing the PAL stub or using weak symbols.
- **Missing symbols with MT libs**: When using `-nostdlib` + MT libs, 5 emscripten threading runtime symbols are undefined: `emscripten_check_blocking_allowed`, `__emscripten_init_main_thread_js`, `_emscripten_thread_mailbox_await`, `_emscripten_thread_set_strongref`, `emscripten_num_logical_cores`. These need dummy stubs in `src/coreclr/pal/src/arch/wasm/stubs.cpp`.

### Untried Approaches

**A. `-L<threads-sysroot-path>` override**
Keep `-nostdlib` but add `-L.../wasm32-emscripten-threads/` so the linker resolves standard `-lc`, `-lcompiler_rt` etc. from the threaded sysroot. Removes the need for `-mt` suffixed library names.

**B. Custom sysroot via `embuilder`**
Pre-build Emscripten's cache with: `embuilder build sysroot --settings SHARED_MEMORY=1`. Creates a custom sysroot with all libs compiled for shared memory using default library names.

### Chosen Approach: Fully ST Build + Post-Link Patch

Rather than fighting the linker's `target_features` check at build time, we **sidestep it entirely**:

1. **Build fully single-threaded** — standard Emscripten ST sysroot, no `-pthread`, no `-sSHARED_MEMORY`, no MT libraries, no threading stubs. No changes to `configureplatform.cmake` link options. No changes to PAL stubs.
2. **Use `-sIMPORTED_MEMORY=1`** — wasm binary imports memory from JS instead of defining it internally.
3. **Override `instantiateWasm`** — in worker mode, create `WebAssembly.Memory({shared: true})`. In non-worker mode, create normal memory.
4. **Post-link: patch the `shared` bit** in `dotnet.native.wasm`'s memory import section — a single byte change so `WebAssembly.instantiate` accepts the shared memory import.

This approach:
- Has **zero build-system changes** for the native link (no sysroot switching, no MT libs, no stubs)
- Produces a **single wasm artifact** that works for both worker and non-worker modes
- Is **safe for single-threaded use** — non-atomic instructions produce correct results when there's no concurrent access
- The `shared` bit in the wasm is harmless — per spec, a `shared`-declared memory import can be satisfied with non-shared memory

## Future Work (Noted, Not Implemented)

- **Multiple workers**: Pool of workers sharing the same SharedArrayBuffer
- **JSImport with non-serializable results**: Proxy objects across threads (e.g., DOM elements)
- **True multi-threaded WASM**: Emscripten pthreads with thread-safe sysroot (would require MT build)
- **Diagnostic forwarding**: Route diagnostics APIs through signaling

## Current Progress

- [x] Research: architecture, existing code, types, cross-module exchange
- [x] Research: JSImport/JSExport marshaling memory layout
- [x] Research: SharedArrayBuffer, Atomics, synchronous call patterns
- [x] Design decisions finalized
- [x] Phase 1: API surface
- [x] Phase 2: Shared memory infrastructure
- [x] Phase 3: Worker-side runtime bootstrap
- [x] Phase 4: Main-thread worker proxy (minimal — runMain/runMainAndExit only)
- [ ] Phase 5: Sample app
- [ ] Phase 6: Verify
