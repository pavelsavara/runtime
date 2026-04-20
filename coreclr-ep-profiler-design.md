# CoreCLR Single-Threaded Sampling Profiler for Browser/WASM

## Problem Statement

CoreCLR on browser/WASM runs single-threaded (`PERFTRACING_DISABLE_THREADS`). The existing EventPipe sample profiler relies on a dedicated sampling thread that periodically suspends all other threads and walks their stacks (`ThreadSuspend::SuspendEE` → `walk_managed_stack_for_threads` → `ThreadSuspend::RestartEE`). This is impossible with a single thread.

Additionally, there is no browser DevTools profiling integration (`performance.measure`) for CoreCLR on WASM, unlike Mono which has a complete implementation.

The goal is to emit sampling events into both EventPipe and the browser profiler API from the CoreCLR interpreter, using IR instrumentation — the same strategy proven in Mono.

## Background: How Mono Solved This

Mono's single-threaded sampling profiler uses three mechanisms:

### IR Instrumentation (Transform Phase)
- `MINT_PROF_SAMPLEPOINT` opcode emitted at **backward branches** (loop targets) in `transform.c`
- Method enter/leave/exception-leave hooks emitted at method boundaries
- Controlled by `MONO_PROFILER_CALL_INSTRUMENTATION_*` flags per method

### EventPipe Sampling (ep-rt-mono-runtime-provider.c)
- `ep_rt_mono_sample_profiler_enabled()` stores `current_sampling_event` and `current_sampling_thread`, installs callbacks
- `method_enter`/`method_samplepoint`/`method_exc_leave` callbacks fire on interpreter events
- Adaptive skip counter: most invocations are a single `sample_skip_counter++` + branch (fast path)
- When interval expires: `update_sample_frequency()` recalculates `skips_per_period`, then `sample_current_thread_stack_trace()` walks the stack and calls `ep_write_sample_profile_event()`
- `ep_rt_mono_sample_profiler_disabled()` clears callbacks and state

### Browser DevTools Profiling (browser.c)
- Separate profiler using `performance.now()` (via `mono_wasm_profiler_now()`) and `performance.measure()` (via `mono_wasm_profiler_record()`)
- Maintains a shadow stack (`profiler_stack_frames[]`) tracking method enter/leave with timestamps
- Records to browser DevTools Performance tab when sampled frames complete
- Requires balanced enter/leave events with stack pointer tracking for correctness

## Current CoreCLR State

| Component | Status |
|-----------|--------|
| **INTOP_SAFEPOINT** | Exists at backward branches + method entry. Checks `g_TrapReturningThreads` for GC/abort. No profiling logic. |
| **ICorProfilerCallback** | Not supported on WASM (all stubs assert-fail in `wasm/profiler.cpp`). |
| **EventPipe sample profiler** | Disabled under `PERFTRACING_DISABLE_THREADS` — `ep_rt_sample_profiler_enabled/disabled` are no-ops. |
| **EP stack walking** | `ep_rt_coreclr_walk_managed_stack_for_thread` works with interpreter frames via `InterpreterFrame` → `InterpMethodContextFrame` chain. |
| **Browser profiler** | No implementation. No `performance.now()` or `performance.measure()` bridge. |
| **Interpreter enter/leave** | Explicitly rejected: `NO_WAY("Interpreter does not support profiling enter/leave hooks")` for `CORJIT_FLAG_PROF_ENTERLEAVE`. |

## Design

### Architecture Overview

```
┌─────────────────────────────────────────────────────────────┐
│                    CoreCLR Interpreter                       │
│                                                             │
│  INTOP_PROF_SAMPLEPOINT ──┐    INTOP_PROF_ENTER ──┐             │
│  (backward branches) │    (method entry)      │             │
│                      ▼                        ▼             │
│              SamplingProfilerCallback()                      │
│                      │                                      │
│         ┌────────────┼────────────┐                         │
│         ▼            ▼            ▼                          │
│    Skip Counter   EP Emit    Browser DevTools               │
│    (fast path)    (stack     (performance.measure            │
│                    walk)      shadow stack)                  │
└─────────────────────────────────────────────────────────────┘
```

