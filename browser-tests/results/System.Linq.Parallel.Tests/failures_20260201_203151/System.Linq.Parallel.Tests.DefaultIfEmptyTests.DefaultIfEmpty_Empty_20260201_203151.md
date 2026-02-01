# Test Failure: System.Linq.Parallel.Tests.DefaultIfEmptyTests.DefaultIfEmpty_Empty

**Timestamp:** 20260201_203151

**Full Name:** `System.Linq.Parallel.Tests.DefaultIfEmptyTests.DefaultIfEmpty_Empty`

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
   at System.Linq.Enumerable.TryGetFirst[TSource](IEnumerable`1 source, Func`2 predicate, Boolean& found)
   at System.Linq.Enumerable.FirstOrDefault[TSource](IEnumerable`1 source, Func`2 predicate)
```

