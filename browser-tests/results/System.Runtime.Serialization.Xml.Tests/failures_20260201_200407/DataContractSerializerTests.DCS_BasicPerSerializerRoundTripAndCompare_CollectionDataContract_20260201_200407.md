# Test Failure: DataContractSerializerTests.DCS_BasicPerSerializerRoundTripAndCompare_CollectionDataContract

**Timestamp:** 20260201_200407

**Full Name:** `DataContractSerializerTests.DCS_BasicPerSerializerRoundTripAndCompare_CollectionDataContract`

**Failed Cases:** 1

---

### Case 1: ``

**Error Message:**

```
System.BadImageFormatException : An attempt was made to load a program with an incorrect format.\n (0x8007000B)
```

**Stack Trace:**

```
at System.Signature.Init(ObjectHandleOnStack _this, Void* pCorSig, Int32 cCorSig, RuntimeFieldHandleInternal fieldHandle, RuntimeMethodHandleInternal methodHandle)
   at System.Signature.Init(Void* pCorSig, Int32 cCorSig, RuntimeFieldHandleInternal fieldHandle, RuntimeMethodHandleInternal methodHandle)
   at System.Signature..ctor(IRuntimeMethodInfo methodHandle, RuntimeType declaringType)
   at System.Reflection.RuntimeMethodInfo.<get_Signature>g__LazyCreateSignature|27_0()
   at System.Reflection.RuntimeMethodInfo.FetchNonReturnParameters()
   at System.Reflection.RuntimeMethodInfo.GetParametersAsSpan()
   at System.RuntimeType.FilterApplyMethodBase(MethodBase methodBase, BindingFlags methodFlags, BindingFlags bindingFlags, CallingConventions callConv, Type[] argumentTypes)
   at System.RuntimeType.FilterApplyMethodInfo(RuntimeMethodInfo method, BindingFlags bindingFlags, CallingConventions callConv, Type[] argumentTypes)
   at System.RuntimeType.GetMethodCandidates(String name, Int32 genericParameterCount, BindingFlags bindingAttr, CallingConventions callConv, Type[] types, Boolean allowPrefixLookup)
   at System.RuntimeType.GetMethodImplCommon(String name, Int32 genericParameterCount, BindingFlags bindingAttr, Binder binder, CallingConventions callConv, Type[] types, ParameterModifier[] modifiers)
   at System.RuntimeType.GetMethodImpl(String name, BindingFlags bindingAttr, Binder binder, CallingConventions callConv, Type[] types, ParameterModifier[] modifiers)
   at System.Type.GetMethod(String name, BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers)
   at System.Type.GetMethod(String name, BindingFlags bindingAttr, Binder binder, Type[] types, ParameterModifier[] modifiers)
   at System.Type.GetMethod(String name, BindingFlags bindingAttr, Type[] types)
   at System.Runtime.Serialization.XmlFormatWriterGenerator.CriticalHelper.WriteCollection(CollectionDataContract collectionContract)
   at System.Runtime.Serialization.XmlFormatWriterGenerator.CriticalHelper.GenerateCollectionWriter(CollectionDataContract collectionContract)
   at System.Runtime.Serialization.XmlFormatWriterGenerator.GenerateCollectionWriter(CollectionDataContract collectionContract)
   at System.Runtime.Serialization.DataContracts.CollectionDataContract.CreateXmlFormatWriterDelegate()
   at System.Runtime.Serialization.DataContracts.CollectionDataContract.get_XmlFormatWriterDelegate()
   at System.Runtime.Serialization.DataContracts.CollectionDataContract.WriteXmlValue(XmlWriterDelegator xmlWriter, Object obj, XmlObjectSerializerWriteContext context)
   at System.Runtime.Serialization.XmlObjectSerializerWriteContext.WriteDataContractValue(DataContract dataContract, XmlWriterDelegator xmlWriter, Object obj, RuntimeTypeHandle declaredTypeHandle)
   at System.Runtime.Serialization.XmlObjectSerializerWriteContext.SerializeWithoutXsiType(DataContract dataContract, XmlWriterDelegator xmlWriter, Object obj, RuntimeTypeHandle declaredTypeHandle)
   at System.Runtime.Serialization.XmlObjectSerializerWriteContext.InternalSerialize(XmlWriterDelegator xmlWriter, Object obj, Boolean isDeclaredType, Boolean writeXsiType, Int32 declaredTypeID, RuntimeTypeHandle declaredTypeHandle)
   at System.Runtime.Serialization.XmlObjectSerializerWriteContext.InternalSerializeReference(XmlWriterDelegator xmlWriter, Object obj, Boolean isDeclaredType, Boolean writeXsiType, Int32 declaredTypeID, RuntimeTypeHandle declaredTypeHandle)
   at WriteDMInCollection2ToXml(XmlWriterDelegator, Object, XmlObjectSerializerWriteContext, ClassDataContract)
   at System.Runtime.Serialization.DataContracts.ClassDataContract.WriteXmlValue(XmlWriterDelegator xmlWriter, Object obj, XmlObjectSerializerWriteContext context)
   at System.Runtime.Serialization.XmlObjectSerializerWriteContext.WriteDataContractValue(DataContract dataContract, XmlWriterDelegator xmlWriter, Object obj, RuntimeTypeHandle declaredTypeHandle)
   at System.Runtime.Serialization.XmlObjectSerializerWriteContext.SerializeAndVerifyType(DataContract dataContract, XmlWriterDelegator xmlWriter, Object obj, Boolean verifyKnownType, RuntimeTypeHandle declaredTypeHandle, Type declaredType)
   at System.Runtime.Serialization.XmlObjectSerializerWriteContext.SerializeWithXsiType(XmlWriterDelegator xmlWriter, Object obj, RuntimeTypeHandle objectTypeHandle, Type objectType, Int32 declaredTypeID, RuntimeTypeHandle declaredTypeHandle, Type declaredType)
   at System.Runtime.Serialization.XmlObjectSerializerWriteContext.InternalSerialize(XmlWriterDelegator xmlWriter, Object obj, Boolean isDeclaredType, Boolean writeXsiType, Int32 declaredTypeID, RuntimeTypeHandle declaredTypeHandle)
   at System.Runtime.Serialization.XmlObjectSerializerWriteContext.InternalSerializeReference(XmlWriterDelegator xmlWriter, Object obj, Boolean isDeclaredType, Boolean writeXsiType, Int32 declaredTypeID, RuntimeTypeHandle declaredTypeHandle)
   at WriteObjectContainerToXml(XmlWriterDelegator, Object, XmlObjectSerializerWriteContext, ClassDataContract)
   at System.Runtime.Serialization.DataContracts.ClassDataContract.WriteXmlValue(XmlWriterDelegator xmlWriter, Object obj, XmlObjectSerializerWriteContext context)
   at System.Runtime.Serialization.XmlObjectSerializerWriteContext.WriteDataContractValue(DataContract dataContract, XmlWriterDelegator xmlWriter, Object obj, RuntimeTypeHandle declaredTypeHandle)
   at System.Runtime.Serialization.XmlObjectSerializerWriteContext.SerializeWithoutXsiType(DataContract dataContract, XmlWriterDelegator xmlWriter, Object obj, RuntimeTypeHandle declaredTypeHandle)
   at System.Runtime.Serialization.DataContractSerializer.InternalWriteObjectContent(XmlWriterDelegator writer, Object graph, DataContractResolver dataContractResolver)
   at System.Runtime.Serialization.DataContractSerializer.InternalWriteObject(XmlWriterDelegator writer, Object graph, DataContractResolver dataContractResolver)
   at System.Runtime.Serialization.XmlObjectSerializer.WriteObjectHandleExceptions(XmlWriterDelegator writer, Object graph, DataContractResolver dataContractResolver)
   at System.Runtime.Serialization.XmlObjectSerializer.WriteObjectHandleExceptions(XmlWriterDelegator writer, Object graph)
   at System.Runtime.Serialization.XmlObjectSerializer.WriteObject(XmlDictionaryWriter writer, Object graph)
   at System.Runtime.Serialization.XmlObjectSerializer.WriteObject(Stream stream, Object graph)
   at System.Runtime.Serialization.Tests.DataContractSerializerHelper.SerializeAndDeserialize[T](T value, String baseline, DataContractSerializerSettings settings, Func`1 serializerFactory, Boolean skipStringCompare, Boolean verifyBinaryRoundTrip)
   at DataContractSerializerTests.TestObjectInObjectContainerWithSimpleResolver[T](T o, String baseline, Boolean skipStringCompare)
   at DataContractSerializerTests.DCS_BasicPerSerializerRoundTripAndCompare_CollectionDataContract()
   at System.RuntimeMethodHandle.InvokeMethod(ObjectHandleOnStack target, Void** arguments, ObjectHandleOnStack sig, BOOL isConstructor, ObjectHandleOnStack result)
   at System.RuntimeMethodHandle.InvokeMethod(Object target, Void** arguments, Signature sig, Boolean isConstructor)
   at System.Reflection.MethodBaseInvoker.InterpretedInvoke_Method(Object obj, IntPtr* args)
   at System.Reflection.MethodBaseInvoker.InvokeWithNoArgs(Object obj, BindingFlags invokeAttr)
```

