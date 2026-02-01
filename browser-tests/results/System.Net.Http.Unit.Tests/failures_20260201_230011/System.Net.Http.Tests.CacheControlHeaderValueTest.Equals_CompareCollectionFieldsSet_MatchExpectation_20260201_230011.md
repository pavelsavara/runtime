# Test Failure: System.Net.Http.Tests.CacheControlHeaderValueTest.Equals_CompareCollectionFieldsSet_MatchExpectation

**Timestamp:** 20260201_230011

**Full Name:** `System.Net.Http.Tests.CacheControlHeaderValueTest.Equals_CompareCollectionFieldsSet_MatchExpectation`

**Failed Cases:** 1

---

### Case 1: ``

**Error Message:**

```
System.BadImageFormatException : An attempt was made to load a program with an incorrect format.\n (0x8007000B)
```

**Stack Trace:**

```
at System.Net.Http.Headers.HeaderUtilities.AreEqualCollections[T](ObjectCollection`1 x, ObjectCollection`1 y, IEqualityComparer`1 comparer)
   at System.Net.Http.Headers.CacheControlHeaderValue.Equals(Object obj)
   at System.Net.Http.Tests.CacheControlHeaderValueTest.CompareValues(CacheControlHeaderValue x, CacheControlHeaderValue y, Boolean areEqual)
   at System.Net.Http.Tests.CacheControlHeaderValueTest.Equals_CompareCollectionFieldsSet_MatchExpectation()
   at System.RuntimeMethodHandle.InvokeMethod(ObjectHandleOnStack target, Void** arguments, ObjectHandleOnStack sig, BOOL isConstructor, ObjectHandleOnStack result)
   at System.RuntimeMethodHandle.InvokeMethod(Object target, Void** arguments, Signature sig, Boolean isConstructor)
   at System.Reflection.MethodBaseInvoker.InterpretedInvoke_Method(Object obj, IntPtr* args)
   at System.Reflection.MethodBaseInvoker.InvokeWithNoArgs(Object obj, BindingFlags invokeAttr)
```

