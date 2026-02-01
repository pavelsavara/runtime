# Test: KnownColor Tests (574 failures)

## Test Suite
System.Drawing.Primitives.Tests

## Failing Test Methods
- `ColorTests.ArgbValues`
- `ColorTests.GetHashCodeTest`
- `ColorTests.IsNamedColor`
- `ColorTests.IsSystemColor`
- `ColorTests.KnownNames`
- `ColorTests.ToStringNamed`
- `ColorTranslatorTests.FromHtml_String_ReturnsExpected`

## Failure Type
assertion

## Exception Type
Assert.Equal failure

## Description
All tests that rely on `Color.FromKnownColor(KnownColor)` fail because the method returns `Color [Empty]` instead of the expected named color. This affects:
- Getting color by name
- ToString returning the color name
- IsNamedColor property
- IsSystemColor property
- GetHashCode (for named colors)

## Sample Stack Trace
```
Assert.Equal() Failure: Strings differ
                  ↓ (pos 7)
Expected: "Color [Red]"
Actual:   "Color [Empty]"
                  ↑ (pos 7)
   at System.Drawing.Primitives.Tests.ColorTests.ToStringNamed(String name)
   at InvokeStub_ColorTests.ToStringNamed(Object, Span`1)
   at System.Reflection.MethodBaseInvoker.InvokeWithOneArg(Object obj, BindingFlags invokeAttr, Binder binder, Object[] parameters, CultureInfo culture)
```

## Root Cause
`Color.FromKnownColor(KnownColor)` is not working correctly on CoreCLR + Browser/WASM. The KnownColor enum values are not being properly resolved to their corresponding Color instances. This may be related to the EnumComparer<T> bug also seen in System.Collections.Immutable.Tests.

## Notes
- Platform: Browser/WASM + CoreCLR
- Category: interpreter/enum
- Total failures: 574
- Passes on Mono: Yes (2437 pass, 0 fail)
- This is likely a variant of the Enum handling bug in the CoreCLR interpreter
