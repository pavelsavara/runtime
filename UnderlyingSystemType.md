# RuntimeTypeBuilder::get_UnderlyingSystemType — Virtual Dispatch Coupling Hub

## Summary

`RuntimeTypeBuilder::get_UnderlyingSystemType()` is a **coupling hub** in the 934-method Reflection SCC. It is inside the SCC (ownSize=102, transitiveSize=163,058) and creates cycles through two paths:

1. **Tight 3-hop cycle**: `Type.Equals(Type)` → `this.UnderlyingSystemType` (virtual) → `RuntimeTypeBuilder::get_UnderlyingSystemType` → `Type.op_Equality` → `Type.Equals` → back
2. **Longer cycle through IsEnum**: `get_UnderlyingSystemType` → `IsEnum` → `IsSubclassOf` → ... → back through the SCC

The core issue is that `UnderlyingSystemType` is a **virtual property** on `Type`, and `Type.Equals`, `Type.op_Equality`, `Type.GetHashCode`, and `Type.GetTypeCodeImpl` all call it via virtual dispatch. The linker sees all possible dispatch targets, and `RuntimeTypeBuilder`'s override is the only one that calls back into the SCC (through `IsEnum`, `op_Equality`, `op_Inequality`). All other overrides are trivial (return `this` or throw).

## The Tight Cycle

```
Type.Equals(Type)
    public virtual bool Equals(Type? o) =>
        o != null && ReferenceEquals(this.UnderlyingSystemType, o.UnderlyingSystemType);
                                      │                          │
                                      ▼  (virtual dispatch)      ▼
RuntimeTypeBuilder::get_UnderlyingSystemType()
    if (m_bakedRuntimeType != null) return m_bakedRuntimeType;
    if (IsEnum) { ... }     ──── [SCC: calls IsSubclassOf → ...]
    else return this;
    │
    ├── Type.op_Equality(Type, Type)  ──── [used internally]
    │       left.Equals(right)  ──── BACK TO Type.Equals
    │
    └── Type.op_Inequality(Type, Type)
            !(left == right)  ──── calls op_Equality ──── back
```

## All UnderlyingSystemType Overrides

| Override | ownSize | transitiveSize | In SCC | Behavior |
|----------|---------|----------------|--------|----------|
| **RuntimeTypeBuilder** | 102 | 163,058 | **Yes** | Checks `m_bakedRuntimeType`, then `IsEnum` → SCC |
| RuntimeType | 2 | 2 | No | Returns `this` |
| SignatureType | 2 | 2 | No | Returns `this` |
| SymbolType | 2 | 2 | No | Returns `this` |
| TypeBuilderInstantiation | 2 | 2 | No | Returns `this` |
| RuntimeEnumBuilder | 22 | 146 | No | Throws `NotSupportedException` |
| RuntimeGenericTypeParameterBuilder | 22 | 146 | No | Throws `NotSupportedException` |
| TypeDelegator | 22 | 146 | No | Throws `NotSupportedException` |

**Only `RuntimeTypeBuilder` is in the SCC.** All others are trivial — they either return `this` or throw.

## Source Code

### RuntimeTypeBuilder (CoreCLR — RuntimeTypeBuilder.cs, line 951)

```csharp
public override Type UnderlyingSystemType
{
    get
    {
        if (m_bakedRuntimeType != null)
            return m_bakedRuntimeType;      // Already baked → return RuntimeType

        if (IsEnum)                          // ← calls Type.IsSubclassOf → SCC!
        {
            if (m_enumUnderlyingType == null)
                throw new InvalidOperationException(...);
            return m_enumUnderlyingType;
        }
        else
        {
            return this;
        }
    }
}
```

### RuntimeTypeBuilder (Mono — RuntimeTypeBuilder.Mono.cs, line 197)

```csharp
public override Type UnderlyingSystemType
{
    get
    {
        if (is_created)
            return created!.UnderlyingSystemType;  // Delegates to baked type

        if (IsEnum)                                 // ← same IsEnum → SCC path
        { ... }
        else
        {
            return this;
        }
    }
}
```

### Type.Equals (Type.cs, line 700)

