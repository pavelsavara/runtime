# StackTrace.IsSupported — Underutilized Feature Switch

## Summary

`System.Diagnostics.StackTrace.IsSupported` is a linker-recognized feature switch (`FeatureSwitchDefinition`) that defaults to **true**. When set to **false** via the MSBuild property `<StackTraceSupport>false</StackTraceSupport>`, the ILLink *can* substitute the property to return constant `false` and trim dead code behind it.

**The problem:** today the switch only guards **two** call sites — both inside `DiagnosticMethodInfo`. The main cost center — `Exception.StackTrace` → `new StackTrace()` → Reflection — is **completely ungated**. This means setting `StackTraceSupport=false` barely saves anything for Browser WASM apps.

## Current State of the Switch

### Definition (StackTrace.cs, line 20–21)

```csharp
[FeatureSwitchDefinition("System.Diagnostics.StackTrace.IsSupported")]
internal static bool IsSupported { get; } =
    AppContext.TryGetSwitch("System.Diagnostics.StackTrace.IsSupported",
        out bool isSupported) ? isSupported : true;
```

### Where it is checked (only 2 sites)

Both are in `DiagnosticMethodInfo`:

```csharp
// DiagnosticMethodInfo.cs:54
public static DiagnosticMethodInfo? Create(Delegate @delegate)
{
    if (!StackTrace.IsSupported)
        return null;
    return new DiagnosticMethodInfo(@delegate.Method);
}

// DiagnosticMethodInfo.cs:73
public static DiagnosticMethodInfo? Create(StackFrame frame)
{
    if (!StackTrace.IsSupported)
        return null;
    MethodBase? method = frame.GetMethod();
    ...
}
```

### Where it is NOT checked (the expensive paths)

**`Exception.StackTrace` getter** (Exception.cs, line 208–228):

```csharp
public virtual string? StackTrace
{
    get
    {
        ...
        return remoteStackTraceString + GetStackTrace();
    }
}

private string GetStackTrace()
{
    return new StackTrace(this, fNeedFileInfo: true)
        .ToString(StackTrace.TraceFormat.Normal);
}
```

**`Exception.SetCurrentStackTrace()`** (Exception.cs, line 236):

```csharp
internal void SetCurrentStackTrace()
{
    ...
    new StackTrace(fNeedFileInfo: true)
        .ToString(Diagnostics.StackTrace.TraceFormat.TrailingNewLine, sb);
    ...
}
```

Neither path checks `StackTrace.IsSupported`. This means **every thrown exception** whose stack trace is accessed will pull in the full StackTrace → Reflection dependency chain, regardless of the feature switch.

## The Dependency Chain

`StackTrace.ToString(TraceFormat, StringBuilder)` is the single method that couples stack traces to the Reflection system. It calls:

| Reflection API | Purpose in ToString |
|---|---|
| `StackFrame.GetMethod()` → `MethodBase` | Get method for each frame |
| `mb.DeclaringType` | Print `Namespace.Type.` prefix |
| `mb.Name` | Print method name |
| `mb is MethodInfo mi && mi.IsGenericMethod` | Check for generic methods |
| `mi.GetGenericArguments()` | Print `[T1,T2]` type params |
| `mb.GetParametersAsSpan()` | Print `(Type1 name1, ...)` params |
| `pi[j].ParameterType.Name` | Parameter type names |
| `declaringType.IsAssignableTo(typeof(IAsyncStateMachine))` | Async state machine detection |
| `IsDefinedSafe(declaringType, typeof(CompilerGeneratedAttribute))` | Filter compiler-generated types |
| `mb.ReflectedType.Module.ScopeName` | Assembly name for IL offsets |
| `mb.MetadataToken` | Token for IL offset display |

This pulls in essentially the entire `RuntimeType` / `RuntimeMethodInfo` / `RuntimeParameterInfo` family — which forms the core of the 934-method super-SCC.

### Impact by the Numbers (from method-cost analysis)

