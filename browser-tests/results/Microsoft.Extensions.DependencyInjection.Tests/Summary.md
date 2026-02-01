# Microsoft.Extensions.DependencyInjection.Tests - Browser/WASM CoreCLR Test Results

## Summary
- **Status**: ⚠️ PASSED with ActiveIssue marks
- **Date**: 2026-02-01

## Test Results
- **Tests run**: 1341
- **Passed**: 1305
- **Failed**: 0
- **Skipped**: 36

## Mono Baseline Comparison
- **Mono tests**: 1362
- **CoreCLR tests**: 1363
- **Extra in CoreCLR**: 1 (test runs on CoreCLR but was skipped on Mono)
- **Missing in CoreCLR**: 0

## Changes Made

### ServiceProviderCompilationTest.cs
Added `[ActiveIssue]` attribute to the entire class (5 test methods):

```csharp
[ActiveIssue("https://github.com/dotnet/runtime/issues/123011", typeof(PlatformDetection), nameof(PlatformDetection.IsBrowser), nameof(PlatformDetection.IsCoreCLR))]
```

**Affected tests** (all `CompilesInLimitedStackSpace` variants):
- `CompilesInLimitedStackSpace(mode: Default, serviceType: typeof(I999))`
- `CompilesInLimitedStackSpace(mode: Dynamic, serviceType: typeof(I999))`
- `CompilesInLimitedStackSpace(mode: Runtime, serviceType: typeof(I999))`
- `CompilesInLimitedStackSpace(mode: ILEmit, serviceType: typeof(I999))`
- `CompilesInLimitedStackSpace(mode: Expressions, serviceType: typeof(I999))`

## Root Cause Analysis

The `CompilesInLimitedStackSpace` test uses `Thread.Start()` to create a new thread with a limited stack size:

```csharp
var thread = new Thread(() => { ... }, stackSize);
thread.Start();
```

On Browser/WASM with CoreCLR (single-threaded mode), `Thread.Start()` throws:
```
System.PlatformNotSupportedException: Operation is not supported on this platform.
   at System.Threading.Thread.ThrowIfSingleThreaded()
   at System.Threading.Thread.Start(Boolean captureContext)
   at System.Threading.Thread.Start()
```

The test class already had `[ActiveIssue]` for Mono runtime. The same exclusion now applies to Browser+CoreCLR.

## Files Modified
- `src/libraries/Microsoft.Extensions.DependencyInjection/tests/DI.Tests/ServiceProviderCompilationTest.cs`
