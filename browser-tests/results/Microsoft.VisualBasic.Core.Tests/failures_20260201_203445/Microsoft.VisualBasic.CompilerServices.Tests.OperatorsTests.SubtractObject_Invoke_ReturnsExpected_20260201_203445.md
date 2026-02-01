# Test Failure: Microsoft.VisualBasic.CompilerServices.Tests.OperatorsTests.SubtractObject_Invoke_ReturnsExpected

**Timestamp:** 20260201_203445

**Full Name:** `Microsoft.VisualBasic.CompilerServices.Tests.OperatorsTests.SubtractObject_Invoke_ReturnsExpected`

**Failed Cases:** 2

---

### Case 1: `(left: null, right: 0001-01-01T00:00:00.0000010, expected: -00:00:00.0000010)`

**Error Message:**

```
Assert.Equal() Failure: Values differ\nExpected: -00:00:00.0000010\nActual:   00:00:00
```

**Stack Trace:**

```
at Microsoft.VisualBasic.CompilerServices.Tests.OperatorsTests.SubtractObject_Invoke_ReturnsExpected(Object left, Object right, Object expected)
   at InvokeStub_OperatorsTests.SubtractObject_Invoke_ReturnsExpected(Object, Span`1)
   at System.Reflection.MethodBaseInvoker.InvokeWithFewArgs(Object obj, BindingFlags invokeAttr, Binder binder, Object[] parameters, CultureInfo culture)
```

---

### Case 2: `(left: 0001-01-01T00:00:00.0000010, right: 00:00:00.0000005, expected: 0001-01-01T00:00:00.0000005)`

**Error Message:**

```
Assert.Equal() Failure: Values differ\nExpected: 0001-01-01T00:00:00.0000005\nActual:   0001-01-01T00:00:00.0000000
```

**Stack Trace:**

```
at Microsoft.VisualBasic.CompilerServices.Tests.OperatorsTests.SubtractObject_Invoke_ReturnsExpected(Object left, Object right, Object expected)
   at InvokeStub_OperatorsTests.SubtractObject_Invoke_ReturnsExpected(Object, Span`1)
   at System.Reflection.MethodBaseInvoker.InvokeWithFewArgs(Object obj, BindingFlags invokeAttr, Binder binder, Object[] parameters, CultureInfo culture)
```