### 1. New Interpreter Opcodes

Add three new opcodes to `src/coreclr/interpreter/inc/intops.def`:

```c
OPDEF(INTOP_PROF_SAMPLEPOINT, "samplepoint", 1, 0, 0, InterpOpNoArgs)
OPDEF(INTOP_PROF_ENTER,  "prof.enter",  2, 0, 0, InterpOpMethodDesc)
OPDEF(INTOP_PROF_LEAVE,  "prof.leave",  2, 0, 0, InterpOpMethodDesc)
OPDEF(INTOP_PROF_TAILCALL, "prof.tailcall", 3, 0, 0, InterpOpMethodDescPair)
```

**INTOP_PROF_SAMPLEPOINT** — Lightweight sampling point at backward branches. Called very frequently in loops. The handler must have a near-zero fast path (single counter increment + branch).

**INTOP_PROF_ENTER / INTOP_PROF_LEAVE** — Method boundary events for browser DevTools profiling. Only emitted when browser profiler is active. Carry a reference to the MethodDesc for the method being entered/left.

**INTOP_PROF_TAILCALL** — Emitted before tail calls. Carries references to both the caller and callee MethodDesc. Pops the caller and pushes the callee on the browser profiler shadow stack.

### 2. Opcode Emission (compiler.cpp)

#### Samplepoint Emission

Emit `INTOP_PROF_SAMPLEPOINT` at backward branches, alongside the existing `INTOP_SAFEPOINT`:

```cpp
// In InterpCompiler::EmitBranch, after existing safepoint emission:
void InterpCompiler::EmitBranch(InterpOpcode opcode, int32_t ilOffset)
{
    // ... existing code ...
    if (ilOffset < 0)
    {
        AddIns(INTOP_SAFEPOINT);
#ifdef PERFTRACING_DISABLE_THREADS
        if (s_samplingProfilerEnabled)
            AddIns(INTOP_PROF_SAMPLEPOINT);
#endif
    }
    // ... existing code ...
}
```

#### Enter/Leave Emission

Emit `INTOP_PROF_ENTER` at method prologue and `INTOP_PROF_LEAVE` before each return:

```cpp
// In method prologue (AddPrologue or equivalent):
#ifdef PERFTRACING_DISABLE_THREADS
if (s_browserProfilerEnabled)
{
    InterpInst *ins = AddIns(INTOP_PROF_ENTER);
    ins->data[0] = GetMethodDescDataItemIndex();
}
#endif

// Before each INTOP_RET* instruction:
#ifdef PERFTRACING_DISABLE_THREADS
if (s_browserProfilerEnabled)
{
    InterpInst *ins = AddIns(INTOP_PROF_LEAVE);
    ins->data[0] = GetMethodDescDataItemIndex();
}
#endif
```

#### Startup Configuration

Profiling is enabled **at application startup** via the existing `DOTNET_WasmPerformanceInstrumentation` environment variable, which Mono already uses. This eliminates the recompilation problem — all methods are compiled with instrumentation from the start.

The variable is set by the `WasmPerformanceInstrumentation` MSBuild property:
```xml
<!-- In .csproj -->
<WasmPerformanceInstrumentation>all</WasmPerformanceInstrumentation>
```
Which translates to:
```
DOTNET_WasmPerformanceInstrumentation=eventpipe,callspec=all
```

Parsed options:
- `callspec=<filter>` — method filter (default `all`)
- `interval=<ms>` — sampling interval in milliseconds (default `10`)
- `eventpipe` — EP-only mode (samplepoint sampling without balanced enter/leave)

When the variable is set, the interpreter compiler emits profiling opcodes into all methods from the start. When absent, no profiling opcodes are emitted and there is zero overhead.

#### Compile-Time Gating

The profiler instrumentation is controlled by static flags read once at startup from `DOTNET_WasmPerformanceInstrumentation`:

