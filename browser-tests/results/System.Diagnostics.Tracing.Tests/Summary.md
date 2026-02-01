# System.Diagnostics.Tracing.Tests - Browser WASM Test Results

## Test Suite Information
- **Test Project**: System.Diagnostics.Tracing.Tests
- **Project Path**: src/libraries/System.Diagnostics.Tracing/tests/System.Diagnostics.Tracing.Tests.csproj
- **Test Date**: 2026-02-01 16:57

## Results Summary
| Metric | Count |
|--------|-------|
| Total Tests Run | 39 |
| Passed | 32 |
| Failed | 5 |
| Skipped | 2 |

## Test Comparison (vs Mono Baseline)
| Metric | Count |
|--------|-------|
| Mono Tests | 41 |
| CoreCLR Tests | 41 |
| Extra in CoreCLR | 0 |
| Missing in CoreCLR | 0 |

## Status: ⚠️ NEEDS ActiveIssue MARKING

5 tests failed related to EventSource ActivityTracking - activity IDs are not being set.

## Failed Tests

### 1. BasicEventSourceTests.ActivityTracking.ActivityFlowsAsync
**Error**: Assert.NotEqual() Failure: Values are equal - Activity ID is 00000000-0000-0000-0000-000000000000 when it shouldn't be

### 2. BasicEventSourceTests.ActivityTracking.SetCurrentActivityIdAfterEventDoesNotFlowAsync
**Error**: Assert.Equal() Failure: Expected 900d8b94-2b76-426c-82aa-4588a2d8e7c9, Actual 00000000-0000-0000-0000-000000000000

### 3. BasicEventSourceTests.ActivityTracking.SetCurrentActivityIdBeforeEventFlowsAsync
**Error**: Assert.Equal() Failure: Expected 7647ae38-8825-4914-bf15-8ab6e93121eb, Actual 00000000-0000-0000-0000-000000000000

### 4. BasicEventSourceTests.ActivityTracking.StartStopCreatesActivity
**Error**: Assert.NotEqual() Failure: Values are equal - Activity ID is 00000000-0000-0000-0000-000000000000

### 5. BasicEventSourceTests.TestsWrite.Test_Write_T(listener: EventListener(UseEventsToListen=False))
**Error**: Assert.Equal() Failure: Expected 42, Actual 3

## Root Cause Analysis
EventSource activity tracking (ActivityId) is not functioning correctly on Browser/WASM + CoreCLR. The ActivityId remains at the default Guid value (all zeros) instead of being set during Start/Stop events.

## Action Required
Mark failing tests with:
```csharp
[ActiveIssue("https://github.com/dotnet/runtime/issues/123011", TestPlatforms.Browser)]
```
