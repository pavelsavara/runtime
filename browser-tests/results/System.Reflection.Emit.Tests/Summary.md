# System.Reflection.Emit.Tests - Browser CoreCLR Test Summary

## Test Run Details
- **Date:** 2026-02-01
- **Platform:** Browser/WASM + CoreCLR (interpreter mode)
- **Configuration:** Release build

## Results: ⚠️ PASSED WITH ISSUES

### Test Execution Summary
- **Tests run:** 2016
- **Passed:** 2005
- **Failed:** 0
- **Skipped:** 11

### Comparison with Mono Baseline
- **Mono tests:** 2019
- **CoreCLR tests:** 2003 (after skipping 3 + 11 original skips)
- **Extra in CoreCLR:** 0
- **Missing in CoreCLR:** 16 (parameter-specific tests with different data)

### Tests Marked with ActiveIssue
3 tests marked with `[ActiveIssue("https://github.com/dotnet/runtime/issues/123011")]`:

1. **Invoke_Private_CrossAssembly_ThrowsMethodAccessException**
   - File: `src/libraries/System.Reflection.Emit/tests/AssemblyBuilderTests.cs`
   - Issue: Expected MethodAccessException not thrown on CoreCLR interpreter

2. **Invoke_Internal_CrossAssembly_ThrowsMethodAccessException**
   - File: `src/libraries/System.Reflection.Emit/tests/AssemblyBuilderTests.cs`
   - Issue: Expected MethodAccessException not thrown on CoreCLR interpreter

3. **Invoke_Private_SameAssembly_ThrowsMethodAccessException**
   - File: `src/libraries/System.Reflection.Emit/tests/AssemblyBuilderTests.cs`
   - Issue: Expected MethodAccessException not thrown on CoreCLR interpreter

## Files Modified
- `src/libraries/System.Reflection.Emit/tests/AssemblyBuilderTests.cs` - Added 3 ActiveIssue attributes

## Root Cause Analysis
The CoreCLR interpreter does not enforce method access restrictions (private/internal) when invoking dynamically emitted code. The delegate created from the dynamic method can call private/internal methods without throwing `MethodAccessException`.

## Notes
- All other Reflection.Emit functionality (TypeBuilder, MethodBuilder, ILGenerator, etc.) works correctly
- This appears to be an access verification issue specific to the interpreter
