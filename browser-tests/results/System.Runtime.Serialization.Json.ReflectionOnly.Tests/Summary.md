# System.Runtime.Serialization.Json.ReflectionOnly.Tests - Browser/WASM CoreCLR Test Summary

## Test Configuration
- **Runtime**: CoreCLR (interpreter mode)
- **Platform**: Browser/WASM
- **Date**: 2026-02-01

## Results
- **Total Tests**: 161
- **Passed**: 161
- **Failed**: 0
- **Skipped**: 0
- **Disabled**: 12 (via ActiveIssue)

## Status: ⚠️ PASSED (with 12 disabled tests)

## Disabled Tests
The following 12 tests were disabled due to `BadImageFormatException` in `System.Signature.Init` when parsing method signatures for generic types during DataContractJsonSerializer operations:

1. `DCJS_ListGenericRoot`
2. `DCJS_ListGenericMembers`
3. `DCJS_Nullables` (also has struct serialization bug)
4. `DCJS_SuspensionManager`
5. `DCJS_TypeWithGenericDictionaryAsKnownType`
6. `DCJS_TypeWithKnownTypeAttributeAndInterfaceMember`
7. `DCJS_TypeWithKnownTypeAttributeAndListOfInterfaceMember`
8. `DCJS_UseSimpleDictionaryFormat`
9. `DCJS_VerifyDictionaryFormat`
10. `DCJS_VerifyDictionaryTypes`
11. `DCJS_VerifyIndentation`
12. `DCJS_WithListOfXElement`

## Root Cause
`System.Signature.Init` throws `BadImageFormatException` (0x8007000B) when reading method signatures for generic collection types during serialization. This affects reflection-based serialization of:
- Lists (`List<T>`)
- Dictionaries (`Dictionary<K,V>`)
- Collections with interface members

Additionally, `DCJS_TypeWithKnownTypeAttributeAndInterfaceMember` caused a complete crash with "table index is out of bounds".

## Issue Reference
All failures tracked in: https://github.com/dotnet/runtime/issues/123011

## Files Modified
- [DataContractJsonSerializer.cs](src/libraries/System.Runtime.Serialization.Json/tests/DataContractJsonSerializer.cs) - Added ActiveIssue attributes

## Failure Documentation
- [Serialization_BadImageFormatException.md](browser-tests/failures/System.Runtime.Serialization.Json.ReflectionOnly.Tests/Serialization_BadImageFormatException.md)
