# Test: System.Collections.Concurrent.Tests.ConcurrentQueueTests.ReferenceTypes_NulledAfterDequeue

## Test Suite
System.Collections.Concurrent.Tests

## Failure Type
assertion

## Exception Type
Assert.True() Failure

## Stack Trace
```
Assert.True() Failure
Expected: True
Actual:   False
   at System.Collections.Concurrent.Tests.ConcurrentQueueTests.ReferenceTypes_NulledAfterDequeue()
   at System.RuntimeMethodHandle.InvokeMethod(ObjectHandleOnStack target, Void** arguments, ObjectHandleOnStack sig, BOOL isConstructor, ObjectHandleOnStack result)
   at System.RuntimeMethodHandle.InvokeMethod(Object target, Void** arguments, Signature sig, Boolean isConstructor)
   at System.Reflection.MethodBaseInvoker.InterpretedInvoke_Method(Object obj, IntPtr* args)
   at System.Reflection.MethodBaseInvoker.InvokeWithNoArgs(Object obj, BindingFlags invokeAttr)
```

## Notes
- Platform: Browser/WASM + CoreCLR
- Category: finalizer
- This test verifies that reference types are properly nulled after dequeue, relying on finalizers being called after GC.
- Finalizers don't work on CoreCLR+Browser/WASM, which is a known limitation.
- The test is already skipped on Mono due to similar issues (mono/mono#16413).
