# Test Failure: System.Tests.BitConverterArray.ConvertFromInt128

**Timestamp:** 20260201_205854

**Full Name:** `System.Tests.BitConverterArray.ConvertFromInt128`

**Failed Cases:** 1

---

### Case 1: `(num: 0, expected: [0, 0, 0, 0, 0, ···])`

**Error Message:**

```
System.NullReferenceException : Object reference not set to an instance of an object.
```

**Stack Trace:**

```
at System.Tests.BitConverterArray.ConvertFromInt128(Int128 num, Byte[] expected)
   at System.RuntimeMethodHandle.InvokeMethod(ObjectHandleOnStack target, Void** arguments, ObjectHandleOnStack sig, BOOL isConstructor, ObjectHandleOnStack result)
   at System.RuntimeMethodHandle.InvokeMethod(Object target, Void** arguments, Signature sig, Boolean isConstructor)
   at System.Reflection.MethodBaseInvoker.InterpretedInvoke_Method(Object obj, IntPtr* args)
   at System.Reflection.MethodBaseInvoker.InvokeDirectByRefWithFewArgs(Object obj, Span`1 copyOfArgs, BindingFlags invokeAttr)
```

