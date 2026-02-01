# Test Failure: Microsoft.VisualBasic.Tests.DecimalTypeTests.Parse

**Timestamp:** 20260201_203445

**Full Name:** `Microsoft.VisualBasic.Tests.DecimalTypeTests.Parse`

**Failed Cases:** 1

---

### Case 1: `(value: \"123\", expected: 0)`

**Error Message:**

```
Assert.Equal() Failure: Values differ\nExpected: 0\nActual:   123
```

**Stack Trace:**

```
at Microsoft.VisualBasic.Tests.DecimalTypeTests.Parse(String value, Decimal expected)
   at System.RuntimeMethodHandle.InvokeMethod(ObjectHandleOnStack target, Void** arguments, ObjectHandleOnStack sig, BOOL isConstructor, ObjectHandleOnStack result)
   at System.RuntimeMethodHandle.InvokeMethod(Object target, Void** arguments, Signature sig, Boolean isConstructor)
   at System.Reflection.MethodBaseInvoker.InterpretedInvoke_Method(Object obj, IntPtr* args)
   at System.Reflection.MethodBaseInvoker.InvokeDirectByRefWithFewArgs(Object obj, Span`1 copyOfArgs, BindingFlags invokeAttr)
```

