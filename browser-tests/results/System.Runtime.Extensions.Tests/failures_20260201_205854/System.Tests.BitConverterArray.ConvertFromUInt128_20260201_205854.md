# Test Failure: System.Tests.BitConverterArray.ConvertFromUInt128

**Timestamp:** 20260201_205854

**Full Name:** `System.Tests.BitConverterArray.ConvertFromUInt128`

**Failed Cases:** 1

---

### Case 1: `(num: 16777215, expected: [255, 255, 255, 0, 0, ···])`

**Error Message:**

```
Assert.Equal() Failure: Collections differ\nExpected:        null\nActual:   byte[] [0, 0, 0, 0, 0, ···]
```

**Stack Trace:**

```
at System.Tests.BitConverterArray.ConvertFromUInt128(UInt128 num, Byte[] expected)
   at System.RuntimeMethodHandle.InvokeMethod(ObjectHandleOnStack target, Void** arguments, ObjectHandleOnStack sig, BOOL isConstructor, ObjectHandleOnStack result)
   at System.RuntimeMethodHandle.InvokeMethod(Object target, Void** arguments, Signature sig, Boolean isConstructor)
   at System.Reflection.MethodBaseInvoker.InterpretedInvoke_Method(Object obj, IntPtr* args)
   at System.Reflection.MethodBaseInvoker.InvokeDirectByRefWithFewArgs(Object obj, Span`1 copyOfArgs, BindingFlags invokeAttr)
```