```cpp
// Static flags, set once at startup before managed code runs
static bool s_samplingProfilerEnabled = false;  // EP or browser profiler wants samplepoints
static bool s_browserProfilerEnabled = false;    // Browser DevTools wants enter/leave
```

Since profiling is enabled at startup before any managed code executes, all interpreter-compiled methods will include the profiling opcodes. No runtime recompilation is needed.

### 3. Execution Handlers (interpexec.cpp)

#### INTOP_PROF_SAMPLEPOINT Handler

```cpp
case INTOP_PROF_SAMPLEPOINT:
{
    // Fast path: single increment + branch — must be as cheap as possible.
    // This runs on every backward branch (every loop iteration).
    if (++s_sampleSkipCounter < s_skipsPerPeriod)
    {
        ip++;
        break;
    }

    // Slow path: time to take a sample
    pFrame->ip = ip; // Save IP for stack walking
    SamplingProfiler_OnSamplepoint(pFrame, pInterpreterFrame);
    ip++;
    break;
}
```

#### INTOP_PROF_ENTER / INTOP_PROF_LEAVE Handlers

```cpp
case INTOP_PROF_ENTER:
{
    MethodDesc *pMD = (MethodDesc*)pMethod->pDataItems[ip[1]];
    pFrame->ip = ip;
    BrowserProfiler_OnMethodEnter(pMD);
    ip += 2;
    break;
}

case INTOP_PROF_LEAVE:
{
    MethodDesc *pMD = (MethodDesc*)pMethod->pDataItems[ip[1]];
    pFrame->ip = ip;
    BrowserProfiler_OnMethodLeave(pMD);
    ip += 2;
    break;
}
```

### 4. EventPipe Sampling Integration

#### EP Runtime Provider Hooks (ep-rt-coreclr.h)

Implement the previously no-op functions for `PERFTRACING_DISABLE_THREADS`:

```cpp
// In ep-rt-coreclr.h, under #ifdef PERFTRACING_DISABLE_THREADS:

static inline void ep_rt_sample_profiler_enabled (EventPipeEvent *sampling_event)
{
    extern void ep_rt_coreclr_sample_profiler_enabled(EventPipeEvent *sampling_event);
    ep_rt_coreclr_sample_profiler_enabled(sampling_event);
}

static inline void ep_rt_sample_profiler_session_enabled (void)
{
    extern void ep_rt_coreclr_sample_profiler_session_enabled(void);
    ep_rt_coreclr_sample_profiler_session_enabled();
}

static inline void ep_rt_sample_profiler_disabled (void)
{
    extern void ep_rt_coreclr_sample_profiler_disabled(void);
    ep_rt_coreclr_sample_profiler_disabled();
}
```

#### EP Sampling Implementation (new: ep-rt-coreclr-sampling.cpp)

