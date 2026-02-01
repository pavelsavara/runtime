# Test Failure: System.Numerics.Tests.GenericVectorTests.DivisionReflectionInt16

**Timestamp:** 20260201_202236

**Full Name:** `System.Numerics.Tests.GenericVectorTests.DivisionReflectionInt16`

**Failed Cases:** 1

---

### Case 1: ``

**Error Message:**

```
Assert.Equal() Failure: Values differ\nExpected: 2\nActual:   0
```

**Stack Trace:**

```
at System.Numerics.Tests.GenericVectorTests.<>c__DisplayClass764_0`1.<TestDivisionReflection>b__3(Int32 index, T val)
   at System.Numerics.Tests.GenericVectorTests.ValidateVector[T](Vector`1 vector, Action`2 indexValidationFunc)
   at System.Numerics.Tests.GenericVectorTests.TestDivisionReflection[T]()
   at System.Numerics.Tests.GenericVectorTests.DivisionReflectionInt16()
   at System.RuntimeMethodHandle.InvokeMethod(ObjectHandleOnStack target, Void** arguments, ObjectHandleOnStack sig, BOOL isConstructor, ObjectHandleOnStack result)
   at System.RuntimeMethodHandle.InvokeMethod(Object target, Void** arguments, Signature sig, Boolean isConstructor)
   at System.Reflection.MethodBaseInvoker.InterpretedInvoke_Method(Object obj, IntPtr* args)
   at System.Reflection.MethodBaseInvoker.InvokeWithNoArgs(Object obj, BindingFlags invokeAttr)
```

