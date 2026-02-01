# System.Runtime.Serialization.Xml.ReflectionOnly.Tests - Browser/WASM CoreCLR Test Results

## Summary
- **Status**: ❌ BLOCKED
- **Date**: 2026-02-01

## Issue

This test suite causes runtime crashes on Browser/WASM + CoreCLR (interpreter mode). The tests cannot complete due to catastrophic "table index is out of bounds" crashes.

## Failure Pattern

1. Tests involving serialization of generic collections trigger a `BadImageFormatException` in:
   ```
   System.Runtime.Serialization.DataContracts.CollectionDataContract.GetCollectionElementType()
   ```
   
2. After the first exception, subsequent tests crash the runtime with:
   ```
   RuntimeError: table index is out of bounds
   ```

## Root Cause Analysis

The `CollectionDataContract.GetCollectionElementType()` method uses reflection to look up generic methods:
```csharp
Type.GetMethod(String name, BindingFlags bindingAttr, Type[] types)
```

On the CoreCLR interpreter with Browser/WASM, this reflection path hits a `BadImageFormatException`:
```
System.BadImageFormatException: An attempt was made to load a program with an incorrect format. (0x8007000B)
   at System.Signature.Init(...)
   at System.Reflection.RuntimeMethodInfo.GetParametersAsSpan()
   at System.RuntimeType.FilterApplyMethodBase(...)
```

This appears to be a fundamental issue with the CoreCLR interpreter's handling of generic method signatures in reflection scenarios.

## Tests Affected (partial list from crash logs)

- `DCS_TypeWithPrimitiveKnownTypes`
- `DCS_TypeWithVirtualGenericProperty`
- `DCS_CircularTypes_PreserveObjectReferences_True`
- `DCS_DerivedTypeWithDifferentOverrides`
- (many more - tests don't complete due to crashes)

## Resolution

Added `IgnoreForCI` condition to the csproj file:
```xml
<!-- https://github.com/dotnet/runtime/issues/123011 - Tests crash CoreCLR interpreter on Browser due to BadImageFormatException in generic reflection -->
<IgnoreForCI Condition="'$(TargetOS)' == 'browser' and '$(RuntimeFlavor)' == 'coreclr'">true</IgnoreForCI>
```

Also added `[ActiveIssue]` attributes to two tests in the shared `DataContractSerializer.cs` file:
- `DCS_TypeWithPrimitiveKnownTypes`
- `DCS_TypeWithVirtualGenericProperty`

## Files Modified
- `src/libraries/System.Runtime.Serialization.Xml/tests/ReflectionOnly/System.Runtime.Serialization.Xml.ReflectionOnly.Tests.csproj` (IgnoreForCI)
- `src/libraries/System.Runtime.Serialization.Xml/tests/DataContractSerializer.cs` (ActiveIssue attributes)

## Mono Baseline
- Mono tests: 265 tests pass on Browser/WASM