```cpp
// src/coreclr/vm/eventing/eventpipe/ep-rt-coreclr-sampling.cpp

#ifdef PERFTRACING_DISABLE_THREADS

static EventPipeEvent *s_currentSamplingEvent = nullptr;
static Thread *s_currentSamplingThread = nullptr;

// Adaptive sampling state
static double s_desiredSampleIntervalMs = 0.0;
static double s_lastSampleTime = 0.0;
static int s_prevSkipsPerPeriod = 1;
int s_skipsPerPeriod = 1;          // Accessed from interpexec.cpp
int s_sampleSkipCounter = 1;       // Accessed from interpexec.cpp
bool s_samplingProfilerEnabled = false;

// Called from EP when sampling is enabled
void ep_rt_coreclr_sample_profiler_enabled(EventPipeEvent *samplingEvent)
{
    s_desiredSampleIntervalMs = ((double)ep_sample_profiler_get_sampling_rate()) / 1000000.0;
    s_currentSamplingEvent = samplingEvent;
    s_currentSamplingThread = GetThread();

    s_lastSampleTime = 0.0;
    s_prevSkipsPerPeriod = 1;
    s_skipsPerPeriod = 1;
    s_sampleSkipCounter = 1;
    s_samplingProfilerEnabled = true;

    // TODO: Invalidate/recompile interpreter methods to insert samplepoints
    // For now, only methods compiled after this point will have samplepoints.
}

void ep_rt_coreclr_sample_profiler_session_enabled(void)
{
#ifdef HOST_BROWSER
    // Emit an empty sample event to satisfy dotnet-gcdump handshake.
    // See: dotnet/diagnostics EventPipeDotNetHeapDumper.cs
    EmitEmptySampleEvent();
#endif
}

void ep_rt_coreclr_sample_profiler_disabled(void)
{
    s_samplingProfilerEnabled = false;
    s_currentSamplingEvent = nullptr;
    s_currentSamplingThread = nullptr;
}

// Called from INTOP_PROF_SAMPLEPOINT slow path
void SamplingProfiler_OnSamplepoint(
    InterpMethodContextFrame *pFrame,
    InterpreterFrame *pInterpreterFrame)
{
    UpdateSampleFrequency();

    if (s_currentSamplingEvent == nullptr)
        return;

    // Walk the stack using existing CoreCLR infrastructure
    EventPipeStackContents stackContents;
    ep_stack_contents_init(&stackContents);

    Thread *pThread = GetThread();
    ep_rt_coreclr_walk_managed_stack_for_thread(pThread, &stackContents);

    if (!ep_stack_contents_is_empty(&stackContents))
    {
        uint32_t payloadData = EP_SAMPLE_PROFILER_SAMPLE_TYPE_MANAGED;
        ep_write_sample_profile_event(
            s_currentSamplingThread,
            s_currentSamplingEvent,
            s_currentSamplingThread,  // target = self (single-threaded)
            &stackContents,
            (uint8_t *)&payloadData,
            sizeof(payloadData));
    }

    ep_stack_contents_fini(&stackContents);
}

static void UpdateSampleFrequency()
{
    double now = GetHighResolutionTimestamp(); // see §6 Timing

    if (s_desiredSampleIntervalMs > 0 && s_lastSampleTime != 0.0)
    {
        double msSinceLastSample = now - s_lastSampleTime;
        double skipsPerMs = ((double)s_sampleSkipCounter) / msSinceLastSample;
        double newSkipsPerPeriod = skipsPerMs * s_desiredSampleIntervalMs;
        s_skipsPerPeriod = (int)((newSkipsPerPeriod + (double)s_sampleSkipCounter + (double)s_prevSkipsPerPeriod) / 3.0);
        s_prevSkipsPerPeriod = s_sampleSkipCounter;
    }
    else
    {
        s_skipsPerPeriod = 0;
    }
    s_lastSampleTime = now;
    s_sampleSkipCounter = 0;
}

#endif // PERFTRACING_DISABLE_THREADS
```

### 5. Browser DevTools Profiler

#### JavaScript Bridge (TypeScript side)

Implemented in `src/native/libs/System.Native.Browser/native/diagnostics.ts` alongside the existing diagnostic server WebSocket bridge:

```typescript
// In src/native/libs/System.Native.Browser/native/diagnostics.ts

export function SystemJS_GetProfilerNow(): number {
    return globalThis.performance.now();
}

export function SystemJS_BrowserProfilerMeasure(methodName: CharPtr, start: number): void {
    const name = Module.UTF8ToString(methodName);
    globalThis.performance.measure(name, { start });
}
```

This is a CoreCLR-dedicated implementation. The Mono equivalents (`mono_wasm_profiler_now`, `mono_wasm_profiler_record`) in `src/mono/browser/runtime/profiler.ts` remain separate.

#### Native Bridge (C/C++ side)

