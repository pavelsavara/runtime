# Test Failure: System.Tests.MathTests.Clamp_Decimal

**Timestamp:** 20260201_205854

**Full Name:** `System.Tests.MathTests.Clamp_Decimal`

**Failed Cases:** 1

---

### Case 1: `(value: 0, min: -1, max: 1, expected: -1)`

**Error Message:**

```
Assert.Equal() Failure: Values differ\nExpected: -1\nActual:   0
```

**Stack Trace:**

```
at System.Tests.MathTests.Clamp_Decimal(Decimal value, Decimal min, Decimal max, Decimal expected)
   at System.RuntimeMethodHandle.InvokeMethod(ObjectHandleOnStack target, Void** arguments, ObjectHandleOnStack sig, BOOL isConstructor, ObjectHandleOnStack result)
   at System.RuntimeMethodHandle.InvokeMethod(Object target, Void** arguments, Signature sig, Boolean isConstructor)
   at System.Reflection.MethodBaseInvoker.InterpretedInvoke_Method(Object obj, IntPtr* args)
   at System.Reflection.MethodBaseInvoker.InvokeDirectByRefWithFewArgs(Object obj, Span`1 copyOfArgs, BindingFlags invokeAttr)
```

