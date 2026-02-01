# System.Drawing.Primitives.Tests Summary

## Latest Run
- **Date:** 2026-02-01
- **CoreCLR:** Tests run: 1819, Passed: 1817, Failed: 0, Skipped: 2 + 574 disabled
- **Mono Baseline:** Tests run: 2439, Passed: 2437, Failed: 0, Skipped: 2
- **Status:** ⚠️ Tests disabled with ActiveIssue

## Test Set Comparison

Run: `./browser-tests/compare-test-results.sh System.Drawing.Primitives.Tests`

### Extra in CoreCLR (0 tests)

No extra tests in CoreCLR.

### Missing in CoreCLR (574 tests)

These tests were disabled due to KnownColor/Color.FromName/Color.FromKnownColor not working on CoreCLR Browser:
- `ColorTests.ArgbValues` (144 test cases)
- `ColorTests.GetHashCodeTest` (143 test cases)
- `ColorTests.IsNamedColor` (1 test)
- `ColorTests.IsSystemColor` (1 test)
- `ColorTests.KnownNames` (145 test cases)
- `ColorTests.ToStringNamed` (146 test cases)
- `ColorTranslatorTests.FromHtml_String_ReturnsExpected` (partial - color name-based tests)

## Disabled Tests (ActiveIssue #123011)

| Test Name | Failure Type | Category |
|-----------|--------------|----------|
| ColorTests.ArgbValues | assertion | enum/interpreter |
| ColorTests.GetHashCodeTest | assertion | enum/interpreter |
| ColorTests.IsNamedColor | assertion | enum/interpreter |
| ColorTests.IsSystemColor | assertion | enum/interpreter |
| ColorTests.KnownNames | assertion | enum/interpreter |
| ColorTests.ToStringNamed | assertion | enum/interpreter |
| ColorTranslatorTests.FromHtml_String_ReturnsExpected | assertion | enum/interpreter |

## Failures and Asserts

See [KnownColor_Tests.md](../../failures/System.Drawing.Primitives.Tests/KnownColor_Tests.md) for details.

## Root Cause

`Color.FromKnownColor(KnownColor)` and `Color.FromName(string)` return `Color.Empty` instead of the expected named color on CoreCLR Browser/WASM. This is likely related to the EnumComparer<T> bug also seen in System.Collections.Immutable.Tests.