```csharp
public virtual bool Equals(Type? o) =>
    o != null && ReferenceEquals(this.UnderlyingSystemType, o.UnderlyingSystemType);
```

### Type.op_Equality (Type.cs, line 703)

```csharp
public static bool operator ==(Type? left, Type? right)
{
    if (ReferenceEquals(left, right))
        return true;
    if (left is null || right is null || left is RuntimeType || right is RuntimeType)
        return false;
    return left.Equals(right);   // ← calls Equals → UnderlyingSystemType → cycle
}
```

## Why It's In the SCC

The cycle exists because:

1. **`Type.Equals`** calls `this.UnderlyingSystemType` (virtual dispatch)
2. The linker includes **all possible override targets**, including `RuntimeTypeBuilder`
3. **`RuntimeTypeBuilder::get_UnderlyingSystemType`** calls `IsEnum`
4. **`Type.IsEnum`** calls `IsSubclassOf(typeof(Enum))` (virtual dispatch)
5. `IsSubclassOf` implementations use `Type.op_Equality`/`op_Inequality`
6. **`Type.op_Equality`** calls `left.Equals(right)` → back to step 1

Additionally, `RuntimeTypeBuilder::get_UnderlyingSystemType` directly calls `op_Equality` and `op_Inequality` (for the `m_bakedRuntimeType != null` check, which the compiler emits as `Type.op_Inequality`).

## 31 Callers (14 in SCC, 17 outside)

### SCC callers (create cycles)

These methods call `attributeType.UnderlyingSystemType is not RuntimeType` as a type validation pattern:

| Caller | Context |
|--------|---------|
| `RuntimeType::IsDefined(Type, Boolean)` | Custom attribute check |
| `RuntimeMethodInfo::IsDefined(Type, Boolean)` | Custom attribute check |
| `RuntimeFieldInfo::IsDefined(Type, Boolean)` | Custom attribute check |
| `RuntimeConstructorInfo::IsDefined(Type, Boolean)` | Custom attribute check |
| `RuntimeParameterInfo::IsDefined(Type, Boolean)` | Custom attribute check |
| `RuntimePropertyInfo::IsDefined(Type, Boolean)` | Custom attribute check |
| `RuntimeEventInfo::IsDefined(Type, Boolean)` | Custom attribute check |
| `RuntimeTypeBuilder::IsDefined(Type, Boolean)` | Custom attribute check |
| `Type::Equals(Type)` | Type equality |
| `Type::IsAssignableFrom(Type)` | Type compatibility |
| `RuntimeType::IsAssignableFrom(Type)` | Type compatibility |
| `Type::GetTypeCodeImpl()` | TypeCode resolution |
| `DefaultBinder::SelectMethod(...)` | Method overload resolution |
| `RuntimeModule::ConvertToTypeHandleArray(Type[])` | Type handle conversion |

### Non-SCC callers (entry points into the SCC via this edge)

| Caller | transitiveSize | Context |
|--------|----------------|---------|
| `Activator::CreateInstance(...)` | 223,229 | Instance creation |
| `RuntimeTypeBuilder::GetCustomAttributes(...)` | 216,950 | Custom attribute retrieval |
| All `Runtime*Info::GetCustomAttributes(...)` | ~216,950 | Custom attribute retrieval |
| `DefaultBinder::SelectProperty(...)` | 163,700 | Property overload resolution |
| `DynamicMethod::Init(...)` | 169,582 | DynamicMethod setup |
| `Array::CreateInstance(Type, Int32)` | 163,184 | Array creation |
| `Type::GetHashCode()` | 163,090 | Hash code computation |

## The Opportunity

### Why RuntimeTypeBuilder exists in trimmed WASM apps

`RuntimeTypeBuilder` is part of `System.Reflection.Emit`, which supports runtime code generation. In a trimmed Browser WASM app, `Reflection.Emit` code may be preserved because:

1. The linker sees virtual dispatch through `Type.UnderlyingSystemType` and must keep all override implementations
2. `RuntimeTypeBuilder` is referenced through the type hierarchy even if no user code uses `TypeBuilder`

### Option A: Devirtualize UnderlyingSystemType for known types

