# StartupHookProvider.IsSupported — Feature Switch Analysis

## Summary

`StartupHookProvider.IsSupported` is a `FeatureSwitchDefinition`-annotated feature switch that controls whether .NET startup hooks are processed. When `PublishTrimmed=true`, the ILLink targets already default `StartupHookSupport` to **false**, and the linker correctly eliminates the main entry paths (`ProcessStartupHooks`, `ParseStartupHook`). However, `CallStartupHook(StartupHookNameOrPath)` — the heavy Reflection consumer at 221 KB transitive size — **survives trimming as an orphan** with no callers, likely due to a rooting issue in the ILLink descriptors.

## Current State of the Switch

### Definition (StartupHookProvider.cs, line 22–23)

```csharp
[FeatureSwitchDefinition("System.StartupHookProvider.IsSupported")]
private static bool IsSupported =>
    AppContext.TryGetSwitch("System.StartupHookProvider.IsSupported",
        out bool isSupported) ? isSupported : true;
```

### Where it is checked (3 sites — all correct)

1. **`ProcessStartupHooks(string)`** — the main Mono entry point (called from `object.c: mono_runtime_run_startup_hooks`):
   ```csharp
   private static void ProcessStartupHooks(string diagnosticStartupHooks)
   {
       if (!IsSupported)
           return;
       ...
   }
   ```

2. **`CallStartupHook(char*)`** — the CoreCLR diagnostic startup hook entry:
   ```csharp
   private static unsafe void CallStartupHook(char* pStartupHookPart)
   {
       if (!IsSupported)
           return;
       ParseStartupHook(ref startupHook, new string(pStartupHookPart));
       CallStartupHook(startupHook);  // calls the heavy overload
   }
   ```

3. **`ManagedStartup(char*)`** — the CoreCLR main entry point (StartupHookProvider.CoreCLR.cs):
   ```csharp
   private static unsafe void ManagedStartup(char* pDiagnosticStartupHooks)
   {
       if (IsSupported)
           ProcessStartupHooks(new string(pDiagnosticStartupHooks));
   }
   ```

### Where it is NOT checked

**`CallStartupHook(StartupHookNameOrPath)`** — the heavy overload that does all the Reflection — does NOT check `IsSupported`:
```csharp
private static void CallStartupHook(StartupHookNameOrPath startupHook)
{
    Assembly assembly;
    // ... LoadFromAssemblyPath / LoadFromAssemblyName ...
    Type type = assembly.GetType(StartupHookTypeName, throwOnError: true)!;
    MethodInfo? initializeMethod = type.GetMethod(InitializeMethodName, ...);
    // ... validation ...
    initializeMethod.Invoke(null, null);
}
```

This isn't a runtime correctness issue — all callers gate on `IsSupported` first. But it is a **linker visibility issue**: the linker should be able to eliminate this method when `IsSupported=false`, yet it survives.

## MSBuild Wiring

The MSBuild property maps to the runtime switch in two places:

**1. SDK targets** (Microsoft.NET.Sdk.targets):
```xml
<RuntimeHostConfigurationOption
    Include="System.StartupHookProvider.IsSupported"
    Condition="'$(StartupHookSupport)' != ''"
    Value="$(StartupHookSupport)"
    Trim="true" />
```

**2. ILLink targets** (Microsoft.NET.ILLink.targets) — defaults to `false` when trimming:
```xml
<PropertyGroup Condition="'$(PublishTrimmed)' == 'true'">
    <StartupHookSupport Condition="'$(StartupHookSupport)' == ''">false</StartupHookSupport>
    ...
</PropertyGroup>
```

**3. Mono ILLink Descriptor** (ILLink.Descriptors.xml) — feature-gated root:
```xml
<assembly fullname="System.Private.CoreLib"
          feature="System.StartupHookProvider.IsSupported"
          featurevalue="true" featuredefault="true">
    <type fullname="System.StartupHookProvider">
        <method name="CallStartupHook" />
        <method name="ProcessStartupHooks" />
    </type>
</assembly>
```

When `StartupHookSupport=false`, the feature value is `false`, so `featurevalue="true"` should NOT match, and these methods should NOT be rooted.

**4. WASM native entry** (callhelpers-reverse.cpp) — unconditional native call:
```cpp
LookupUnmanagedCallersOnlyMethodByName(
    "System.StartupHookProvider, System.Private.CoreLib",
    "CallStartupHook", &MD_...);
```
This roots `CallStartupHook(Char*, Exception*)` regardless of the feature switch, since it's an `[UnmanagedCallersOnly]` export called by the native runtime.

## What Survived Trimming in the Published Wasm.Browser.Sample

The sample has `PublishTrimmed=true` (triggering `StartupHookSupport=false` via ILLink targets):

| Method | ownSize | transitiveSize | Has Callers? | Notes |
|--------|---------|----------------|--------------|-------|
| `CallStartupHook(StartupHookNameOrPath)` | 444 | 221,174 | **No** | Orphan — heavy Reflection user |
| `CallStartupHook(Char*, Exception*)` | 15 | 18 | Native entry | `[UnmanagedCallersOnly]`, rooted by callhelpers-reverse.cpp |
| `ManagedStartup(Char*)` | 4 | 4 | Native entry | Guard inlined → body is just `return` |
| `CallStartupHook(Char*)` | 3 | 3 | Yes (Char*, Exception*) | Guard inlined → body is just `return` |

