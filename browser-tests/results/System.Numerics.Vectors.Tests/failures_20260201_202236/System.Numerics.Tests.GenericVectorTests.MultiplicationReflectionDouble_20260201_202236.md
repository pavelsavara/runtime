# Test Failure: System.Numerics.Tests.GenericVectorTests.MultiplicationReflectionDouble

**Timestamp:** 20260201_202236

**Full Name:** `System.Numerics.Tests.GenericVectorTests.MultiplicationReflectionDouble`

**Failed Cases:** 1

---

### Case 1: ``

**Error Message:**

```
System.BadImageFormatException : Format of the executable (.exe) or library (.dll) is invalid.
```

**Stack Trace:**

```
at System.RuntimeMethodHandle.GetUtf8Name(RuntimeMethodHandleInternal method)
   at System.RuntimeMethodHandle.GetName(RuntimeMethodHandleInternal method)
   at System.RuntimeMethodHandle.GetName(IRuntimeMethodInfo method)
   at System.Reflection.RuntimeMethodInfo.get_Name()
   at System.Reflection.TypeInfo.GetDeclaredMethods(String name)+MoveNext()
   at System.Linq.Enumerable.IEnumerableWhereIterator`1.MoveNext()
   at System.Linq.Enumerable.TryGetSingle[TSource](IEnumerable`1 source, Boolean& found)
   at System.Linq.Enumerable.Single[TSource](IEnumerable`1 source)
   at System.Numerics.Tests.GenericVectorTests.TestMultiplicationReflection[T]()
   at System.Numerics.Tests.GenericVectorTests.MultiplicationReflectionDouble()
   at System.RuntimeMethodHandle.InvokeMethod(ObjectHandleOnStack target, Void** arguments, ObjectHandleOnStack sig, BOOL isConstructor, ObjectHandleOnStack result)
   at System.RuntimeMethodHandle.InvokeMethod(Object target, Void** arguments, Signature sig, Boolean isConstructor)
   at System.Reflection.MethodBaseInvoker.InterpretedInvoke_Method(Object obj, IntPtr* args)
   at System.Reflection.MethodBaseInvoker.InvokeWithNoArgs(Object obj, BindingFlags invokeAttr)
```