The `attributeType.UnderlyingSystemType is not RuntimeType` pattern is used in ~20 places across `IsDefined` and `GetCustomAttributes` methods. Since the `attributeType` parameter in these methods comes from `typeof(...)` expressions (already `RuntimeType`) in most real usage, the linker should be able to devirtualize these calls.

However, the linker's devirtualization analysis may not be sophisticated enough to prove that `attributeType` is always `RuntimeType` in the trimmed app.

### Option B: Avoid calling IsEnum in get_UnderlyingSystemType

The `IsEnum` check in `RuntimeTypeBuilder::get_UnderlyingSystemType` could be replaced with a direct field check:

```csharp
// Instead of:
if (IsEnum)  // ← virtual call to IsSubclassOf → SCC

// Use direct state:
if (m_enumUnderlyingType != null)  // ← field check, no virtual dispatch
```

This would eliminate the `IsEnum` → `IsSubclassOf` → `op_Equality` → `Equals` → `UnderlyingSystemType` cycle entirely. The `m_enumUnderlyingType` field is set when defining an enum's underlying type, so it should be a reliable indicator.

**Caveat**: This changes semantics slightly — currently `IsEnum` checks the base type hierarchy, while `m_enumUnderlyingType != null` checks whether the underlying type was explicitly set. But for `UnderlyingSystemType`, the only purpose is to return the correct type, and if `m_enumUnderlyingType` is null the method already throws `InvalidOperationException`.

### Option C: Break the Type.Equals → UnderlyingSystemType cycle

`Type.Equals` calls `UnderlyingSystemType` on both operands via virtual dispatch. If `Type.op_Equality` could avoid calling `Equals` when both types are `RuntimeType` (which it already does via the `left is RuntimeType || right is RuntimeType` early-exit), the remaining case is non-RuntimeType equality.

The current `op_Equality` already handles `RuntimeType` fast paths:
```csharp
public static bool operator ==(Type? left, Type? right)
{
    if (ReferenceEquals(left, right)) return true;
    if (left is null || right is null || left is RuntimeType || right is RuntimeType)
        return false;   // ← Already fast-paths RuntimeType!
    return left.Equals(right);  // ← Only reached for non-runtime types
}
```

The cycle only exists because the linker can't prove that `left.Equals(right)` is unreachable in the trimmed app (it might be, if no non-RuntimeType instances exist).

### Option D: Trim Reflection.Emit more aggressively

If `RuntimeTypeBuilder` could be trimmed away entirely for apps that don't use `System.Reflection.Emit`, the virtual dispatch table for `Type.UnderlyingSystemType` would only contain trivial implementations (all returning `this` or throwing). This would break the cycle at the root.

This would require a feature switch for Reflection.Emit support (similar to `System.Diagnostics.StackTrace.IsSupported`).

## Impact Assessment

`RuntimeTypeBuilder::get_UnderlyingSystemType` is called from **31 methods** (14 in SCC). Eliminating it from the SCC would break multiple cycle paths simultaneously:

- All `IsDefined(Type, Boolean)` methods on Runtime*Info types would lose one of their SCC callees
- `Type.Equals` and `Type.op_Equality` would no longer cycle through `RuntimeTypeBuilder`
- `Type.GetHashCode` and `Type.GetTypeCodeImpl` would become acyclic

However, this is a **virtual dispatch** problem — the linker must keep all override targets. The fix requires either changing `RuntimeTypeBuilder`'s implementation (Option B) or providing the linker enough information to exclude `RuntimeTypeBuilder` from the dispatch set (Option D).

## Conclusion

`RuntimeTypeBuilder::get_UnderlyingSystemType` is the only `UnderlyingSystemType` override that participates in the SCC. It does so because it calls `IsEnum` (which triggers `IsSubclassOf` → type comparison → `Equals` → `UnderlyingSystemType`). The most targeted fix is Option B — replacing the `IsEnum` check with a direct `m_enumUnderlyingType != null` field check to avoid the virtual dispatch chain. The most impactful fix is Option D — a Reflection.Emit feature switch that would eliminate `RuntimeTypeBuilder` entirely from trimmed apps, breaking the virtual dispatch cycle at its source and potentially removing many Emit-related methods from the trimmed output.
