# COM / Interop Orphans in Browser-WASM Trimmed App

## Summary

The trimmed Browser-WASM `Wasm.Browser.Sample` retains several COM-specific and
Windows-interop types despite having **zero callers from application code**.
All reside in `System.Private.CoreLib`. There are **no Swift or ObjC interop**
types in the output.

| Type | Methods | Own Size | Max Transitive | External Callers |
|------|---------|----------|----------------|------------------|
| `ComVariant` | 3 | 1,133 B | 164,932 B | 0 |
| `ComVariant/Vector<T>` | 1 | 18 B | 163,076 B | 0 (only from ComVariant.Dispose) |
| `COMException` | 6 | 374 B | 217,441 B | 0 |
| `ExternalException` | 6 | 370 B | 217,130 B | 0 (only from COMException/SEHException) |
| `SEHException` | 4 | 66 B | 2,354 B | 0 |
| `InvalidOleVariantTypeException` | 4 | 191 B | 2,345 B | 0 |
| `DynamicInterfaceCastableHelpers` | 2 | 435 B | 163,459 B | 0 |
| `ComponentActivator` | 2 | 54 B | 61 B | 0 |
| `ComImportAttribute` | 1 | 7 B | 15 B | 1 (PseudoCustomAttribute) |
| `MarshalDirectiveException` | 4 | 176 B | 2,345 B | 1 (StubHelpers) |
| **Total** | **33** | **2,824 B** | | |

Own-size waste is modest (~2.8 KB), but **`ComVariant.Dispose()` alone
transitively reaches 2,986 methods (164 KB)** through
`Enum.HasFlag`, `Marshal.Release`, `Marshal.FreeBSTR`, etc.
`COMException.ToString()` transitively reaches 217 KB through
`Exception.get_StackTrace()`.

---

## Root Cause

### CoreCLR auto-generated ILLink.Descriptors.xml

The CoreCLR System.Private.CoreLib build runs a custom MSBuild task
`CreateRuntimeRootILLinkDescriptorFile` that parses C/C++ VM header files and
generates `ILLink.Descriptors.xml`, which gets embedded as a resource:

1. **`rexcep.h`** &rarr; `ProcessExceptionTypes()` &mdash; every
   `DEFINE_EXCEPTION` macro becomes a rooted `<type>` + `<method name=".ctor"/>`.
2. **`corelib.h`** &rarr; `ProcessMscorlib()` &mdash; every `DEFINE_CLASS`
   becomes a `<type preserve="nothing"/>` entry.

**The COMException/ExternalException/SEHException entries lack an
`#ifdef FEATURE_COMINTEROP` guard**, unlike `InvalidComObjectException` which IS
guarded:

```c
// rexcep.h — GUARDED (correct):
#ifdef FEATURE_COMINTEROP
DEFINE_EXCEPTION(g_InteropNS,  InvalidComObjectException,  false,  COR_E_INVALIDCOMOBJECT)
#endif

// rexcep.h — NOT GUARDED (all platforms including WASM):
DEFINE_EXCEPTION(g_InteropNS,  COMException,       false,  E_FAIL)
DEFINE_EXCEPTION(g_InteropNS,  ExternalException,  false,  E_FAIL)
DEFINE_EXCEPTION(g_InteropNS,  SEHException,       false,  E_FAIL)
```

Similarly in `corelib.h`, `ComVariant` is unconditionally included:

```c
// corelib.h — NOT GUARDED:
DEFINE_CLASS(COMVARIANT,  Marshalling,  ComVariant)
```

### Mono builds are clean

The Mono SPCL build does NOT use the CoreCLR header-parsing pipeline.
Its hand-maintained `ILLink.Descriptors.xml` does not mention COMException,
ExternalException, SEHException, or ComVariant. The Mono descriptor only roots
exception types that the Mono runtime actually constructs from native code
(e.g. `ArgumentException`, `NullReferenceException`).

### Shared descriptor quirks

[`ILLink.Descriptors.Shared.xml`](src/libraries/System.Private.CoreLib/src/ILLink/ILLink.Descriptors.Shared.xml)
unconditionally roots `ComponentActivator.GetFunctionPointer` "for a reasonable
error experience when using native hosting on a trimmed app". This is also
irrelevant for Browser WASM.

---

## Heaviest Orphan: `ComVariant.Dispose()`

The 1,037 B method body of `ComVariant.Dispose()` has a giant `switch`
over every VARIANT type. Selected callees:

| Callee | Transitive Size |
|--------|----------------|
| `Enum.HasFlag(Enum)` | 163,443 B |
| `ComVariant.GetRawDataRef()` | 163,300 B |
| `ComVariant/Vector<T>.AsSpan()` | 163,076 B |
| `Marshal.Release(IntPtr)` | 200 B |
| `Marshal.FreeCoTaskMem(IntPtr)` | 26 B |
| `Marshal.FreeBSTR(IntPtr)` | 20 B |

