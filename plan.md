# typeof(T) Linker Optimization — Research & Plan

## 1. Problem Statement

The ILLinker's `UnreachableBlocksOptimizer` cannot fold `typeof(T) == typeof(X)` patterns. The JIT handles this at runtime (each generic instantiation gets constant-folded), and NativeAOT's `SubstitutedILProvider` handles it at compile time. But ILLink leaves these patterns intact, which means:

- `Type.GetTypeFromHandle` / `Type.op_Equality` must be preserved
- Both branches of `typeof(T) == typeof(int)` remain live
- Types like `Scalar<T>` (8,177B IL, 11 methods — all guarded by typeof dispatch) cannot be trimmed
- 18/29 SCC clusters couple through `Type.op_Equality`

Estimated impact: **20–40 KB IL savings** on WASM/trimmed targets.

---

## 2. Code Locations Verified

| Component | File | Key Area |
|-----------|------|----------|
| **ILLink optimizer** | `src/tools/illink/src/linker/Linker.Steps/UnreachableBlocksOptimizer.cs` | Main optimizer — `ProcessMethod`, `BodyReducer`, `CallInliner`, `ConstantExpressionMethodAnalyzer` |
| **ILLink context entry** | `src/tools/illink/src/linker/Linker/LinkContext.cs:1034` | `_unreachableBlocksOptimizer.ProcessMethod(method)` called from `GetMethodIL` |
| **NativeAOT pattern analyzer** | `src/coreclr/tools/aot/ILCompiler.Compiler/IL/TypeEqualityPatternAnalyzer.cs` | State machine matching the `ldtoken;GetTypeFromHandle;...;op_Equality;br*` sequence |
| **NativeAOT substituted IL** | `src/coreclr/tools/aot/ILCompiler.Compiler/Compiler/SubstitutedILProvider.cs` | `TryExpandTypeEquality` at line ~1015, `TryGetConstantArgument`, dead-branch removal |
| **NativeAOT scanner usage** | `src/coreclr/tools/aot/ILCompiler.Compiler/IL/ILImporter.Scanner.cs:995` | Uses `TypeEqualityPatternAnalyzer` to conditionally remove branches |
| **Type comparison** | `src/coreclr/tools/Common/Compiler/TypeExtensions.cs:157` | `CompareTypesForEquality(TypeDesc, TypeDesc)` |
| **Body substitution XML** | `src/tools/illink/src/linker/Linker.Steps/BodySubstitutionParser.cs` | Existing substitution mechanism (IntPtr.Size, feature switches) |
| **Example pattern (Scalar\<T\>)** | `src/libraries/System.Private.CoreLib/src/System/Runtime/Intrinsics/Scalar.cs` | `typeof(T) == typeof(byte)` repeated for every numeric type |
| **ILLink tests** | `src/tools/illink/test/Mono.Linker.Tests.Cases/UnreachableBlock/` | 32 test files covering existing optimization patterns |

---

## 3. Verified Blockers in ILLink

### Blocker 1: `IsConstantValue()` does not recognize `ldtoken`

**File:** `UnreachableBlocksOptimizer.cs`, `GetArgumentsOnStack` method (~line 388)

The nested `IsConstantValue` function only recognizes `Ldc_I4_*`, `Ldc_I8`, `Ldc_R4`, `Ldc_R8`, `Ldnull`, `Ldstr`. `Code.Ldtoken` is not in the list.

This means when `GetArgumentsOnStack` scans for arguments to a call like `Type.GetTypeFromHandle(RuntimeTypeHandle)`, it sees `ldtoken T` as the argument, returns `null` ("non-constant argument"), and the entire call chain is abandoned.

**Partial exception:** Inside `ConstantExpressionMethodAnalyzer.Analyze()`, `Code.Ldtoken` IS pushed onto the stack (line ~1810). But `ConvertStackToResult()` (line ~2050) does NOT recognize it as a valid result. And `GetConstantValue()` does not extract its operand. So even the deeper analysis path cannot propagate ldtoken values.

### Blocker 2: `EvaluateIntrinsicCall()` only handles `String` methods

**File:** `UnreachableBlocksOptimizer.cs`, line ~326

```csharp
static Instruction? EvaluateIntrinsicCall(MethodReference method, Instruction[] arguments)
{
    if (method.DeclaringType.MetadataType == MetadataType.String)
    {
        switch (method.Name)
        {
            case "op_Equality":
            case "op_Inequality":
            case "Concat":
```

