# Test Failure: System.Net.Http.Tests.RangeParserTest.TryParse_SetOfValidValueStrings_ParsedCorrectly

**Timestamp:** 20260201_230011

**Full Name:** `System.Net.Http.Tests.RangeParserTest.TryParse_SetOfValidValueStrings_ParsedCorrectly`

**Failed Cases:** 1

---

### Case 1: ``

**Error Message:**

```
Assert.Equal() Failure: Exception thrown during comparison\nExpected: bytes=1-2\nActual:   bytes=1-2\n---- System.BadImageFormatException : An attempt was made to load a program with an incorrect format.\n (0x8007000B)
```

**Stack Trace:**

```
at System.Net.Http.Tests.RangeParserTest.CheckValidParsedValue(String input, Int32 startIndex, RangeHeaderValue expectedResult, Int32 expectedIndex)
   at System.Net.Http.Tests.RangeParserTest.TryParse_SetOfValidValueStrings_ParsedCorrectly()
   at System.RuntimeMethodHandle.InvokeMethod(ObjectHandleOnStack target, Void** arguments, ObjectHandleOnStack sig, BOOL isConstructor, ObjectHandleOnStack result)
   at System.RuntimeMethodHandle.InvokeMethod(Object target, Void** arguments, Signature sig, Boolean isConstructor)
   at System.Reflection.MethodBaseInvoker.InterpretedInvoke_Method(Object obj, IntPtr* args)
   at System.Reflection.MethodBaseInvoker.InvokeWithNoArgs(Object obj, BindingFlags invokeAttr)
----- Inner Stack Trace -----
   at System.Net.Http.Headers.HeaderUtilities.AreEqualCollections[T](ObjectCollection`1 x, ObjectCollection`1 y, IEqualityComparer`1 comparer)
   at System.Net.Http.Headers.HeaderUtilities.AreEqualCollections[T](ObjectCollection`1 x, ObjectCollection`1 y)
   at System.Net.Http.Headers.RangeHeaderValue.Equals(Object obj)
   at System.Object.Equals(Object objA, Object objB)
```

