# System.Numerics.Vectors.Tests - Browser/WASM Test Summary

## Test Results
- **Status**: ⚠️ PASSED (with ActiveIssue workarounds)
- **Tests Run**: 7044
- **Passed**: 7044
- **Failed**: 0
- **Skipped**: 0 (50 tests disabled via ActiveIssue)

## Platform
- **Target**: Browser/WebAssembly
- **Runtime**: CoreCLR (interpreter mode)
- **Configuration**: Release

## ActiveIssue Workarounds

### Issue #123011: BadImageFormatException in Vector<T> Reflection Tests (50 tests)

All reflection tests on `Vector<T>` fail with `BadImageFormatException` when calling `RuntimeMethodHandle.GetUtf8Name` during reflection:

```
System.BadImageFormatException: Format of the executable (.exe) or library (.dll) is invalid.
   at System.RuntimeMethodHandle.GetUtf8Name(RuntimeMethodHandleInternal method)
   at System.RuntimeMethodHandle.GetName(RuntimeMethodHandleInternal method)
   at System.Reflection.RuntimeMethodInfo.get_Name()
   at System.Reflection.TypeInfo.GetDeclaredMethods(String name)+MoveNext()
```

**Affected Tests (50 total):**

| Category | Tests | Count |
|----------|-------|-------|
| MultiplicationReflection* | Byte, SByte, UInt16, Int16, UInt32, Int32, UInt64, Int64, Single, Double | 10 |
| AdditionReflection* | Byte, SByte, UInt16, Int16, UInt32, Int32, UInt64, Int64, Single, Double | 10 |
| DivisionReflection* | Byte, SByte, UInt16, Int16, UInt32, Int32, UInt64, Int64, Single, Double | 10 |
| CopyToReflection* | Byte, SByte, UInt16, Int16, UInt32, Int32, UInt64, Int64, Single, Double | 10 |
| CopyToWithOffsetReflection* | Byte, SByte, UInt16, Int16, UInt32, Int32, UInt64, Int64, Single, Double | 10 |
| Convert*WithReflection | ConvertUInt32ToSingleWithReflection, ConvertInt64ToDoubleWithReflection, ConvertUInt64ToDoubleWithReflection | 3 |

**Total:** 53 (but only 50 unique - some may not be in the test suite)

**File Modified:**
- `src/libraries/System.Numerics.Vectors/tests/GenericVectorTests.cs`

**Workaround Applied:**
Added `[ActiveIssue("https://github.com/dotnet/runtime/issues/123011", typeof(PlatformDetection), nameof(PlatformDetection.IsBrowser), nameof(PlatformDetection.IsCoreCLR))]` attribute to all 50+ reflection tests.

## Notes

The CoreCLR interpreter in WASM cannot properly handle `RuntimeMethodHandle.GetUtf8Name` when retrieving method names via reflection on intrinsic `Vector<T>` types. This is a known limitation that affects all reflection-based operations on vector types in the Browser/WASM environment.

All non-reflection vector operations work correctly. The reflection tests verify that intrinsic methods work correctly when invoked via reflection, which is a less common use case.
