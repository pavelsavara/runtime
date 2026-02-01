# GenericVectorTests Reflection Tests - BadImageFormatException

## Summary
- **Test File**: `src/libraries/System.Numerics.Vectors/tests/GenericVectorTests.cs`
- **Affected Tests**: 50 tests with "Reflection" in their names
- **Platform**: Browser/WASM + CoreCLR (interpreter mode)
- **Tracking Issue**: https://github.com/dotnet/runtime/issues/123011

## Root Cause

All 50 failing tests use reflection to invoke methods on `Vector<T>` types. The failure occurs when calling `RuntimeMethodHandle.GetUtf8Name` during reflection:

```
System.BadImageFormatException : Format of the executable (.exe) or library (.dll) is invalid.
   at System.RuntimeMethodHandle.GetUtf8Name(RuntimeMethodHandleInternal method)
   at System.RuntimeMethodHandle.GetName(RuntimeMethodHandleInternal method)
   at System.Reflection.RuntimeMethodInfo.get_Name()
   at System.Reflection.TypeInfo.GetDeclaredMethods(String name)+MoveNext()
```

The CoreCLR interpreter cannot properly handle `RuntimeMethodHandle.GetUtf8Name` for dynamically generated/intrinsic `Vector<T>` methods.

## Affected Test Methods (50 total)

### Arithmetic Operations (40 tests)
- AdditionReflection{Byte,SByte,Int16,UInt16,Int32,UInt32,Int64,UInt64,Single,Double}
- DivisionReflection{Byte,SByte,Int16,UInt16,Int32,UInt32,Int64,UInt64,Single,Double}
- MultiplicationReflection{Byte,SByte,Int16,UInt16,Int32,UInt32,Int64,UInt64,Single,Double}

### CopyTo Operations (10 tests)
- CopyToReflection{Byte,SByte,Int32,Int64,UInt16,UInt32,UInt64,Single,Double}
- CopyToWithOffsetReflection{Byte,SByte,Int16,Int64,UInt32,UInt64,Single,Double}

### Conversion Operations (3 tests)
- ConvertInt64ToDoubleWithReflection
- ConvertUInt32ToSingleWithReflection
- ConvertUInt64ToDoubleWithReflection

## Common Pattern

All failing tests use this pattern:
```csharp
var method = typeof(Vector<T>).GetTypeInfo().GetDeclaredMethods("op_Addition")
    .Where(mi => ...).Single();
```

The failure occurs during `GetDeclaredMethods()` when the runtime attempts to retrieve method names via reflection.

## Workaround

Add `[ActiveIssue]` attributes to all 50 affected test methods:
```csharp
[ActiveIssue("https://github.com/dotnet/runtime/issues/123011", typeof(PlatformDetection), nameof(PlatformDetection.IsBrowser), nameof(PlatformDetection.IsCoreCLR))]
```
