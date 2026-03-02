## The `CustomAttribute::IsDefined` Opportunity

This is **Cut #3** in the greedy fragmentation analysis, removing 101 methods from the residual SCC (after Lock/Monitor cuts have already reduced it from 934 → 153).

### The Single Call Site

The entire chain is triggered by **one line of code** in `RuntimeType.FilterApplyMethodBase` ([RuntimeType.CoreCLR.cs](src/coreclr/System.Private.CoreLib/src/System/RuntimeType.CoreCLR.cs)):

```csharp
if (!lastParameter.IsDefined(typeof(ParamArrayAttribute), false))
    return false;
```

This is the `params` array check during method overload resolution. When `Type.GetMethod()` is called with explicit argument types that don't match the parameter count, the runtime checks the last parameter for `[ParamArray]` to see if the method accepts variable arguments.

### The Cycle

The call creates an 11-hop cycle:

1. `RuntimeType::FilterApplyMethodBase` — checking `params` on method candidates
2. → `RuntimeParameterInfo::IsDefined(typeof(ParamArrayAttribute))` — the trigger
3. → `CustomAttribute::IsDefined(RuntimeParameterInfo, RuntimeType)`
4. → `CustomAttribute::IsCustomAttributeDefined(RuntimeModule, int, RuntimeType)`
5. → `CustomAttribute::FilterCustomAttributeRecord` — resolves the attribute's constructor token by calling `decoratedModule.ResolveType(...)`, which pulls in...
6. → `RuntimeModule::ResolveMethod` — resolves the ctor method handle
7. → `RuntimeType::GetMethodBase` — wraps the handle in a MethodBase
8. → `RuntimeType::GetMember` — general member lookup (handles all member types)
9. → `RuntimeType::GetMethodCandidates` — enumerates method candidates
10. → `RuntimeType::FilterApplyMethodInfo` — applies binding flags filter
11. → **back to** `RuntimeType::FilterApplyMethodBase` — which calls `IsDefined` again

The `FilterCustomAttributeRecord` method is the key — it needs to resolve the custom attribute's type from metadata, which pulls in the full module type resolution machinery, which in turn needs the full method candidate filtering pipeline.

### Why It's a Bridge

After Lock/Monitor are cut, this edge connects two otherwise-separate halves of the Reflection subsystem:
- **Side A** (108 methods): The core type system — `RuntimeType`, `Type`, member caching, method lookup
- **Side B** (52 methods): Custom attribute resolution — `CustomAttribute`, `PseudoCustomAttribute`, `MetadataImport`, `RuntimeModule.ResolveType/Method`, plus Enum infrastructure (via `RuntimeFieldInfo::IsDefined`)

Cutting it separates them into two components of 108 and 52 methods.

### Potential Fix Approaches

1. **Metadata-only `ParamArrayAttribute` check**: Instead of going through the full `CustomAttribute::IsDefined` pipeline (which resolves types from metadata), check for the `ParamArrayAttribute` constructor token directly using `MetadataImport.EnumCustomAttributes` + token comparison. The simpler `IsCustomAttributeDefined(module, token, null, ctorToken, false)` overload **already exists** and doesn't need `FilterCustomAttributeRecord` — it just compares raw metadata tokens. The fix would be to cache the `ParamArrayAttribute` ctor token and use the token-matching path.

2. **ILLink substitution**: Teach the linker that `FilterApplyMethodBase` never needs the `params` check on platforms where dynamic invoke via `Type.InvokeMember`/`Binder` is uncommon. This is more aggressive.

3. **Lazy attribute resolution**: Move the custom attribute type resolution behind a lazy pattern so `FilterCustomAttributeRecord` doesn't eagerly pull in `ResolveType`/`ResolveMethod`.

Approach #1 is the most surgical — it would eliminate the cycle without changing observable behavior, since `ParamArrayAttribute` has a known, fixed constructor token.