Only `MetadataType.String` is checked. `System.Type` methods (`GetTypeFromHandle`, `op_Equality`, `op_Inequality`) are never matched. There's no concept of a "type identity" value that could flow through the optimizer.

### Blocker 3: No per-instantiation context

**File:** `UnreachableBlocksOptimizer.cs`, line ~266

```csharp
readonly Dictionary<MethodDefinition, MethodResult?> _cache_method_results = new(2048);
```

The cache is keyed by `MethodDefinition`, not `MethodReference` (which would include generic instantiation). The optimizer works on uninstantiated method definitions. When processing `Scalar<T>.Add()`, `T` remains an open generic parameter — there's no mechanism to evaluate `typeof(T)` as `typeof(byte)` for the `Scalar<byte>` instantiation.

The linker only ever calls `ProcessMethod(MethodDefinition method)` — it never processes per-instantiation bodies.

### Blocker 4 (architecture): BodyReducer vs. SubstitutedILProvider approach

ILLink's optimizer works in two phases:
1. `ApplyTemporaryInlining` — replaces calls with constant results inline
2. `RemoveConditions` / `RewriteBody` — removes dead conditional branches

This is fundamentally different from NativeAOT's approach which:
1. Scans the IL byte stream forward with a state machine (`TypeEqualityPatternAnalyzer`)
2. Recognizes the multi-instruction pattern as a unit
3. Evaluates `TryExpandTypeEquality` against resolved types
4. Marks only the taken branch as reachable

ILLink would need either a similar pattern-matching approach or to extend its existing infrastructure to handle multi-instruction evaluation chains.

---

## 4. What NativeAOT Does (Reference Implementation)

NativeAOT's `SubstitutedILProvider.GetMethodILWithInlinedSubstitutions()` handles this in three coordinated pieces:

### 4a. Pattern Recognition — `TypeEqualityPatternAnalyzer`
A state machine that recognizes the IL sequence:
```
ldtoken Foo → call GetTypeFromHandle → ldtoken Bar → call GetTypeFromHandle → call op_Equality → brtrue/brfalse
```
It also handles single-token patterns (one ldtoken + arg/local) and stloc/ldloc pairs from debug codegen.

### 4b. Pattern Evaluation — `TryExpandTypeEquality`
When both tokens are concrete (non-signature-variable) types:
```csharp
bool? equality = TypeExtensions.CompareTypesForEquality(type1, type2);
```
When only one token is known, it checks whether the type could exist at runtime (has constructed MethodTable).

