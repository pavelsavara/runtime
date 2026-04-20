// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"

#ifdef TARGET_BROWSER

#include <emscripten.h>
#include "method.hpp"
#include "typestring.h"
#include "wasm/browserprofiler.h"

// Inline JS for performance.measure() — records a timing entry to the
// browser DevTools Performance tab. UTF8ToString is an Emscripten
// runtime method exported via EMCC_EXPORTED_RUNTIME_METHODS.
EM_JS(void, BrowserProfiler_RecordMeasure, (const char *name, double start), {
    globalThis.performance.measure(UTF8ToString(name), { start : start });
})

static constexpr int MAX_STACK_DEPTH = 600;

struct ProfilerStackFrame
{
    MethodDesc *pMethod;
    double startMs;
    bool shouldRecord;
};

static ProfilerStackFrame s_profilerStack[MAX_STACK_DEPTH];
static int s_topStackFrameIndex = -1;

// Adaptive sampling state — controls how often we actually call
// performance.measure(). The shadow stack always tracks enter/leave
// for correctness, but recording is rate-limited.
static int32_t s_browserSkipsPerPeriod = 10;
static int32_t s_browserSampleSkipCounter = 0;
static double s_browserLastRecordTimeMs = 0.0;
static constexpr double s_desiredRecordIntervalMs = 1.0;

static bool ShouldRecordFrame()
{
    if (++s_browserSampleSkipCounter < s_browserSkipsPerPeriod)
        return false;

    double now = emscripten_get_now();
    if (s_browserLastRecordTimeMs > 0.0)
    {
        double elapsed = now - s_browserLastRecordTimeMs;
        if (elapsed > 0.0)
        {
            double ratio = s_desiredRecordIntervalMs / elapsed;
            int32_t newSkips = (int32_t)((double)s_browserSampleSkipCounter * ratio);
            if (newSkips < 1)
                newSkips = 1;
            s_browserSkipsPerPeriod = newSkips;
        }
    }

    s_browserLastRecordTimeMs = now;
    s_browserSampleSkipCounter = 0;

    return true;
}

void BrowserProfiler_OnMethodEnter(void *pMethodDesc)
{
    MethodDesc *pMD = (MethodDesc *)pMethodDesc;

    s_topStackFrameIndex++;
    _ASSERTE(s_topStackFrameIndex < MAX_STACK_DEPTH);

    ProfilerStackFrame *frame = &s_profilerStack[s_topStackFrameIndex];
    frame->pMethod = pMD;
    frame->startMs = emscripten_get_now();
    frame->shouldRecord = ShouldRecordFrame();
}

void BrowserProfiler_OnMethodLeave()
{
    if (s_topStackFrameIndex < 0)
        return;

    ProfilerStackFrame *frame = &s_profilerStack[s_topStackFrameIndex];

    if (frame->shouldRecord)
    {
        SString methodName;
        TypeString::AppendMethodInternal(methodName, frame->pMethod, TypeString::FormatNamespace);
        BrowserProfiler_RecordMeasure(methodName.GetUTF8(), frame->startMs);

        // Mark parent frame for recording so the flame chart nests properly.
        if (s_topStackFrameIndex > 0)
            s_profilerStack[s_topStackFrameIndex - 1].shouldRecord = true;
    }

    s_topStackFrameIndex--;
}

#endif // TARGET_BROWSER
