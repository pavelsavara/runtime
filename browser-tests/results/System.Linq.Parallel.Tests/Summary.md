# System.Linq.Parallel.Tests - Browser/WASM Test Summary

## Test Results
- **Status**: ⚠️ PASSED (with ActiveIssue workarounds)
- **Tests Run**: 27737
- **Passed**: 27628
- **Failed**: 0
- **Skipped**: 109

## Platform
- **Target**: Browser/WebAssembly
- **Runtime**: CoreCLR (interpreter mode)
- **Configuration**: Release

## ActiveIssue Workarounds

### Issue #123011: BadImageFormatException in DefaultIfEmpty tests (2 tests)

Two `DefaultIfEmpty_Empty` tests fail with `BadImageFormatException` when calling `RuntimeMethodHandle.GetUtf8Name` during reflection operations in LINQ:

```
System.BadImageFormatException : Format of the executable (.exe) or library (.dll) is invalid.
   at System.RuntimeMethodHandle.GetUtf8Name(RuntimeMethodHandleInternal method)
   at System.RuntimeMethodHandle.GetName(RuntimeMethodHandleInternal method)
   at System.Reflection.RuntimeMethodInfo.get_Name()
   at System.Linq.Enumerable.TryGetFirst[TSource](IEnumerable`1 source, Func`2 predicate, Boolean& found)
```

**Affected Tests:**
- `DefaultIfEmpty_Empty<T>`
- `DefaultIfEmpty_Empty_NotPipelined<T>`

**File Modified:**
- `src/libraries/System.Linq.Parallel/tests/QueryOperators/DefaultIfEmptyTests.cs`

**Workaround Applied:**
Added `[ActiveIssue("https://github.com/dotnet/runtime/issues/123011", typeof(PlatformDetection), nameof(PlatformDetection.IsBrowser), nameof(PlatformDetection.IsCoreCLR))]` attribute to both tests.

## Notes

The failure occurs when LINQ operations internally call `Assert.Single` which uses reflection to get method names. The CoreCLR interpreter cannot properly handle `RuntimeMethodHandle.GetUtf8Name` in certain scenarios on Browser/WASM.
