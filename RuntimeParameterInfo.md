# RuntimeParameterInfo::IsDefined — Articulation Edge in the Reflection SCC

## Summary

`RuntimeParameterInfo::IsDefined(Type, Boolean)` is a critical **articulation edge** in the 934-method Reflection SCC. It creates a cycle because `FilterApplyMethodBase` calls `lastParameter.IsDefined(typeof(ParamArrayAttribute), false)` to detect `params` parameters — but the custom attribute lookup pipeline (`CustomAttribute::IsCustomAttributeDefined` → `FilterCustomAttributeRecord` → `RuntimeModule::ResolveMethod` → `GetMethodBase` → `GetMember` → `GetMethodCandidates` → `FilterApplyMethodBase`) circles back to the same method that initiated the check.

**The root cause:** `ParamArrayAttribute` is NOT a pseudo custom attribute. Unlike `InAttribute`, `OutAttribute`, `OptionalAttribute`, and `MarshalAsAttribute` (which are handled by lightweight `PseudoCustomAttribute::IsDefined`), `ParamArrayAttribute` must go through the full metadata-resolving custom attribute pipeline, dragging in the entire Reflection type resolution system.

## The 12-Hop Cycle

```
1.  FilterApplyMethodBase(MethodBase, BindingFlags, ...)
        ↓  lastParameter.IsDefined(typeof(ParamArrayAttribute), false)
2.  RuntimeParameterInfo::IsDefined(Type, Boolean)
        ↓  CustomAttribute.IsDefined(this, attributeRuntimeType)
3.  CustomAttribute::IsDefined(RuntimeParameterInfo, RuntimeType)
        ↓  IsCustomAttributeDefined(parameter.GetRuntimeModule(), parameter.MetadataToken, caType)
4.  CustomAttribute::IsCustomAttributeDefined(RuntimeModule, Int32, RuntimeType)
        ↓  IsCustomAttributeDefined(module, token, type, 0, false)
5.  CustomAttribute::IsCustomAttributeDefined(RuntimeModule, Int32, RuntimeType, Int32, Boolean)
        ↓  FilterCustomAttributeRecord(record.tkCtor, scope, module, token, type, ...)
6.  FilterCustomAttributeRecord(MetadataToken, MetadataImport&, RuntimeModule, ...)
        ↓  decoratedModule.ResolveMethod(caCtorToken, typeArgs, null)  [for generic attributes]
7.  RuntimeModule::ResolveMethod(Int32, Type[], Type[])
        ↓  RuntimeType.GetMethodBase(...)
8.  RuntimeType::GetMethodBase(RuntimeType, IRuntimeMethodInfo)
        ↓  GetMethodBase(type, handle)
9.  RuntimeType::GetMethodBase(RuntimeType, RuntimeMethodHandleInternal)
        ↓  type.GetMember(name, MemberTypes, BindingFlags)
10. RuntimeType::GetMember(String, MemberTypes, BindingFlags)
        ↓  GetMethodCandidates(name, ...)
11. RuntimeType::GetMethodCandidates(String, Int32, BindingFlags, CallingConventions, Type[], Boolean)
        ↓  FilterApplyMethodInfo(method, flags, callConv, types)
12. RuntimeType::FilterApplyMethodInfo(RuntimeMethodInfo, BindingFlags, CallingConventions, Type[])
        ↓  FilterApplyMethodBase(method, flags, flags, callConv, types)
    → BACK TO STEP 1
```

## Method Metrics

| Method | ownSize | transitiveSize | In SCC |
|--------|---------|----------------|--------|
| `RuntimeParameterInfo::IsDefined(Type, Boolean)` | 105 | 163,058 | Yes |
| `CustomAttribute::IsDefined(RuntimeParameterInfo, RuntimeType)` | 30 | 163,058 | Yes |
| `PseudoCustomAttribute::IsDefined(RuntimeParameterInfo, RuntimeType)` | 182 | 163,058 | Yes |
| `CustomAttribute::IsCustomAttributeDefined(RuntimeModule, Int32, RuntimeType, Int32, Boolean)` | 222 | 163,058 | Yes |
| `FilterCustomAttributeRecord(...)` | 479 | 163,058 | Yes |
| `RuntimeModule::ResolveMethod(Int32, Type[], Type[])` | 446 | 163,058 | Yes |
| `RuntimeType::GetMethodBase(RuntimeType, RuntimeMethodHandleInternal)` | 540 | 163,058 | Yes |
| `RuntimeType::FilterApplyMethodBase(...)` | 315 | 163,058 | Yes |

All 8 methods are inside the SCC with transitiveSize = 163,058.

## Call Sites

`RuntimeParameterInfo::IsDefined` has **exactly 2 callers** in the published app:

1. **`RuntimeType::FilterApplyMethodBase`** — checking for `ParamArrayAttribute` on the last parameter (IN the SCC)
2. **`DefaultBinder::BindToMethod`** — also checking for `ParamArrayAttribute` on parameters (NOT in the SCC, transitiveSize = 220,004)

### FilterApplyMethodBase (RuntimeType.CoreCLR.cs, line 2366-2370)

```csharp
// ParamArray
if (testForParamArray)
{
    ...
    ParameterInfo lastParameter = parameterInfos[^1];

    if (!lastParameter.ParameterType.IsArray)
        return false;

    if (!lastParameter.IsDefined(typeof(ParamArrayAttribute), false))
        return false;
}
```

This is in the **method candidate filtering** path — called when `argumentTypes` count doesn't match the parameter count and we need to check if the method accepts `params` arrays. This is invoked during `Type.GetMethod(...)`, `Type.InvokeMember(...)`, and related Reflection APIs.

