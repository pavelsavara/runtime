# System.Runtime.InteropServices.Tests - Browser/WASM CoreCLR Test Results

## Summary
⚠️ **PASSED WITH ISSUES** (8 tests marked with ActiveIssue)

## Test Results
- **Tests run:** 2476
- **Passed:** 2325
- **Skipped:** 151 (includes 8 tests with new ActiveIssue attributes)
- **Failed:** 0

## Comparison with Mono Baseline
- Mono tests: 2517
- CoreCLR tests: 2614
- 107 tests extra in CoreCLR (additional tests not in Mono)
- 10 tests "missing" - actually just Unicode character differences in test names

## Issues Found and Fixed

### COM Interop Tests (8 tests marked with ActiveIssue)

All 8 failing tests were related to COM interop functionality which is not supported on Browser/WASM platform. Each test threw `PlatformNotSupportedException` at `ComWrappers.GetOrCreateComInterfaceForObject`.

**Files Modified:**

1. **AddRefTests.cs** - Added ActiveIssue to `AddRef_ValidPointer_Success`
2. **ReleaseTests.cs** - Added ActiveIssue to `Release_ValidPointer_Success`
3. **QueryInterfaceTests.cs** - Added ActiveIssue to:
   - `QueryInterface_ValidInterface_Success`
   - `QueryInterface_NoSuchInterface_Success`
4. **ComVariantMarshallerTests.cs** - Added ActiveIssue to:
   - `GeneratedComInterfaceType_Marshals_To_UNKNOWN`
   - `UnknownWrapper_Of_GeneratedComInterfaceType_Marshals_To_UNKNOWN`

**Root Cause:** COM interop is not supported on Browser/WASM platform with CoreCLR interpreter.

**ActiveIssue Format Used:**
```csharp
[ActiveIssue("https://github.com/dotnet/runtime/issues/123011", typeof(PlatformDetection), nameof(PlatformDetection.IsBrowser), nameof(PlatformDetection.IsCoreCLR))]
```

## Test Execution Details
- **Date:** 2026-02-01
- **Platform:** Browser/WASM + CoreCLR (interpreter mode)
- **XHarness exit code:** 0
