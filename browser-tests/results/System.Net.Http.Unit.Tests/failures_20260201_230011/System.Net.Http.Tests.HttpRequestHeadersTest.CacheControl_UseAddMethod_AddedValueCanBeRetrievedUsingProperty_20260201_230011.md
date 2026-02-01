# Test Failure: System.Net.Http.Tests.HttpRequestHeadersTest.CacheControl_UseAddMethod_AddedValueCanBeRetrievedUsingProperty

**Timestamp:** 20260201_230011

**Full Name:** `System.Net.Http.Tests.HttpRequestHeadersTest.CacheControl_UseAddMethod_AddedValueCanBeRetrievedUsingProperty`

**Failed Cases:** 1

---

### Case 1: ``

**Error Message:**

```
Assert.Equal() Failure: Exception thrown during comparison\nExpected: public, must-revalidate, no-cache=\"token1, token2\", max-age=3, s-maxage=15\nActual:   public, must-revalidate, no-cache=\"token1, token2\", max-age=3, s-maxage=15\n---- System.BadImageFormatException : An attempt was made to load a program with an incorrect format.\n (0x8007000B)
```

**Stack Trace:**

```
at System.Net.Http.Tests.HttpRequestHeadersTest.CacheControl_UseAddMethod_AddedValueCanBeRetrievedUsingProperty()
   at System.RuntimeMethodHandle.InvokeMethod(ObjectHandleOnStack target, Void** arguments, ObjectHandleOnStack sig, BOOL isConstructor, ObjectHandleOnStack result)
   at System.RuntimeMethodHandle.InvokeMethod(Object target, Void** arguments, Signature sig, Boolean isConstructor)
   at System.Reflection.MethodBaseInvoker.InterpretedInvoke_Method(Object obj, IntPtr* args)
   at System.Reflection.MethodBaseInvoker.InvokeWithNoArgs(Object obj, BindingFlags invokeAttr)
----- Inner Stack Trace -----
   at System.Net.Http.Headers.HeaderUtilities.AreEqualCollections[T](ObjectCollection`1 x, ObjectCollection`1 y, IEqualityComparer`1 comparer)
   at System.Net.Http.Headers.CacheControlHeaderValue.Equals(Object obj)
   at System.Object.Equals(Object objA, Object objB)
```

