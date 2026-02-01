# Test: PackedSimdTests.* (45 tests)

## Test Suite
System.Runtime.Intrinsics.Tests

## Failure Type
assertion

## Exception Type
Xunit.Sdk.TrueException

## Stack Trace
```
Assert.True() Failure
Expected: True
Actual:   False
   at System.Runtime.Intrinsics.Wasm.Tests.PackedSimdTests.PackedSimdIsSupported()
```

## Notes
- Platform: Browser/WASM + CoreCLR
- Category: interpreter
- 45 tests affected - all PackedSimd tests fail
- `PackedSimd.IsSupported` returns `false` on CoreCLR interpreter
- WASM SIMD hardware intrinsics are not supported on CoreCLR interpreter, only on Mono runtime
- All tests in PackedSimdTests class are affected