- **42 StackTrace/StackFrame methods** exist in the published app
- **0 of them are inside the SCC** — they sit outside it
- **Forward-reachable from StackTrace into the SCC**: 934 methods (i.e., StackTrace can reach the entire SCC)
- `StackTrace.ToString(TraceFormat, StringBuilder)` has **transitive size = 216,950 bytes**
- Only **3 methods** are exclusively reachable through StackTrace and not through other paths: `StackFrame.ToString()`, `StackTrace.ToString()`, `StackTraceHiddenAttribute..ctor()` — totalling 328 bytes of IL

The 3 exclusive methods mean StackTrace alone doesn't uniquely pull in much code that nothing else uses. However, StackTrace is one of the **major consumers** that keeps the SCC reachable. Eliminating it reduces the number of entry points into the SCC, making it easier for other cuts (e.g., `CustomAttribute.IsDefined`, `Enum` formatting) to render the SCC fully unreachable.

## MSBuild Wiring

The MSBuild property `StackTraceSupport` maps to the runtime switch via the .NET SDK targets:

```xml
<!-- Microsoft.NET.Sdk.targets -->
<RuntimeHostConfigurationOption
    Include="System.Diagnostics.StackTrace.IsSupported"
    Condition="'$(StackTraceSupport)' != ''"
    Value="$(StackTraceSupport)"
    Trim="true" />
```

The `Trim="true"` attribute tells ILLink to substitute the `FeatureSwitchDefinition`-annotated property at link time. When `StackTraceSupport=false`, the linker replaces `StackTrace.IsSupported` with constant `false`, enabling dead-code elimination.

The Wasm.Browser.Sample already sets `<StackTraceSupport>false</StackTraceSupport>`, but because `Exception.GetStackTrace()` and `SetCurrentStackTrace()` don't check the switch, the linker cannot trim the StackTrace→Reflection chain.

## The Opportunity

Gate the heavyweight paths behind `StackTrace.IsSupported`:

```csharp
// Exception.cs — proposed change
private string GetStackTrace()
{
    if (!StackTrace.IsSupported)
        return ""; // or a fixed message like "<stack trace unavailable>"

    return new StackTrace(this, fNeedFileInfo: true)
        .ToString(Diagnostics.StackTrace.TraceFormat.Normal);
}

internal void SetCurrentStackTrace()
{
    if (!CanSetRemoteStackTrace())
        return;

    if (!StackTrace.IsSupported)
        return;

    var sb = new StringBuilder(256);
    new StackTrace(fNeedFileInfo: true)
        .ToString(Diagnostics.StackTrace.TraceFormat.TrailingNewLine, sb);
    sb.AppendLine(SR.Exception_EndStackTraceFromPreviousThrow);
    _remoteStackTraceString = sb.ToString();
}
```

### Expected Impact

When `StackTraceSupport=false` **and** these guards are added:

1. **ILLink can eliminate** `StackTrace.ToString(TraceFormat, StringBuilder)` and its ~216 KB transitive closure
2. **One fewer major entry point** into the 934-method Reflection SCC
3. **Combined with other cuts** (e.g., `CustomAttribute.IsDefined`, `Enum` formatting guards), this moves closer to making the entire SCC unreachable in minimal WASM apps
4. **No behavioral change** when the switch is true (default), preserving backward compatibility

### Risks / Considerations

- **`Exception.ToString()` calls `this.StackTrace`** (the virtual property). Subclasses that override `StackTrace` might not respect the guard. The guard in `GetStackTrace()` only affects the default implementation.
- **Debuggability**: apps with `StackTraceSupport=false` would get empty/placeholder stack traces. This is already the expected trade-off (NativeAOT already strips stack trace metadata when this is false).
- **`SerializationStackTraceString`** (line 270) also calls `GetStackTrace()` — it would automatically benefit from the guard.
- The `StackTrace.IsSupported` property is `internal`, so the guard doesn't leak API surface.

## Conclusion

`StackTrace.IsSupported` is the right mechanism to control stack trace support, but it currently guards only a minor API (`DiagnosticMethodInfo`). The high-impact path — `Exception.StackTrace` → `new StackTrace()` → Reflection — is ungated. Adding two `if (!StackTrace.IsSupported)` guards in `Exception.GetStackTrace()` and `Exception.SetCurrentStackTrace()` would let the linker eliminate the entire StackTrace→Reflection dependency chain when `StackTraceSupport=false`, significantly reducing code size for Browser WASM apps.
