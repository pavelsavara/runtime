# Test: DataContractSerializerTests - Multiple collection serialization tests (13 tests)

## Test Suite
System.Runtime.Serialization.Xml.Tests

## Failure Type
exception

## Exception Type
System.BadImageFormatException

## Stack Trace
```
System.BadImageFormatException : An attempt was made to load a program with an incorrect format.
 (0x8007000B)
   at System.Signature.Init(ObjectHandleOnStack _this, Void* pCorSig, Int32 cCorSig, RuntimeFieldHandleInternal fieldHandle, RuntimeMethodHandleInternal methodHandle)
   at System.Signature.Init(Void* pCorSig, Int32 cCorSig, RuntimeFieldHandleInternal fieldHandle, RuntimeMethodHandleInternal methodHandle)
   at System.Signature..ctor(IRuntimeMethodInfo methodHandle, RuntimeType declaringType)
   at System.Reflection.RuntimeMethodInfo.<get_Signature>g__LazyCreateSignature|27_0()
   at System.Reflection.RuntimeMethodInfo.FetchNonReturnParameters()
   at System.Reflection.RuntimeMethodInfo.GetParametersAsSpan()
   at System.RuntimeType.FilterApplyMethodBase(...)
   at System.Type.GetMethod(String name, BindingFlags bindingAttr, Type[] types)
   at System.Runtime.Serialization.XmlFormatWriterGenerator.CriticalHelper.WriteCollection(CollectionDataContract collectionContract)
```

## Affected Tests
1. DCS_CircularTypes_PreserveObjectReferences_False
2. DCS_InvalidDataContract_Write_And_Read_Empty_Collection_Of_Invalid_Type_Succeeds
3. DCS_BasicPerSerializerRoundTripAndCompare_CollectionDataContract
4. DCS_BasicPerSerializerRoundTripAndCompare_EnumStruct
5. DCS_TypeWithKnownTypeAttributeAndListOfInterfaceMember
6. DCS_WithListOfXElement
7. DCS_CollectionOfTypeWithNonDefaultNamcespace
8. DCS_ListGenericRoot
9. DCS_CircularTypes_PreserveObjectReferences_True
10. DCS_ListGenericMembers
11. DCS_BasicPerSerializerRoundTripAndCompare_Collections
12. DCS_BasicPerSerializerRoundTripAndCompare_SampleTypes
13. DCS_BaseClassAndDerivedClassWithSameProperty

## Notes
- Platform: Browser/WASM + CoreCLR
- Category: interpreter/reflection-emit
- 13 tests affected
- All failures occur in `XmlFormatWriterGenerator.CriticalHelper.WriteCollection` which uses Reflection.Emit to generate serialization code
- The generated IL signature appears to be invalid when parsed by CoreCLR interpreter
- Root cause: CoreCLR interpreter cannot handle certain Reflection.Emit'd method signatures with generic types