### DefaultBinder::BindToMethod (DefaultBinder.cs, multiple sites)

```csharp
// Line 147:
if (!par[j].IsDefined(typeof(ParamArrayAttribute), true))
    continue;

// Line 163:
if (!par[lastArgPos].IsDefined(typeof(ParamArrayAttribute), true))
    continue;

// Line 178:
if (par[lastArgPos].ParameterType.IsArray
    && par[lastArgPos].IsDefined(typeof(ParamArrayAttribute), true))
```

The `DefaultBinder` is called from `RuntimeType::InvokeMember` and `RuntimeType::CreateInstanceImpl`. It is NOT in the SCC but sits outside it.

## Why ParamArrayAttribute Is Special

The `PseudoCustomAttribute` system handles certain attributes as "pseudo" — they're stored in metadata flags rather than custom attribute blobs, so they can be returned without the full custom attribute resolution pipeline. For `RuntimeParameterInfo`, the pseudo custom attributes are:

- `InAttribute` → `parameter.IsIn` (flag check)
- `OutAttribute` → `parameter.IsOut` (flag check)
- `OptionalAttribute` → `parameter.IsOptional` (flag check)
- `MarshalAsAttribute` → `GetMarshalAsCustomAttribute()` (direct metadata read)

`ParamArrayAttribute` is **not** in this list. It's a real custom attribute stored in the metadata blob, so checking for it requires:
1. Enumerate custom attribute tokens on the parameter
2. For each, call `FilterCustomAttributeRecord` to resolve the attribute type
3. `FilterCustomAttributeRecord` calls `decoratedModule.ResolveType(...)` and potentially `decoratedModule.ResolveMethod(...)` to resolve the attribute constructor
4. Those `Resolve*` calls reach into the full `RuntimeType`/`RuntimeMethodInfo` infrastructure, which cycles back into `FilterApplyMethodBase`

## The Opportunity

### Option A: Treat ParamArrayAttribute as a pseudo custom attribute

The VM already recognizes `ParamArrayAttribute` as a well-known attribute in `wellknownattributes.h`. If `PseudoCustomAttribute::IsDefined` could detect `ParamArrayAttribute` via a lightweight metadata check instead of the full custom attribute pipeline, the cycle would be broken.

This would require either:
- Adding a new `ParamArray` parameter flag (if one doesn't already exist in metadata)
- Or adding a fast-path metadata token comparison for `ParamArrayAttribute` in `PseudoCustomAttribute::IsDefined`

### Option B: Use metadata token comparison in FilterApplyMethodBase

Instead of calling `lastParameter.IsDefined(typeof(ParamArrayAttribute), false)`, use a direct metadata check:

```csharp
// Instead of:
if (!lastParameter.IsDefined(typeof(ParamArrayAttribute), false))
    return false;

// Use a direct metadata token approach:
if (!HasParamArrayAttribute(lastParameter))
    return false;
```

Where `HasParamArrayAttribute` checks the custom attribute blob directly for the `ParamArrayAttribute` constructor token without going through `FilterCustomAttributeRecord`'s full type resolution.

### Option C: Bypass IsDefined for known simple attributes

For attributes that have no constructor parameters (like `ParamArrayAttribute`), the full `FilterCustomAttributeRecord` → `ResolveMethod` path for constructor resolution is unnecessary. A specialized `IsSimpleAttributeDefined` that only checks `ResolveType` (not `ResolveMethod`) might break the specific `ResolveMethod` → `GetMethodBase` → `FilterApplyMethodBase` cycle link.

### Impact Assessment

**Cutting this single edge** (`FilterApplyMethodBase` → `IsDefined`) was identified as **Cut #3** in the Phase 4 greedy fragmentation analysis. In the greedy analysis, the first 11 cuts fully fragmented the 934-method SCC. This edge is one of the key articulation points.

However, the `DefaultBinder::BindToMethod` → `IsDefined` path would still exist. That path doesn't create a cycle (DefaultBinder is not in the SCC), but it still pulls in the SCC through IsDefined. Fixing the IsDefined pipeline itself (Option A or C) would benefit both callers.

**Methods exclusively dependent on this edge:** Since `IsDefined` is also called from `DefaultBinder`, cutting only the `FilterApplyMethodBase` → `IsDefined` edge wouldn't make `IsDefined` itself unreachable. The benefit is breaking the **cycle**, not eliminating the method.

## Risks / Considerations

- **Behavioral correctness:** Any alternative must correctly detect `ParamArrayAttribute` on parameters. `ParamArrayAttribute` is a real attribute emitted by every C# compiler for `params` parameters, so it will always be present in metadata.
- **Performance:** A faster `ParamArrayAttribute` check would actually improve Reflection performance for method resolution with mismatched argument counts — it would avoid the overhead of `FilterCustomAttributeRecord` for a simple presence check.
- **Cross-runtime consistency:** Both CoreCLR and Mono runtimes have the identical `FilterApplyMethodBase` code with the same `IsDefined` call. NativeAOT uses `QualifiesBasedOnParameterCount` which also calls `IsDefined` but has a different custom attribute backend.

## Conclusion

`RuntimeParameterInfo::IsDefined` creates a cycle because checking for `ParamArrayAttribute` requires the full custom attribute resolution pipeline, which needs to resolve types and methods, which invokes the same method-filtering code that initiated the attribute check. The most elegant fix would be to promote `ParamArrayAttribute` detection to the `PseudoCustomAttribute` fast path or implement a direct metadata check, avoiding the heavy `FilterCustomAttributeRecord` → `ResolveMethod` → `GetMethodBase` pipeline entirely. This would break a key cycle in the SCC and potentially improve Reflection performance.
