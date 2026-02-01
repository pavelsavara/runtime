# Test Failure: System.Runtime.Intrinsics.Wasm.Tests.PackedSimdTests.SaturatingArithmeticSignedTest

**Timestamp:** 20260201_190551

**Full Name:** `System.Runtime.Intrinsics.Wasm.Tests.PackedSimdTests.SaturatingArithmeticSignedTest`

**Failed Cases:** 1

---

### Case 1: ``

**Error Message:**

```
System.PlatformNotSupportedException : Operation is not supported on this platform.
```

**Stack Trace:**

```
at System.Runtime.Intrinsics.Wasm.PackedSimd.AddSaturate(Vector128`1 left, Vector128`1 right)
   at System.Runtime.Intrinsics.Wasm.Tests.PackedSimdTests.SaturatingArithmeticSignedTest()
   at System.RuntimeMethodHandle.InvokeMethod(ObjectHandleOnStack target, Void** arguments, ObjectHandleOnStack sig, BOOL isConstructor, ObjectHandleOnStack result)
   at System.RuntimeMethodHandle.InvokeMethod(Object target, Void** arguments, Signature sig, Boolean isConstructor)
   at System.Reflection.MethodBaseInvoker.InterpretedInvoke_Method(Object obj, IntPtr* args)
   at System.Reflection.MethodBaseInvoker.InvokeWithNoArgs(Object obj, BindingFlags invokeAttr)
```

