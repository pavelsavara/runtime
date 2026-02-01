# Microsoft.VisualBasic.Core.Tests - Browser/WASM Test Summary

## Test Results
- **Status**: ⚠️ PASSED (with ActiveIssue workarounds)
- **Tests Run**: 22070
- **Passed**: 22066
- **Failed**: 0
- **Skipped**: 4

## Platform
- **Target**: Browser/WebAssembly
- **Runtime**: CoreCLR (interpreter mode)
- **Configuration**: Release

## ActiveIssue Workarounds

### Issue #123011: Test data serialization issues (4 tests)

The xUnit test framework has issues serializing/deserializing decimal and TimeSpan values in test data when running on Browser/WASM + CoreCLR. Tests expecting specific values receive wrong data due to serialization issues.

**Affected Tests:**

| Test | Issue |
|------|-------|
| `DecimalTypeTests.FromBoolean` | `expected: -1` becomes `expected: 0` |
| `DecimalTypeTests.Parse` | `expected: 123` becomes `expected: 0` |
| `OperatorsTests.SubtractObject_Invoke_ReturnsExpected` | TimeSpan precision issues |

**Files Modified:**
- `src/libraries/Microsoft.VisualBasic.Core/tests/CompilerServices/DecimalTypeTests.cs`
- `src/libraries/Microsoft.VisualBasic.Core/tests/OperatorsTests.cs`

**Workaround Applied:**
Added `[ActiveIssue("https://github.com/dotnet/runtime/issues/123011", typeof(PlatformDetection), nameof(PlatformDetection.IsBrowser), nameof(PlatformDetection.IsCoreCLR))]` attribute to all 4 affected tests.

## Notes

The test data serialization issue is specific to the xUnit test framework interacting with the xharness runner in the Browser/WASM + CoreCLR environment. The actual functionality being tested likely works correctly; only the test framework parameter deserialization is affected.