Key guard: `type.ContainsSignatureVariables()` → return false (can't evaluate open generics).

### 4c. Per-Instantiation IL
NativeAOT processes **instantiated** method IL. `SubstitutedILProvider.GetMethodIL(MethodDesc method)` receives e.g. `Scalar<byte>.Add()`, not `Scalar<T>.Add()`. When it calls `method.GetObject(token)`, the token resolves to the concrete type. This is the fundamental enabler — without it, open generic `T` cannot be evaluated.

---

## 5. Implementation Options

### Option A: Concrete-Types-Only Pattern Matching (Low Difficulty, Partial Win)

Add `TypeEqualityPatternAnalyzer`-style pattern matching to ILLink's `BodyReducer`, but only for the case where **both tokens are concrete types** (no open generic parameters).

**What it handles:**
- `typeof(int) == typeof(byte)` → folds to `false`
- `typeof(string) == typeof(string)` → folds to `true`
- Guards in non-generic code or where both sides are concrete

**What it doesn't handle:**
- `typeof(T) == typeof(int)` where `T` is a generic parameter (the main `Scalar<T>` case)

**Changes needed:**
1. In `BodyReducer.ApplyTemporaryInlining`, add a new case for the `ldtoken; call GetTypeFromHandle; ldtoken; call GetTypeFromHandle; call op_Equality` pattern
2. When both ldtoken operands are `TypeReference` that `Resolve()` to concrete `TypeDefinition`s (not `GenericParameter`), evaluate equality
3. Replace the `call op_Equality` result with `ldc.i4.0` or `ldc.i4.1`, and the preceding instructions with `nop`

**Estimated difficulty:** Medium
**Estimated impact:** Low — most interesting patterns involve generic `T`

### Option B: Extend ConstantExpressionMethodAnalyzer with Type Identity (Medium Difficulty)

Make the `ConstantExpressionMethodAnalyzer` understand "type identity" as a trackable value:

1. When encountering `ldtoken <TypeRef>; call GetTypeFromHandle`, push a synthetic "type identity" instruction
2. Add `Type.GetTypeFromHandle` and `Type.op_Equality/Inequality` as intrinsics in `EvaluateIntrinsicCall`
3. Represent type identity as a special tagged value (e.g., extend `GetConstantValue` to extract `TypeReference` from `ldtoken`)

This still only works for concrete types but integrates more naturally with the existing evaluation framework.

**Changes needed:**
1. Add `Code.Ldtoken` to `IsConstantValue()` in `GetArgumentsOnStack`
2. Extend `GetConstantValue` to return a `TypeReference` wrapper for ldtoken operands
3. Add `System.Type` handling in `EvaluateIntrinsicCall`:
   - `GetTypeFromHandle`: pass through the type identity
   - `op_Equality`: compare two type identities → `Ldc_I4_0` or `Ldc_I4_1`
4. Extend `ConvertStackToResult` to accept ldtoken-derived values

**Estimated difficulty:** Medium
**Estimated impact:** Low-Medium for concrete types

### Option C: Per-Instantiation Optimization (Hard, Full Win)

This is the big one. Make ILLink process generic method instantiations individually.

**Architecture change:** Instead of calling `ProcessMethod(MethodDefinition)` once for `Scalar<T>.Add()`, call it for each used instantiation: `Scalar<byte>.Add()`, `Scalar<int>.Add()`, etc.

**What this requires:**
1. **Instantiation discovery:** During marking, collect all concrete generic instantiations that are used
2. **Per-instantiation body:** Create instantiated method bodies where generic parameters are resolved
3. **Per-instantiation optimization:** Run the optimizer on each instantiated body separately
4. **Cache key change:** Key results by `MethodReference` (including generic args), not `MethodDefinition`
5. **Integration with marking:** Only mark types/methods reachable from the taken branches of each instantiation

**Risks:**
- Significant throughput impact (multiplies work by number of instantiations)
- Complex interaction with existing linker marking (currently works on definitions)
- May require changes to how Cecil resolves types in generic contexts
- Potential correctness issues with shared generic implementations

**Estimated difficulty:** Very Hard
**Estimated impact:** High — estimated 20-40KB IL reduction, SCC breakup

### Option D: XML Substitution for Specific Patterns (Easy, Targeted Win)

Instead of making the linker smarter, manually annotate the known problematic patterns:

1. Add `ILLink.Substitutions.xml` entries that stub out specific methods
2. Use `[FeatureSwitchDefinition]` or similar attributes on wrapper properties

For example, create non-generic helper methods that the linker CAN fold:
```csharp
// In CoreLib
internal static class ScalarTypeHelper
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool IsTypeSupported<T>() where T : struct
    {
        // The linker can't fold this, but we could provide substitutions
        return typeof(T) == typeof(byte) || typeof(T) == typeof(int) || ...;
    }
}
```

Or refactor `Scalar<T>` to use checked patterns that the linker already handles (e.g., `RuntimeHelpers.IsReferenceOrContainsReferences<T>()` is already handled via substitutions in some cases).

**Estimated difficulty:** Easy per-pattern, doesn't scale
**Estimated impact:** Targeted — only helps manually annotated code

### Option E: Hybrid — Pattern Match + Selective Instantiation (Medium-Hard, Best ROI)

Combine Options A/B with limited instantiation analysis:

1. Add concrete-type pattern matching (Option A/B)
2. For methods where the pattern analysis detects an unresolvable `typeof(T)`, flag them
3. When the linker encounters a concrete instantiation of a flagged method during marking, analyze the instantiated body
4. Only do per-instantiation analysis for methods that contain `typeof(T)` patterns

This limits the throughput impact while getting the full benefit for the patterns that matter.

---

## 6. Recommended Approach

### Phase 1 (Quick Win): Option A/B — Concrete-type typeof equality folding

Validates the approach, establishes test infrastructure, proves the pattern-matching works in ILLink.

**Step 1.1: Add `ldtoken` as recognizable constant in `GetArgumentsOnStack`**
- Add `Code.Ldtoken` to inner `IsConstantValue()` switch
- Extend `GetConstantValue()` to extract `TypeReference` from ldtoken operand

Tests (add to `src/tools/illink/test/Mono.Linker.Tests.Cases/UnreachableBlock/`):
- `TypeOfEqualityConcreteTypes.cs` — basic case:
  ```csharp
  static void ConcreteTypeofEqualityTrue()
  {
      if (typeof(int) == typeof(int)) { Kept(); } else { Removed(); }
  }
  static void ConcreteTypeofEqualityFalse()
  {
      if (typeof(int) == typeof(byte)) { Removed(); } else { Kept(); }
  }
  ```
  Verify with `[ExpectedInstructionSequence]` / `[RemovedMemberInAssembly]` that dead branch is eliminated.
- `TypeOfInequalityConcreteTypes.cs` — same patterns with `!=` (`op_Inequality`).
- `TypeOfReferencetypes.cs` — `typeof(string) == typeof(object)` → false, `typeof(string) == typeof(string)` → true.
- `TypeOfNested.cs` — `typeof(List<int>) == typeof(List<int>)` → true, `typeof(List<int>) == typeof(List<byte>)` → false.

**Step 1.2: Add `Type.GetTypeFromHandle` and `Type.op_Equality/Inequality` as intrinsics in `EvaluateIntrinsicCall`**
- Detect `method.DeclaringType` is `System.Type`
- `GetTypeFromHandle`: pass through the ldtoken type identity
- `op_Equality`: compare two resolved `TypeReference`s → `Ldc_I4_0` or `Ldc_I4_1`

Tests:
- `TypeOfEqualityBranchRemoval.cs` — full pattern: `ldtoken; GetTypeFromHandle; ldtoken; GetTypeFromHandle; op_Equality; brtrue` → dead branch removed. Verify with `[ExpectedInstructionSequence]` that the entire 6-instruction sequence is folded to a constant + branch (or nop).
- `TypeOfEqualityInlinedResult.cs` — verify that when the typeof comparison is the return value of a method, the method body is replaced:
  ```csharp
  static bool AreSameType() => typeof(int) == typeof(int); // should inline to true
  static void Caller() { if (AreSameType()) { Kept(); } else { Removed(); } }
  ```
- `TypeOfNegative_GenericParam.cs` — open generic `typeof(T) == typeof(int)` must NOT be folded (remains untouched). Verify the method body is unchanged.
- `TypeOfNegative_DebugCodegen.cs` — same pattern with stloc/ldloc pair (debug mode C# compiler output) for concrete types. Verify folding still works through the stloc/ldloc.

**Step 1.3: Extend `ConstantExpressionMethodAnalyzer` to propagate type identity through calls**
- When analyzing a method body that calls `Type.GetTypeFromHandle` with a concrete ldtoken, evaluate the chain
- `ConvertStackToResult` accepts type-comparison results

Tests:
- `TypeOfEqualityThroughPropertyCall.cs` — typeof check behind a property:
  ```csharp
  static bool IsInt => typeof(int) == typeof(byte);
  static void Test() { if (IsInt) { Removed(); } else { Kept(); } }
  ```
- `TypeOfEqualityMultipleBranches.cs` — chain of typeof checks:
  ```csharp
  static void Test()
  {
      if (typeof(int) == typeof(byte)) { Removed1(); }
      else if (typeof(int) == typeof(int)) { Kept(); }
      else { Removed2(); }
  }
  ```
- `TypeOfEqualitySwitchLike.cs` — simulate the `Scalar<T>` dispatch pattern but with all-concrete types to prove the chain works end-to-end.

**Step 1.4: Integration tests and measurement**

Tests:
- `TypeOfEqualitySideEffects.cs` — typeof comparison combined with a side-effecting method. Verify the side-effecting call is NOT removed even when the branch is dead:
  ```csharp
  static void Test()
  {
      SideEffect();
      if (typeof(int) == typeof(byte)) { Removed(); }
  }
  ```
- `TypeOfEqualityExceptionHandlers.cs` — typeof check inside try/catch. Verify exception handler is kept or removed correctly based on reachability.
- `TypeOfEqualityWithExistingSubstitutions.cs` — typeof check combined with an existing substitution (e.g., `IntPtr.Size`). Verify both optimizations compose correctly.
- Measurement script: count `ldtoken; call GetTypeFromHandle; ...; call op_Equality` patterns in trimmed WASM CoreLib output. Record baseline IL size.

---

### Phase 2 (Medium-term): Option E — Selective per-instantiation optimization

Only for methods flagged as containing `typeof(T)` patterns. Focus on `Scalar<T>`, `Vector128<T>`, `Vector256<T>` as primary targets.

**Step 2.1: Flag methods containing open-generic `typeof(T)` patterns**
- During `ApplyTemporaryInlining`, when a `ldtoken <GenericParameter>` is encountered in a typeof-equality pattern, flag the `MethodDefinition`
- Store flagged methods in a set on `UnreachableBlocksOptimizer`

Tests:
- `TypeOfGenericFlagging.cs` — verify (via logging or diagnostic output) that `Scalar<T>.AllBitsSet` getter is flagged as containing a `typeof(T)` pattern.
- `TypeOfGenericNotFlaggedWhenConcrete.cs` — verify that a method containing only `typeof(int) == typeof(byte)` is NOT flagged (it's resolved in Phase 1, no per-instantiation needed).

**Step 2.2: Collect concrete generic instantiations during marking**
- When `MarkStep` encounters a `GenericInstanceMethod` or `GenericInstanceType` reference to a flagged method, record the concrete instantiation
- Store as `Dictionary<MethodDefinition, HashSet<GenericInstanceMethod>>`

Tests:
- `TypeOfGenericInstantiationDiscovery.cs` — a test assembly that calls `Scalar<byte>.AllBitsSet` and `Scalar<int>.AllBitsSet`. Verify both instantiations are collected.
- `TypeOfGenericTransitiveDiscovery.cs` — call `Vector128.Create<byte>(...)` which internally calls `Scalar<byte>`. Verify the transitive instantiation is discovered.
- `TypeOfGenericNoInstantiationForUnflagged.cs` — verify that generic methods without typeof patterns do NOT trigger instantiation collection (no overhead for normal generics).

**Step 2.3: Create and optimize per-instantiation method bodies**
- For each concrete instantiation of a flagged method, create an instantiated copy of the method body using Cecil's generic resolution
- Resolve `GenericParameter` references to concrete types
- Run `ApplyTemporaryInlining` + `RemoveConditions` on the instantiated body
- Merge marking results: only mark types/methods reachable from the surviving branches

Tests:
- `TypeOfGenericScalarPattern.cs` — simulate `Scalar<T>.AllBitsSet` pattern:
  ```csharp
  class MyScalar<T>
  {
      public static T Value
      {
          get
          {
              if (typeof(T) == typeof(int)) return (T)(object)42;
              else if (typeof(T) == typeof(byte)) return (T)(object)(byte)255;
              else throw new NotSupportedException();
          }
      }
  }
  // Usage: MyScalar<int>.Value — only the int branch should survive
  ```
  Verify with `[KeptMemberInAssembly]` / `[RemovedMemberInAssembly]` that the byte branch and NotSupportedException throw are removed for the `int` instantiation, and vice versa.
- `TypeOfGenericMultipleInstantiations.cs` — call `MyScalar<int>.Value` AND `MyScalar<byte>.Value`. Verify both instantiations are optimized independently (int branch kept for int, byte branch kept for byte).
- `TypeOfGenericUnusedInstantiation.cs` — declare `MyScalar<long>` but never use it. Verify it is fully trimmed.
- `TypeOfGenericMixedBranches.cs` — typeof dispatch that also has non-typeof branches:
  ```csharp
  if (typeof(T) == typeof(int)) { ... }
  else if (someRuntimeCondition) { ... }
  else { ... }
  ```
  Verify only the typeof branches are folded; the runtime-condition branch survives.
- `TypeOfGenericNestedCalls.cs` — typeof dispatch method calls another typeof dispatch method. Verify transitive folding works.

**Step 2.4: Cache and throughput validation**
- Key per-instantiation results by `MethodReference` (with generic args)
- Measure linker throughput: time and memory on a real WASM app build

Tests:
- `TypeOfGenericCacheCorrectness.cs` — same method with 10+ instantiations. Verify each gets the correct optimization result (no cache collision between instantiations).
- Benchmark test: trim a WASM app that references `System.Numerics.Vectors` heavily. Compare before/after:
  - Total IL bytes in trimmed output
  - Number of methods kept from `Scalar<T>`, `Vector128<T>`, `Vector256<T>`
  - Linker wall-clock time (must not regress more than 10%)
  - Linker peak memory (must not regress more than 15%)

---

### Phase 3 (Long-term): Option C — Full per-instantiation optimization

Generalizes Phase 2. Requires deeper integration with the marking pipeline.

**Step 3.1: Generalize instantiation tracking to all generic methods**
- Move from "flagged methods only" to all generic method definitions
- Optimize per-instantiation bodies for any method that benefits

Tests:
- `GenericBranchEliminationNonTypeof.cs` — a generic method with constant-foldable branches that don't use typeof (e.g., `if (default(T) == null)` for reference vs value type dispatch). Verify branch elimination works per-instantiation.
- `GenericConstraintBasedFolding.cs` — generic method using `RuntimeHelpers.IsReferenceOrContainsReferences<T>()`. Verify the optimization composes with existing substitution-based folding.

**Step 3.2: Integrate with marking pipeline (lazy per-instantiation optimization)**
- Process instantiated bodies lazily: only when the marking step reaches a specific generic instantiation
- Avoid processing instantiations that are never reached

Tests:
- `GenericLazyOptimization.cs` — declare many generic instantiations but only call a few. Verify that unused instantiations are never analyzed (check via linker diagnostic messages or timing).
- `GenericCircularInstantiations.cs` — `A<T>` calls `B<T>` calls `A<T>`. Verify the optimizer handles circular references without infinite loops or stack overflow.
- `GenericRecursiveInstantiation.cs` — `Foo<T>` calls `Foo<List<T>>`. Verify the optimizer bounds recursion depth correctly.

**Step 3.3: Shared generic implementation correctness**
- Verify interaction with runtime generic sharing (all reference types share one implementation)
- Ensure optimized IL is compatible with the runtime's instantiation strategy

Tests:
- `GenericSharingReferenceTypes.cs` — `MyDispatch<string>` and `MyDispatch<object>` where both are reference types. If the runtime shares their implementation, verify the linker doesn't remove branches that the shared implementation needs.
- `GenericSharingValueTypes.cs` — `MyDispatch<int>` and `MyDispatch<byte>` (value types are NOT shared). Verify independent optimization is safe.
- `GenericSharingMixed.cs` — mix of reference and value type instantiations. Verify each category is handled correctly.

**Step 3.4: End-to-end validation**

Tests:
- Full WASM app test: build a trimmed Blazor WASM app using `System.Text.Json` (which uses `typeof(T)` dispatch internally). Verify:
  - App loads and runs correctly
  - Serialization/deserialization of known types works
  - Trimmed output size is measurably smaller
- CoreLib SCC analysis: re-run the SCC analysis from §2.11. Verify that the 18/29 `Type.op_Equality` clusters break apart after Phase 3.
- Regression suite: run the full ILLink test suite (`src/tools/illink/test/`) — zero regressions.

---

## 7. Open Questions

1. **Is this the right place to optimize?** The linker runs on IL before compilation. Would it be better to handle this in a post-linker step or in the Mono/CoreCLR AOT compiler instead?

2. **Mono AOT handling:** Does Mono's AOT compiler (used for WASM) already handle `typeof(T)` folding per-instantiation? If so, the linker optimization is less critical for WASM and mainly benefits IL size.

3. **Interaction with generic sharing:** If the runtime uses shared generic code (e.g., all reference type instantiations share one implementation), per-instantiation linker optimization could produce different code than what's actually executed. Need to verify this doesn't cause issues.

4. **Cecil limitations:** Can Cecil resolve `GenericParameter` references in the context of a specific `GenericInstanceType`? Or would we need to build our own type substitution logic?

5. **Upstream appetite:** ILLink is a shared component. Would the ILLink maintainers accept this kind of change? Should this be proposed as an issue first?

6. **Measurement:** Before implementing, we should measure the actual impact on a real WASM app. How much IL does the `typeof(T)` pattern actually contribute to trimmed output size?

---

## 8. Progress

### Phase 1: Concrete-type typeof equality folding — ✅ COMPLETE

**Implementation:**
- Added `TryEvaluateTypeEqualityPattern()` to detect the IL pattern `ldtoken; GetTypeFromHandle; ldtoken; GetTypeFromHandle; op_Equality/op_Inequality` inline.
- Extended `EvaluateIntrinsicCall()` to handle `System.Type` methods (`GetTypeFromHandle` passthrough, `op_Equality/op_Inequality` comparison).
- Updated `ConstantExpressionMethodAnalyzer` to propagate type identity through property/method calls.
- All concrete-type typeof comparisons fold correctly, including nested generics like `List<int> == List<byte>`.

**Tests:** `TypeOfComparisonConcreteTypes.cs` — 11 test methods covering equality, inequality, reference types, generic types, property-based patterns, multiple branches, and open-generic non-folding. All pass.

**Regressions:** 0. Full suite: 1126/1160 passed, 1 pre-existing failure, 33 skipped.

### Phase 2: Selective per-instantiation optimization — ✅ COMPLETE

**Architecture decision:** Post-marking body re-optimization. MarkStep processes method bodies ONCE per MethodDefinition (not per instantiation), so the initial marking sees all branches as live. After marking completes, a new pipeline step (`TypeofOptimizationStep`) collects known instantiations and re-optimizes flagged method bodies. This provides CODE SIZE reduction (dead typeof branches removed from IL) but does NOT enable trimming of methods only reachable from dead branches (they were already marked). Full trimming integration is deferred to Phase 3.

**Implementation (4 files modified, 2 new files):**

1. **`UnreachableBlocksOptimizer.cs`** — Added:
   - Fields: `_methodsWithGenericTypeofPatterns`, `_typeInstantiations`, `_methodInstantiations`
   - `FlagMethodWithGenericTypeofPattern()` — called during initial processing when typeof(T) pattern detected
   - `ProcessDeferredTypeofMethods()` — main entry point called after marking
   - `CollectInstantiationsFromLinkedAssemblies()` / `CollectInstantiationsFromType()` — scans all marked method bodies for generic call sites referencing flagged methods
   - `GetInstantiationsForMethod()` — merges method-level and type-level instantiations
   - `ReprocessMethodWithInstantiations()` — creates fresh BodyReducer with instantiation context
   - `TryEvaluateTypeofWithInstantiations()` — evaluates typeof equality against all known instantiations, folds if unanimous
   - `HasGenericTypeofPattern()` — lightweight check for flagging
   - Modified `TryEvaluateTypeEqualityPattern()` — optional `knownInstantiations` parameter
   - Modified `ApplyTemporaryInlining()` — optional `knownInstantiations` parameter, flagging logic

2. **`TypeofOptimizationStep.cs`** (NEW) — Pipeline step that triggers deferred processing

3. **`LinkContext.cs`** — Added `ProcessDeferredTypeofOptimizations()` bridge method

4. **`Driver.cs`** — Registered `TypeofOptimizationStep` before `SweepStep` in pipeline

**Key algorithm:** For each typeof(T)==typeof(X) pattern, evaluate against ALL known instantiations using `TypeReferenceExtensions.InflateGenericType()`. If all instantiations agree (all true or all false) → fold to constant. If instantiations disagree → leave unchanged. Uses Cecil's `TypeReferenceEqualityComparer.AreEqual()` for type comparison.

**Tests:** `TypeOfComparisonGenericTypes.cs` — 5 test scenarios:
- Method-level generic dead branch (typeof(T)==typeof(float) with T={int,byte} → always false → folded)
- Type-level generic dead branch (MyGenericType<T> with typeof(T)==typeof(long) → folded)
- Method-level generic alive branch (typeof(T)==typeof(int) with T={int,byte} → disagree → NOT folded)
- Multiple dead branches (float/double folded, int/byte kept)
- Updated Phase 1 tests: open-generic methods with concrete instantiations now correctly fold

**Regressions:** 0. Full suite: 1126/1160 passed, 1 pre-existing failure, 33 skipped. All 23 UnreachableBlock tests pass.

**Known limitations:**
- Methods only reachable from dead typeof branches are still marked (marking happens before re-optimization)
- Duplicate instantiations in the collection lists (harmless, minor perf overhead)
- Methods with both type-level AND method-level generic parameters: if the wrong kind of provider is encountered during inflation, the parameter stays unresolved → safe bail-out (returns null)

---

## 9. Next Steps

- [ ] Measure: Quantify IL bytes saved on a real WASM app with typeof(T) dispatch patterns
- [ ] Investigate Phase 3: Two-pass marking or deferred body scanning for full trimming of dead-branch members
- [ ] File GitHub issue to discuss approach with ILLink maintainers
- [ ] Consider: Should TypeofOptimizationStep be conditional on optimization being enabled?