Key observations:
- **`ProcessStartupHooks` was correctly trimmed** — the `IsSupported` guard worked.
- **`ParseStartupHook` was correctly trimmed** — no callers remain.
- **`CallStartupHook(Char*)` was reduced to 3 bytes** — the linker substituted `IsSupported=false` and eliminated the dead branch.
- **`CallStartupHook(StartupHookNameOrPath)` survived at 444 bytes with NO callers** — this is the problem.

## The Dependency Chain

`CallStartupHook(StartupHookNameOrPath)` uses heavy Reflection:

| API | Purpose |
|-----|---------|
| `AssemblyLoadContext.Default.LoadFromAssemblyPath()` | Load hook assembly from path |
| `AssemblyLoadContext.Default.LoadFromAssemblyName()` | Load hook assembly by name |
| `Assembly.GetType(string, bool)` | Find `StartupHook` type |
| `Type.GetMethod(string, BindingFlags, Binder, Type[], ParameterModifier[])` | Find `Initialize` method |
| `Type.GetMethod(string, BindingFlags)` | Fallback method search |
| `MethodBase.Invoke(object, object[])` | Call `Initialize()` |
| `MethodInfo.ReturnType` | Validate return type |
| `MethodBase.GetParametersAsSpan()` | Validate parameters |

Its **transitive size of 221,174 bytes** is one of the largest in the entire app, pulling in:
- `MethodBase.Invoke` → 220,109 bytes (the single heaviest callee)
- `AssemblyLoadContext.LoadFromAssemblyPath` → 163,405 bytes
- `Type.GetMethod(...)` → 163,073 bytes
- `Assembly.GetType(...)` → 163,068 bytes
- Multiple paths into the 934-method SCC

## The Opportunity

### What's wrong

`CallStartupHook(StartupHookNameOrPath)` is an **orphan method** — it has zero callers in the trimmed app — but the linker didn't remove it. This is likely because:

1. The Mono ILLink descriptor roots `CallStartupHook` by **name** (matching ALL overloads), and the feature-gate might not be working correctly for the `StartupHookNameOrPath` overload.
2. Or the `[RequiresUnreferencedCode]` attribute on the method interacts with the linker's analysis in unexpected ways.

### Proposed fixes

**Option A: Add `IsSupported` guard to `CallStartupHook(StartupHookNameOrPath)`**

```csharp
private static void CallStartupHook(StartupHookNameOrPath startupHook)
{
    if (!IsSupported)
        return;

    Assembly assembly;
    // ... rest unchanged ...
}
```

This gives the linker a direct signal to eliminate the method body when `IsSupported=false`, regardless of rooting. It's defense-in-depth — the callers already check, but the body-level guard lets the linker substitute and eliminate.

**Option B: Fix the ILLink descriptor to not root `CallStartupHook(StartupHookNameOrPath)`**

The Mono descriptor roots `<method name="CallStartupHook" />` which matches ALL overloads. If only `CallStartupHook(Char*, Exception*)` needs rooting (as a native entry point), the descriptor could be more specific. However, the native entry is already rooted via `[UnmanagedCallersOnly]` + callhelpers-reverse.cpp, so the descriptor entry for `CallStartupHook` might be entirely unnecessary.

**Option C: Both** — add the guard AND tighten the descriptor.

### Expected Impact

If `CallStartupHook(StartupHookNameOrPath)` is eliminated:
- **221 KB of transitive closure** becomes unreachable from this path
- Specifically, `MethodBase.Invoke` (220 KB transitive) loses one of its entry points
- `Assembly.GetType`, `Type.GetMethod`, and `AssemblyLoadContext.LoadFromAssemblyPath` also lose an entry point
- Only **4 methods** (466 bytes IL) are *exclusively* reachable via StartupHookProvider, so the direct savings are modest
- But eliminating a major Reflection consumer reduces entry points into the SCC, compounding with other cuts

### Risks / Considerations

- **No runtime behavior change** — all callers already check `IsSupported` before calling the heavy overload
- **NativeAOT already handles this** — via `--feature:System.StartupHookProvider.IsSupported=false` in its repro.csproj
- The `[RequiresUnreferencedCode]` attribute on `CallStartupHook(StartupHookNameOrPath)` produces an intentional ILLink warning when `IsSupported=true` — this behavior is correct and should be preserved

## Conclusion

The `StartupHookProvider.IsSupported` feature switch is correctly implemented at all entry points, and the ILLink targets correctly default it to `false` for trimmed apps. The problem is narrower than it first appears: **`CallStartupHook(StartupHookNameOrPath)` survives as an orphan** in the trimmed output despite having no callers. Adding an `IsSupported` guard to this method's body would let the linker substitute it away, eliminating 221 KB of transitive Reflection closure. This is a low-risk, high-impact fix since the guard is already enforced by all calling paths.