```cpp
// src/coreclr/vm/wasm/browserprofiler.cpp (new file)
#ifdef HOST_BROWSER

extern "C" double SystemJS_GetProfilerNow();
extern "C" void SystemJS_BrowserProfilerMeasure(const char *methodName, double start);

static constexpr int MAX_STACK_DEPTH = 600;

struct ProfilerStackFrame
{
    MethodDesc *pMethod;
    double start;
    bool shouldRecord;
};

static ProfilerStackFrame s_profilerStack[MAX_STACK_DEPTH];
static int s_topStackFrameIndex = -1;

// Adaptive sampling (shared with EP sampling or separate)
static double s_browserLastSampleTime = 0.0;
static int s_browserSkipsPerPeriod = 1;
static int s_browserSampleSkipCounter = 1;
bool s_browserProfilerEnabled = false;

static bool ShouldRecordFrame(double now)
{
    if (s_browserSampleSkipCounter < s_browserSkipsPerPeriod)
        return false;

    if (now == 0.0)
        now = SystemJS_GetProfilerNow();

    // Adaptive frequency adjustment (same algorithm as Mono)
    // ... (see Mono's should_record_frame)

    return true;
}

void BrowserProfiler_OnMethodEnter(MethodDesc *pMD)
{
    s_browserSampleSkipCounter++;
    s_topStackFrameIndex++;
    _ASSERTE(s_topStackFrameIndex < MAX_STACK_DEPTH);

    ProfilerStackFrame *frame = &s_profilerStack[s_topStackFrameIndex];
    double now = SystemJS_GetProfilerNow();
    frame->start = now;
    frame->shouldRecord = ShouldRecordFrame(now);
    frame->pMethod = pMD;
}

void BrowserProfiler_OnMethodLeave(MethodDesc *pMD)
{
    _ASSERTE(s_topStackFrameIndex >= 0);
    s_browserSampleSkipCounter++;

    ProfilerStackFrame *frame = &s_profilerStack[s_topStackFrameIndex];
    frame->shouldRecord = frame->shouldRecord || ShouldRecordFrame(0.0);

    if (frame->shouldRecord)
    {
        // Record to browser DevTools performance tab
        // TODO: Cache method name to avoid repeated string formatting
        SString methodName;
        TypeString::AppendMethodInternal(methodName, pMD, TypeString::FormatNamespace);
        SystemJS_BrowserProfilerMeasure(methodName.GetUTF8(), frame->start);

        // Mark parent for recording too
        if (s_topStackFrameIndex > 0)
            s_profilerStack[s_topStackFrameIndex - 1].shouldRecord = true;
    }

    s_topStackFrameIndex--;
}

#endif // HOST_BROWSER
```

### 6. Timing Source

