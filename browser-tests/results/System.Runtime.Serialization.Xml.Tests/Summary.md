# System.Runtime.Serialization.Xml.Tests - Browser/CoreCLR Test Summary

## Test Run Results
- **Status**: ⚠️ PASS (with skips)
- **Tests Run**: 327
- **Passed**: 322
- **Failed**: 0
- **Skipped**: 5 (includes newly added ActiveIssue skips)
- **Previously Skipped**: 5 (Mono baseline)

## Failure Categories

### BadImageFormatException in Collection Serialization (15 tests)
- **Root Cause**: `System.Signature.Init` fails with `BadImageFormatException (0x8007000B)` when deserializing types that involve collection serialization
- **Call Path**: `XmlFormatWriterGenerator.CriticalHelper.WriteCollection` → `Type.GetMethod()` → `RuntimeType.GetMethodCandidates` → `RuntimeMethodInfo.GetParametersAsSpan` → `Signature.Init`
- **Issue**: The CoreCLR interpreter cannot handle dynamically generated method signatures from Reflection.Emit used by the DataContractSerializer's collection writer
- **Tracking Issue**: https://github.com/dotnet/runtime/issues/123011

### Affected Tests (15 total)
1. `DCS_ListGenericRoot`
2. `DCS_ListGenericMembers`
3. `DCS_WithListOfXElement`
4. `DCS_BaseClassAndDerivedClassWithSameProperty`
5. `DCS_TypeWithKnownTypeAttributeAndListOfInterfaceMember`
6. `DCS_InvalidDataContract_Write_And_Read_Empty_Collection_Of_Invalid_Type_Succeeds`
7. `DCS_CircularTypes_PreserveObjectReferences_True`
8. `DCS_CircularTypes_PreserveObjectReferences_False`
9. `DCS_CollectionOfTypeWithNonDefaultNamcespace`
10. `DCS_BasicPerSerializerRoundTripAndCompare_SampleTypes`
11. `DCS_BasicPerSerializerRoundTripAndCompare_Collections`
12. `DCS_BasicPerSerializerRoundTripAndCompare_CollectionDataContract`
13. `DCS_BasicPerSerializerRoundTripAndCompare_EnumStruct`
14. `DCS_TypeWithVirtualGenericProperty`
15. `DCS_TypeWithPrimitiveKnownTypes`

## Files Modified

### Test Files
- `src/libraries/System.Runtime.Serialization.Xml/tests/DataContractSerializer.cs`
  - Added `[ActiveIssue("https://github.com/dotnet/runtime/issues/123011", typeof(PlatformDetection), nameof(PlatformDetection.IsBrowser), nameof(PlatformDetection.IsCoreCLR))]` to 15 test methods

## Comparison with Mono Baseline
- Mono baseline: 340 run, 337 passed, 3 skipped
- CoreCLR: 327 run, 322 passed, 5 skipped (15 tests disabled via ActiveIssue)

## Failure Documentation
- [DataContractSerializerTests.BadImageFormatException.md](../failures/System.Runtime.Serialization.Xml.Tests/DataContractSerializerTests.BadImageFormatException.md)
