# Test Failure: System.Net.Http.Tests.CacheControlHeaderValueTest.Clone_Call_CloneFieldsMatchSourceFields

**Timestamp:** 20260201_230011

**Full Name:** `System.Net.Http.Tests.CacheControlHeaderValueTest.Clone_Call_CloneFieldsMatchSourceFields`

**Failed Cases:** 1

---

### Case 1: ``

**Error Message:**

```
Assert.Equal() Failure: Exception thrown during comparison\nExpected: no-store, no-transform, only-if-cached, public, must-revalidate, proxy-revalidate, no-cache=\"token1\", max-age=3661, s-maxage=3664, max-stale=3662, min-fresh=3663, private=\"token2\", custom, customN=customV\nActual:   no-store, no-transform, only-if-cached, public, must-revalidate, proxy-revalidate, no-cache=\"token1\", max-age=3661, s-maxage=3664, max-stale=3662, min-fresh=3663, private=\"token2\", custom, customN=customV\n---- System.BadImageFormatException : An attempt was made to load a program with an incorrect format.\n (0x8007000B)
```

**Stack Trace:**

```
at System.Net.Http.Tests.CacheControlHeaderValueTest.Clone_Call_CloneFieldsMatchSourceFields()
   at System.RuntimeMethodHandle.InvokeMethod(ObjectHandleOnStack target, Void** arguments, ObjectHandleOnStack sig, BOOL isConstructor, ObjectHandleOnStack result)
   at System.RuntimeMethodHandle.InvokeMethod(Object target, Void** arguments, Signature sig, Boolean isConstructor)
   at System.Reflection.MethodBaseInvoker.InterpretedInvoke_Method(Object obj, IntPtr* args)
   at System.Reflection.MethodBaseInvoker.InvokeWithNoArgs(Object obj, BindingFlags invokeAttr)
----- Inner Stack Trace -----
   at System.Net.Http.Headers.HeaderUtilities.AreEqualCollections[T](ObjectCollection`1 x, ObjectCollection`1 y, IEqualityComparer`1 comparer)
   at System.Net.Http.Headers.CacheControlHeaderValue.Equals(Object obj)
   at System.Object.Equals(Object objA, Object objB)
```