Use `performance.now()` via JavaScript interop for browser timing. This provides:
- **Consistent timestamps** with browser DevTools (same clock source as performance.measure)
- **Cross-origin isolation awareness** (Emscripten's clock_gettime has 100μs resolution in non-isolated contexts; performance.now() has the same limitation but is the canonical source)

Declare in C:
```c
#ifdef HOST_BROWSER
extern "C" double SystemJS_GetProfilerNow(void);
#endif
```

For the adaptive sampling `GetHighResolutionTimestamp()`:
```cpp
static double GetHighResolutionTimestamp()
{
#ifdef HOST_BROWSER
    return SystemJS_GetProfilerNow();
#else
    // Fallback for non-browser WASI or other single-threaded platforms
    return (double)minipal_hires_ticks() / (double)minipal_hires_ticks_frequency() * 1000.0;
#endif
}
```

### 7. Extensibility for Future JIT/AOT

The design should accommodate future scenarios where CoreCLR WASM may use JIT or AOT compilation:

- **Callback interface is not tied to interpreter opcodes.** `SamplingProfiler_OnSamplepoint()` and `BrowserProfiler_OnMethodEnter/Leave()` are standalone C++ functions that can be called from any code generation backend.
- **For JIT/AOT:** Future work would emit calls to these functions at method entry/exit and backward branches in the generated native code, similar to how `CORJIT_FLAG_PROF_ENTERLEAVE` works for JIT but using these lighter-weight callbacks instead of ICorProfilerCallback.
- **The EP integration layer** (`ep_rt_coreclr_sample_profiler_enabled/disabled`) is already backend-agnostic — it just sets flags that the code generators check.

### 8. Tail Call Handling

For the browser DevTools profiler shadow stack, tail calls are handled with pop-and-push, matching Mono's approach:

```cpp
void BrowserProfiler_OnTailCall(MethodDesc *pCallerMD, MethodDesc *pCalleeMD)
{
    // Pop the caller frame (like a leave)
    BrowserProfiler_OnMethodLeave(pCallerMD);
    // Push the callee frame (like an enter)
    BrowserProfiler_OnMethodEnter(pCalleeMD);
}
```

The interpreter's tail call path (`UpdateFrameForTailCall`) should call this before reusing the frame. A new opcode `INTOP_PROF_TAILCALL` carries both the caller and callee MethodDesc references.

### 9. Exception Handling

For precision, the interpreter's exception dispatch code should explicitly call profiler leave callbacks for each frame being unwound, without creating new interpreter frames. This is done by injecting calls into the existing exception unwind loop:

```cpp
// In the interpreter's exception unwind path (not a new opcode — inline in exception dispatch):
while (unwinding through frames)
{
    if (s_browserProfilerEnabled)
    {
        // Record and pop the browser profiler shadow stack for this frame
        MethodDesc *pMD = pUnwindFrame->startIp->Method->GetMethodDesc();
        BrowserProfiler_OnMethodLeave(pMD);
    }
    if (s_samplingProfilerEnabled)
    {
        // Increment sample skip counter (for EP sampling frequency tracking)
        s_sampleSkipCounter++;
    }
    // ... existing frame unwind logic ...
}
```

This approach:
- Provides precise shadow stack tracking (each unwound frame is recorded)
- Does **not** create new interpreter frames (no INTOP_PROF_EXC_LEAVE opcode needed)
- Leverages the interpreter's explicit `InterpMethodContextFrame` parent chain for iteration
- Matches Mono's behavior where `method_exc_leave` fires for exception unwinding

## File Organization

| File | Purpose |
|------|---------|
| `src/coreclr/interpreter/inc/intops.def` | New opcode definitions |
| `src/coreclr/interpreter/compiler.cpp` | Opcode emission at backward branches, method entry/exit |
| `src/coreclr/vm/interpexec.cpp` | Opcode execution handlers |
| `src/coreclr/vm/eventing/eventpipe/ep-rt-coreclr.h` | Wire up `ep_rt_sample_profiler_enabled/disabled/session_enabled` |
| `src/coreclr/vm/eventing/eventpipe/ep-rt-coreclr-sampling.cpp` | EP sampling logic, adaptive frequency, stack walk + event emission |
| `src/coreclr/vm/wasm/browserprofiler.cpp` | Browser DevTools profiler (shadow stack, performance.measure bridge) |
| `src/native/libs/System.Native.Browser/native/diagnostics.ts` | JavaScript `performance.now()` and `performance.measure()` bridge (added to existing file) |

## Implementation Plan

### Phase 1: EP Samplepoint Sampling (Core)
1. Parse `DOTNET_WasmPerformanceInstrumentation` at startup, set `s_samplingProfilerEnabled`
2. Add `INTOP_PROF_SAMPLEPOINT` opcode definition
3. Emit at backward branches in `compiler.cpp` (gated on `s_samplingProfilerEnabled`)
4. Implement handler in `interpexec.cpp` with fast-path skip counter
5. Implement `ep_rt_coreclr_sample_profiler_enabled/disabled` to store EP event/thread
6. Implement `SamplingProfiler_OnSamplepoint` — adaptive frequency + stack walk + `ep_write_sample_profile_event`
7. Add `SystemJS_GetProfilerNow()` to `diagnostics.ts` for timing
8. Implement `ep_rt_coreclr_sample_profiler_session_enabled` with empty event for dotnet-gcdump

### Phase 2: Browser DevTools Profiler
1. Add `INTOP_PROF_ENTER`, `INTOP_PROF_LEAVE`, `INTOP_PROF_TAILCALL` opcodes
2. Emit at method entry/exit/tailcall in `compiler.cpp` (gated on `s_browserProfilerEnabled`)
3. Add `SystemJS_BrowserProfilerMeasure()` to `diagnostics.ts`
4. Implement browser profiler shadow stack with pop-and-push tail call handling
5. Add exception unwind callbacks in interpreter exception dispatch (inline, no new frames)
6. Add callspec filtering and sample interval configuration

### Phase 3: Polish & Integration
1. Method name caching for browser profiler (avoid repeated string formatting)
2. Testing with dotnet-trace, dotnet-gcdump, and browser DevTools

## Key Design Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Separate opcode vs extend INTOP_SAFEPOINT | **New INTOP_PROF_SAMPLEPOINT** | Keeps safepoint fast when profiling is off. Samplepoint is only emitted when profiling is enabled. |
| Stack walking approach | **Reuse existing** `ep_rt_coreclr_walk_managed_stack_for_thread` | Already handles interpreter frames correctly. No need for custom walk. |
| Timing source | **performance.now()** via JS interop | Matches browser DevTools clock. Same resolution as native clock on WASM. |
| Callback architecture | **Direct function calls** | No abstraction layer needed. EP callbacks call `ep_write_sample_profile_event` directly. Browser callbacks call `performance.measure()` directly. Simple function pointers gated by static booleans. |
| Enter/leave support | **Full instrumentation** | Required for browser DevTools profiler with accurate per-method timing. |
| Extensibility | **Design for JIT/AOT** | Callbacks are standalone functions callable from any backend, not tied to interpreter opcodes. |

## Comparison with Mono Implementation

| Aspect | Mono | CoreCLR (Proposed) |
|--------|------|-------------------|
| Samplepoint opcode | `MINT_PROF_SAMPLEPOINT` in mintops.def | `INTOP_PROF_SAMPLEPOINT` in intops.def |
| Emission location | Backward branches in transform.c | Backward branches in compiler.cpp |
| EP integration | `ep-rt-mono-runtime-provider.c` with MonoProfiler callbacks | `ep-rt-coreclr-sampling.cpp` with direct function calls |
| Browser profiler | `browser.c` with MonoProfiler callbacks | `wasm/browserprofiler.cpp` with direct function calls |
| Profiler callback system | MonoProfiler (multi-subscriber, per-method filter) | Static function pointers (single subscriber, compile-time gating) |
| Stack walking | `mono_walk_stack_with_ctx` | `ep_rt_coreclr_walk_managed_stack_for_thread` |
| Timing | `mono_wasm_profiler_now()` → `performance.now()` | `SystemJS_GetProfilerNow()` → `performance.now()` |
| Shadow stack for DevTools | `profiler_stack_frames[]` with SP tracking | `s_profilerStack[]` — simpler, no SP needed (interpreter has explicit frames) |
| Method filter | `MonoCallSpec` (regex-based) | `DOTNET_WasmPerformanceInstrumentation` callspec (same format) |
| AOT support | Yes, AOT code calls same MonoProfiler hooks | Designed for extensibility, interpreter-only initially |
| Tail calls | Pop-and-push via `tail_call` callback | Pop-and-push via `INTOP_PROF_TAILCALL` |
| Exception unwind | `method_exc_leave` callback per frame | Inline calls in exception dispatch loop (no new frames) |

## Resolved Design Decisions

1. **Profiling enabled at startup:** Profiling is enabled via `DOTNET_WasmPerformanceInstrumentation` before managed code runs. No runtime recompilation needed — all methods are compiled with instrumentation from the start.

2. **Conditional emission only:** Samplepoints are NOT always emitted. They are only emitted when `s_samplingProfilerEnabled` is true (set at startup). Zero overhead when profiling is not configured.

3. **Dedicated CoreCLR TypeScript:** Browser profiler JS bridge lives in `src/native/libs/System.Native.Browser/native/diagnostics.ts`, separate from Mono's implementation.

4. **Tail calls:** Pop-and-push like Mono. `BrowserProfiler_OnTailCall()` pops the caller frame and pushes the callee frame on the shadow stack.

5. **Exception leave:** Precise tracking. The interpreter's exception unwind loop explicitly calls `BrowserProfiler_OnMethodLeave()` for each frame being unwound, without creating new interpreter frames.
