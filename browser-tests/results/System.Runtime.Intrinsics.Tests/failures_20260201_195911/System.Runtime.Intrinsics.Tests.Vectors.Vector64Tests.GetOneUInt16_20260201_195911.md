# Test Failure: System.Runtime.Intrinsics.Tests.Vectors.Vector64Tests.GetOneUInt16

**Timestamp:** 20260201_195911

**Full Name:** `System.Runtime.Intrinsics.Tests.Vectors.Vector64Tests.GetOneUInt16`

**Failed Cases:** 1

---

### Case 1: ``

**Error Message:**

```
Assert.Equal() Failure: Values differ\nExpected: <0, 0, 0, 0>\nActual:   <1, 1, 1, 1>
```

**Stack Trace:**

```
at System.Runtime.Intrinsics.Tests.Vectors.Vector64Tests.TestGetOne[T]()
   at System.Runtime.Intrinsics.Tests.Vectors.Vector64Tests.GetOneUInt16()
   at System.RuntimeMethodHandle.InvokeMethod(ObjectHandleOnStack target, Void** arguments, ObjectHandleOnStack sig, BOOL isConstructor, ObjectHandleOnStack result)
   at System.RuntimeMethodHandle.InvokeMethod(Object target, Void** arguments, Signature sig, Boolean isConstructor)
   at System.Reflection.MethodBaseInvoker.InterpretedInvoke_Method(Object obj, IntPtr* args)
   at System.Reflection.MethodBaseInvoker.InvokeWithNoArgs(Object obj, BindingFlags invokeAttr)
```

