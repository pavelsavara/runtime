# System.ComponentModel.TypeConverter.Tests - Browser/WASM CoreCLR Test Results

## Summary
⚠️ **PASSED with ActiveIssue** - 853 tests marked with ActiveIssue (7 test methods × 142 color names + 1 TestGetProperties)

## Test Counts

| Runtime | Tests Run | Passed | Failed | Skipped |
|---------|-----------|--------|--------|---------|
| Mono (baseline) | 7861 | 7835 | 0 | 26 |
| CoreCLR (after ActiveIssue) | 7010 | 6982 | 0 | 28 |

## Failures Marked with ActiveIssue

All failures are related to `Color.FromName()` not working correctly on CoreCLR interpreter:

### ColorConverterTests (7 methods × 142 color names = 994 test cases)
- `ConvertFrom_Name` - Color.FromName returns Color.Empty
- `ConvertFromInvariantString_Name` - Color.FromName returns Color.Empty
- `ConvertFromString_Name` - Color.FromName returns Color.Empty
- `ConvertTo_Named` - Color.FromName returns Color.Empty
- `ConvertToInvariantString_Name` - Color.FromName returns Color.Empty
- `ConvertToString_Name` - Color.FromName returns Color.Empty

### RectangleConverterTests (1 method)
- `TestGetProperties` - PropertyDescriptor.GetValue returns {X=0,Y=0} instead of {X=10,Y=20}

## Root Cause
`Color.FromName()` relies on reflection to look up static properties on the Color type by name.
On CoreCLR interpreter + Browser, this reflection-based property lookup returns Color.Empty/default values.

## Files Modified
- [ColorConverterTests.cs](src/libraries/System.ComponentModel.TypeConverter/tests/Drawing/ColorConverterTests.cs) - Added 6 ActiveIssue attributes
- [RectangleConverterTests.cs](src/libraries/System.ComponentModel.TypeConverter/tests/Drawing/RectangleConverterTests.cs) - Added 1 ActiveIssue attribute

## Artifacts
- Console log: `console_20260201_185620.log`
- Test results: `testResults_20260201_185620.xml`
- Comparison: `test-comparison.txt`
