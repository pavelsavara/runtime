# System.Runtime.Serialization.Json.ReflectionOnly.Tests - Browser/WASM CoreCLR Failures

## Summary
Multiple tests crash with `BadImageFormatException` in `System.Signature.Init` when serializing collections and other types using `DataContractJsonSerializer`. The test run also crashed completely with "table index is out of bounds" error.

## Root Cause
The `System.Signature.Init` method throws `BadImageFormatException` (0x8007000B) when parsing method signatures for reflection. This appears to be related to how the CoreCLR interpreter handles type signatures for generic types in collection serialization scenarios.

Additionally, there's a separate issue where nullable struct values are serialized incorrectly (outputting 0 values instead of expected values).

## Failing Tests

### BadImageFormatException Tests
1. `DataContractJsonSerializerTests.DCJS_ListGenericRoot`
2. `DataContractJsonSerializerTests.DCJS_VerifyDictionaryFormat`
3. `DataContractJsonSerializerTests.DCJS_UseSimpleDictionaryFormat`
4. `DataContractJsonSerializerTests.DCJS_SuspensionManager`
5. `DataContractJsonSerializerTests.DCJS_WithListOfXElement`
6. `DataContractJsonSerializerTests.DCJS_ListGenericMembers`

### Nullable Serialization Bug
7. `DataContractJsonSerializerTests.DCJS_Nullables`
   - Expected: `"Struct1":{"A":1,"B":2}`
   - Actual: `"Struct1":{"A":0,"B":0}`

## Stack Trace Example
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
   at System.RuntimeType.GetMethodCandidates(...)
   at System.Type.GetMethod(...)
   at System.Runtime.Serialization.DataContracts.CollectionDataContract.CollectionDataContractCriticalHelper.GetCollectionElementType()
```

## Crash
The test run crashed with:
```
DOTNET: Unhandled error: RuntimeError: table index is out of bounds
WASM EXIT 1
```

## Issue Reference
https://github.com/dotnet/runtime/issues/123011 - Browser+CoreCLR umbrella issue

## Date Discovered
2026-02-01
