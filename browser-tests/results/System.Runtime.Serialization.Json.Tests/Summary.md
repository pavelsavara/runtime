# System.Runtime.Serialization.Json.Tests - Browser/WASM CoreCLR Test Summary

## Test Configuration
- **Runtime**: CoreCLR (interpreter mode)
- **Platform**: Browser/WASM
- **Date**: 2026-02-01

## Results
- **Total Tests**: 160
- **Passed**: 160
- **Failed**: 0
- **Skipped**: 0
- **Disabled**: 13 (via ActiveIssue)

## Status: ⚠️ PASSED (with 13 disabled tests)

## Disabled Tests
The following 13 tests were disabled due to `BadImageFormatException` in `System.Signature.Init`:

1. `DCJS_ListGenericRoot`
2. `DCJS_ListGenericMembers`
3. `DCJS_Nullables`
4. `DCJS_SuspensionManager`
5. `DCJS_TypeWithGenericDictionaryAsKnownType`
6. `DCJS_TypeWithKnownTypeAttributeAndInterfaceMember`
7. `DCJS_TypeWithKnownTypeAttributeAndListOfInterfaceMember`
8. `DCJS_UseSimpleDictionaryFormat`
9. `DCJS_VerifyDateTimeForFormatStringDCJsonSerSettings`
10. `DCJS_VerifyDictionaryFormat`
11. `DCJS_VerifyDictionaryTypes`
12. `DCJS_VerifyIndentation`
13. `DCJS_WithListOfXElement`

## Notes
- Same tests file as ReflectionOnly variant, but uses non-reflection code paths
- One additional test disabled: `DCJS_VerifyDateTimeForFormatStringDCJsonSerSettings` (same BadImageFormatException issue)

## Issue Reference
All failures tracked in: https://github.com/dotnet/runtime/issues/123011
