# Test: Vector64Tests/Vector128Tests/Vector256Tests.GetOne* (36 tests)

## Test Suite
System.Runtime.Intrinsics.Tests

## Failure Type
assertion

## Exception Type
Xunit.Sdk.EqualException

## Stack Trace
```
Assert.Equal() Failure: Values differ
Expected: <0, 0>
Actual:   <1, 1>
   at System.Runtime.Intrinsics.Tests.Vectors.Vector64Tests.TestGetOne[T]()
   at System.Runtime.Intrinsics.Tests.Vectors.Vector64Tests.GetOneInt32()
```

## Notes
- Platform: Browser/WASM + CoreCLR
- Category: interpreter
- 36 tests affected across Vector64Tests, Vector128Tests, and Vector256Tests
- The `Vector64<T>.One`, `Vector128<T>.One`, and `Vector256<T>.One` properties return zero vectors instead of vectors filled with 1s on CoreCLR interpreter
- Tests affected: GetOneByte, GetOneDouble, GetOneInt16, GetOneInt32, GetOneInt64, GetOneIntPtr, GetOneSByte, GetOneSingle, GetOneUInt16, GetOneUInt32, GetOneUInt64, GetOneUIntPtr (12 tests per Vector size)