`GetRawDataRef()` itself calls `Type.op_Equality` (tSize 163,058 — the SCC),
so the entire super-SCC is reachable from this one orphan type.

---

## Heaviest Orphan: `COMException.ToString()`

```
COMException::ToString()  own=197B  tSize=217,441B
  -> Exception.get_StackTrace()           tSize=216,950B
  -> StringBuilder.AppendFormatted<T>(T)  tSize=163,345B
  -> Exception.get_Message()              tSize=163,123B
```

The `get_StackTrace()` callee pulls in the entire stack trace infrastructure
(even though StackTraceSupport is disabled — the method body still survives
because the type is rooted).

---

## `DynamicInterfaceCastableHelpers` (No External Callers)

Two methods (435 B own, 163 KB transitive) that call into the IsDefined
pipeline and type resolution:

```
GetInterfaceImplementation(IDynamicInterfaceCastable, RuntimeType)
  -> Type.IsAssignableTo(Type)          tSize=163,071B
  -> RuntimeType::IsDefined(Type,Bool)  tSize=163,058B
  -> SR.Format(String, Object, Object)  tSize=163,058B
```

Zero callers in the trimmed output.

---

## Proposed Fixes

### Fix 1 — Guard COM exceptions in `rexcep.h`

Wrap the three COM-specific exceptions in
`#ifdef FEATURE_COMINTEROP` / `#endif`:

```c
#ifdef FEATURE_COMINTEROP
DEFINE_EXCEPTION(g_InteropNS,  COMException,       false,  E_FAIL)
DEFINE_EXCEPTION(g_InteropNS,  ExternalException,  false,  E_FAIL)
DEFINE_EXCEPTION(g_InteropNS,  SEHException,       false,  E_FAIL)
#endif
```

`FEATURE_COMINTEROP` is already defined only on Windows, so this removes them
from all non-Windows (including WASM) ILLink descriptors. The
`CreateRuntimeRootILLinkDescriptorFile` task already handles `#ifdef` via its
`DefineTracker`.

**Risk**: Low. These exceptions can never be thrown on WASM.
`ExternalException.ToString()` is overridden by `COMException`, so no
virtual-dispatch concern.

### Fix 2 — Guard `ComVariant` in `corelib.h`

```c
#ifdef FEATURE_COMINTEROP
DEFINE_CLASS(COMVARIANT,  Marshalling,  ComVariant)
#endif
```

**Risk**: Low. `ComVariant` wraps the Win32 `VARIANT` type; it has no
meaning on non-Windows platforms.

### Fix 3 — Guard `ComponentActivator` and `DynamicInterfaceCastableHelpers`

For `ComponentActivator`, the shared descriptor already has a feature check
(`EnableConsumingManagedCodeFromNativeHosting`), but `GetFunctionPointer` is
always rooted. On WASM this is unnecessary.

`DynamicInterfaceCastableHelpers` is rooted by the runtime's casting
infrastructure. If it has no callers in the linker output, it may be coming
from a runtime descriptor or from the type being referenced in metadata.
This needs further investigation.

### Fix 4 — Guard `InvalidOleVariantTypeException` and `MarshalDirectiveException`

These are also generated from unguarded `DEFINE_EXCEPTION` entries in
`rexcep.h`:

```c
DEFINE_EXCEPTION(g_InteropNS,  InvalidOleVariantTypeException, false, COR_E_INVALIDOLEVARIANTTYPE)
DEFINE_EXCEPTION(g_InteropNS,  MarshalDirectiveException,      false, COR_E_MARSHALDIRECTIVE)
```

Both could be wrapped in `#ifndef TARGET_WASM` or
`#ifdef FEATURE_COMINTEROP`, depending on whether they're needed on
non-Windows desktop platforms (Linux/macOS). `MarshalDirectiveException`
has one caller (`StubHelpers.CheckStringLength`) that may need
investigation.

---

## Impact Estimate

| Fix | Own Bytes Saved | Orphan Methods Removed | Transitive Reach Eliminated |
|-----|----------------|----------------------|----------------------------|
| Guard COM exceptions | 810 B | 16 | COMException.ToString (217 KB transitive) |
| Guard ComVariant | 1,151 B | 4 | ComVariant.Dispose (164 KB transitive) |
| Guard ComponentActivator | 54 B | 2 | Minimal (61 B transitive) |
| Guard OleVariant+Marshal exceptions | 367 B | 8 | ~4.7 KB transitive |
| **Total** | **~2.4 KB** | **30** | Eliminates several phantom SCC entries |

The direct byte savings are small, but removing these phantom roots improves
the linker's ability to trim deeper — especially `ComVariant.Dispose()` and
`COMException.ToString()` which transitively pull in the super-SCC and the
stack trace machinery.
