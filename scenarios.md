# ILSplit — Test Scenarios

Organized by implementation step (see [plan.md](plan.md) Implementation Order).
Each scenario has a risk level: **H**igh, **M**edium, **L**ow.

---

## Step 0: Input Validation

| # | Scenario | Risk | Notes |
|---|----------|------|-------|
| 0.1 | Reject R2R (ReadyToRun) assemblies | H | R2R native code contains inlined methods; splitting invalidates native image metadata sync. Check `HasReadyToRunHeader()`. |
| 0.2 | Reject mixed-mode (C++/CLI) assemblies | H | Non-IL-only assemblies have VTableFixups or native entry points — cannot split. |
| 0.3 | Accept IL-only assemblies | H | Verify `ILOnly` flag set, `VTableFixups.Size == 0`, `ExportAddressTableJumps.Size == 0`. |
| 0.4 | Reject assemblies with fewer types than clusters | L | Single-type assembly — nothing to split. |
| 0.5 | Handle strong-named input assemblies | H | **Deferred (known issue).** Splitting invalidates the signature. v1 produces unsigned output. |

---

## Step 2: DependencyGraph

### 2A — Edge Discovery (type A depends on type B)

| # | Scenario | Risk | Notes |
|---|----------|------|-------|
| 2.1 | Base type edge | H | `class Derived : Base` → edge Derived→Base. |
| 2.2 | Interface implementation edge | H | `class Foo : IBar` → edge Foo→IBar. |
| 2.3 | Multiple interface edges | M | Class implementing 3+ interfaces produces edges to each. |
| 2.4 | Field type edge | H | `MyType _field;` → edge from declaring type to field type. |
| 2.5 | Method parameter type edge | H | `void Foo(OtherType x)` → edge to OtherType. |
| 2.6 | Method return type edge | H | `OtherType Foo()` → edge to OtherType. |
| 2.7 | Method body — `newobj` operand | H | `new OtherType()` in IL → edge to OtherType. |
| 2.8 | Method body — `call`/`callvirt` operand | H | Calling method on OtherType → edge to OtherType (via DeclaringType). |
| 2.9 | Method body — `ldfld`/`stfld` operand | M | Field access on OtherType → edge. |
| 2.10 | Method body — `ldtoken` (typeof) | M | `typeof(OtherType)` → edge. |
| 2.11 | Method body — `ldftn`/`ldvirtftn` (delegates) | M | Delegate creation targeting method in OtherType → edge. |
| 2.12 | Custom attribute type edge | M | `[MyAttribute]` on a type → edge to MyAttribute. |
| 2.13 | Custom attribute constructor argument edge | M | `[MyAttr(typeof(OtherType))]` → edge to OtherType. |
| 2.14 | Generic type argument edge | H | `List<OtherType>` in field/param/body → edge to OtherType. |
| 2.15 | Generic constraint edge | H | `where T : OtherType` → edge to OtherType. |
| 2.16 | Generic constraint — `new()` | L | `where T : new()` — no type edge, but must preserve constraint. |
| 2.17 | Nested type → declaring type (bidirectional) | H | Nested types always co-located. Edge must be bidirectional. |
| 2.18 | Generic nested type inheriting parent's generic params | H | `Outer<T>.Inner` shares T — must stay together. |
| 2.19 | Property type edge | M | `OtherType Prop { get; set; }` → edge. |
| 2.20 | Event handler type edge | M | `event EventHandler<OtherType> E;` → edge. |
| 2.21 | P/Invoke return type edge | M | `[DllImport] OtherType Foo()` → edge, plus marshaling struct fields preserved. |
| 2.22 | P/Invoke parameter struct edge | M | Struct passed to P/Invoke — all fields are dependency edges (marshaling). |
| 2.23 | Explicit interface implementation (MethodImpl) | H | `void IFoo.Bar()` → edge to IFoo. MethodImpl record tied to declaring type. |
| 2.24 | Default interface method (DIM) | M | Interface with implementation body → edge to types used in DIM body. |
| 2.25 | Static abstract interface member | M | `static abstract void Foo();` in interface → implementor has implicit edge. |
| 2.26 | `ByRefLike` / `ref struct` type usage | L | Span-like types — edge like any other type, but cannot box. |
| 2.27 | Local variable types (StandAloneSig) | M | `OtherType x;` in method body → edge via local signature. |
| 2.28 | Catch clause exception type | M | `catch (SomeException)` → edge to exception type. |
| 2.29 | Covariant/contravariant generic interface usage | M | `IEnumerable<out T>` — variance affects type identity but edge is the same. |
| 2.30 | Cross-assembly type references (external) are ignored | H | Only edges between types **within the same assembly** matter. External TypeRefs are not graph nodes. |

### 2B — Graph Integrity

| # | Scenario | Risk | Notes |
|---|----------|------|-------|
| 2.31 | No types in assembly (empty module) | L | Edge case — should produce no graph. |
| 2.32 | Single type, no internal dependencies | L | Trivial graph — one node, no edges. |
| 2.33 | `<Module>` type (global functions) | M | Module type always exists. Must be included as a node if it has methods. |
| 2.34 | Compiler-generated types (`<>c`, display classes) | M | Lambda closures, async state machines — must follow edges to their parent types. |
| 2.35 | Large assembly (1000+ types) | M | Performance: graph construction should be O(types × avg_edges). |

---

## Step 3: ClusterStrategy

### 3A — SCC (Tarjan's Algorithm)

| # | Scenario | Risk | Notes |
|---|----------|------|-------|
| 3.1 | Two types in a cycle (A→B→A) | H | Must be in same SCC, same cluster. |
| 3.2 | Three-type cycle (A→B→C→A) | H | All three in same SCC. |
| 3.3 | Nested cycles sharing a node | M | Overlapping cycles form one SCC. |
| 3.4 | No cycles — all types independent | M | Each type is its own SCC (trivial). |
| 3.5 | Entire assembly is one big SCC | M | Worst case — can't split. Produces single cluster. |

### 3B — Hot/Cold Partitioning

| # | Scenario | Risk | Notes |
|---|----------|------|-------|
| 3.6 | Profile marks 3 types hot → transitive closure pulls in 2 more | H | Hot cluster = 5 types. Verify transitive deps included. |
| 3.7 | Hot type depends on cold type → cold type promoted to hot | H | Transitive closure must follow dependency edges. |
| 3.8 | All types are hot | M | Single hot cluster, no cold clusters. |
| 3.9 | No types are hot (empty profile) | M | Everything cold. No hot cluster (or hot cluster = just `<Module>`). |
| 3.10 | Profile contains type names not in the assembly | L | Silently ignore unknown names. |
| 3.11 | Hot type cycle with cold types | M | SCC containing mix → entire SCC becomes hot. |

### 3C — Cluster Merging

| # | Scenario | Risk | Notes |
|---|----------|------|-------|
| 3.12 | SCCs below 100 KB merged into larger clusters | H | Minimum threshold enforcement. |
| 3.13 | All cold SCCs below threshold → merge into one cold cluster | M | Degenerate case: hot + 1 cold cluster. |
| 3.14 | One SCC exceeds 100 KB alone | L | Becomes its own cluster without merging. |
| 3.15 | Cluster merging respects dependency edges (merge connected SCCs first) | M | Prefer merging SCCs that reference each other to reduce cross-cluster refs. |

### 3D — Namespace Fallback (No Profile)

| # | Scenario | Risk | Notes |
|---|----------|------|-------|
| 3.16 | Types grouped by top-level namespace, then by SCC | M | `System.IO.*` → cluster, `System.Net.*` → cluster. |
| 3.17 | Types in same namespace but different SCCs | M | Split within namespace is allowed if no cycle. |
| 3.18 | Single namespace — all types in one namespace | L | Fallback produces fewer clusters. |

---

## Step 4: ProfileReader

| # | Scenario | Risk | Notes |
|---|----------|------|-------|
| 4.1 | Parse simple text file, one class per line | H | `System.String` → matches `TypeDef` with FullName. |
| 4.2 | Blank lines and `#` comment lines ignored | L | Standard convention. |
| 4.3 | Generic type names with arity suffix | H | `System.Collections.Generic.List`1` must match correctly. |
| 4.4 | Nested type names with `/` or `+` separator | M | `Outer/Inner` or `Outer+Inner` — define canonical format. |
| 4.5 | Whitespace trimming | L | Leading/trailing spaces stripped. |
| 4.6 | Duplicate entries | L | Deduplicate silently. |
| 4.7 | Empty file | L | No hot types → namespace fallback. |
| 4.8 | BOM handling (UTF-8 with BOM) | L | Common on Windows-generated text files. |

---

## Step 5: AssemblyRewriter

This is the largest and riskiest step.

### 5A — PE Header & Assembly Identity

| # | Scenario | Risk | Notes |
|---|----------|------|-------|
| 5.1 | Output assemblies have `MajorRuntimeVersion=2`, `MinorRuntimeVersion=5` | H | Runtime rejects other versions with `COR_E_BADIMAGEFORMAT`. |
| 5.2 | IL-Only flag preserved on all outputs | H | `VTableFixups.Size == 0`, no native entry point. |
| 5.3 | Entry point token: type containing `Main` pinned in forwarder shell | H | Entry point type is never moved to a chunk. Forwarder shell keeps the entry point MethodDef. No resolution issues. |
| 5.3b | Assembly without entry point (class library) | M | EntryPointToken = `mdTokenNil`. No pinning needed. |
| 5.4 | New unique MVID per output module | H | Debuggers, PDBs, E&C depend on MVID uniqueness. |
| 5.5 | Assembly version/culture copied from original | M | All chunk assemblies share the same culture, but have distinct names (`*.0`, `*.1`). |
| 5.6 | Strong-name: output unsigned or delay-signed | H | Original strong-name signature invalidated by metadata changes. |
| 5.7 | PublicKeyToken preserved in forwarder shell's identity | M | External AssemblyRefs still match the original identity. |

### 5B — Type Forwarder Creation

| # | Scenario | Risk | Notes |
|---|----------|------|-------|
| 5.8 | Basic type forwarder: non-generic class | H | `ExportedType` with `TypeAttributes.Forwarder` pointing to chunk assembly. |
| 5.9 | Type forwarder for generic type (`List<T>`) | H | Forwarder uses open generic name (`List`1`); runtime resolves generic instantiations through it. |
| 5.10 | Type forwarder for nested type | H | Nested type forwarder needs `Implementation` pointing to enclosing `ExportedType`, not directly to assembly. |
| 5.11 | Multi-level nested type forwarder (`A.B.C.D`) | H | Chain: `D` → `C` → `B` → `A` → chunk assembly. |
| 5.12 | Forwarder shell has no type bodies | M | All MethodDef, FieldDef, etc. removed. Only ExportedType entries remain. |
| 5.13 | Forwarder for interface type | M | Interface types forward the same way as classes. |
| 5.14 | Forwarder for struct (value type) | M | Same mechanism; `ExportedType` doesn't distinguish class vs struct. |
| 5.15 | Forwarder for enum type | M | Enum forwards like any type. |
| 5.16 | Forwarder for delegate type | M | Delegate types are classes — standard forwarding. |
| 5.17 | No transitive forwarder chains | M | Shell → chunk is one hop. No A→B→C chains produced. |

### 5C — Cross-Cluster Reference Metadata

| # | Scenario | Risk | Notes |
|---|----------|------|-------|
| 5.18 | TypeRef created for type in other cluster | H | Each chunk has TypeRef + AssemblyRef for cross-cluster types. |
| 5.19 | MemberRef for method in other cluster | H | `MemberRef.Class` = TypeRef pointing to other chunk assembly. |
| 5.20 | MemberRef for field in other cluster | M | Same as method — field access across clusters. |
| 5.21 | Circular AssemblyRef between two chunks | H | `MyApp.0.dll` ←→ `MyApp.1.dll` — valid at runtime. Must verify both Mono and CoreCLR load correctly. |
| 5.22 | TypeSpec for generic instantiation across clusters | H | `List<TypeInOtherCluster>` → TypeSpec with TypeRef operand pointing to other chunk. |
| 5.23 | Minimal AssemblyRef set per chunk | M | Only include AssemblyRefs actually needed by the chunk's types. |
| 5.24 | External AssemblyRefs preserved | M | References to `System.Runtime`, `System.Collections`, etc. copied as-is. |

### 5D — Type Movement & IL Fixup

| # | Scenario | Risk | Notes |
|---|----------|------|-------|
| 5.25 | Move simple class (fields + methods, no inheritance) | H | Baseline case — type appears in chunk with correct MethodDef, FieldDef. |
| 5.26 | Move class with base type in same cluster | H | `extends` clause references TypeDef (internal). |
| 5.27 | Move class with base type in different cluster | H | `extends` clause must become TypeRef to other chunk. |
| 5.28 | Move class implementing interface in different cluster | H | `InterfaceImpl` row references TypeRef for interface. |
| 5.29 | Move class with virtual override — base in same cluster | M | `MethodImpl` points to TypeDef. |
| 5.30 | Move class with virtual override — base in different cluster | H | `MethodImpl.MethodDeclaration` must be MemberRef to other chunk. |
| 5.31 | Move abstract class — concrete impl in different cluster | M | Abstract class doesn't need the impl; impl needs abstract class ref. |
| 5.32 | Move nested type with declaring type (always together) | H | NestedClass table row: both types in same chunk. |
| 5.33 | Move generic type with constraints referencing other cluster | H | GenericParamConstraint → TypeRef in other chunk. |
| 5.34 | Move generic type: preserve GenericParam names and attributes | M | `variance`, `ReferenceTypeConstraint`, `NotNullableValueTypeConstraint` flags. |
| 5.35 | Move type with `[StructLayout(LayoutKind.Explicit)]` | M | ClassLayout and FieldLayout table rows must follow the type. |
| 5.36 | Move type with `[FieldOffset]` on fields | M | FieldLayout entries follow fields. |
| 5.37 | Move type with DeclSecurity attribute | L | DeclSecurity rows follow the type. |
| 5.38 | Move type with FieldMarshal (interop) | M | FieldMarshal entries follow fields for P/Invoke structs. |
| 5.39 | Move type with P/Invoke methods (`[DllImport]`) | M | ImplMap entries + ModuleRef must follow. |
| 5.40 | Move type with constant (literal) fields | M | Constant table rows follow the field. |
| 5.41 | Move type with default parameter values | L | Constant table rows for parameters follow. |
| 5.42 | IL operand remapping: all method body tokens updated | H | Every instruction referencing a moved type/method/field must use new token. |
| 5.43 | StandAloneSig (local variables) token remapping | H | Local variable signatures reference types — tokens must be remapped. |
| 5.44 | Custom attribute blob: `typeof(X)` argument where X moved | M | Binary blob contains encoded TypeRef — must point to correct chunk. |
| 5.45 | Method body exception handler: catch type remapping | M | Exception handler clause references a type token. |

### 5E — Special Types & Members

| # | Scenario | Risk | Notes |
|---|----------|------|-------|
| 5.46 | `<Module>` global type stays in hot cluster | H | Module initializer (.cctor) must run once. Cannot split `<Module>`. |
| 5.47 | Module initializer references types in cold clusters | M | `.cctor` body may call methods in cold cluster → cross-cluster MemberRef. |
| 5.48 | `InternalsVisibleTo` replicated on all chunk assemblies | H | Internal types must be accessible from friend assemblies across all chunks. |
| 5.49 | Assembly-level custom attributes on forwarder shell | M | `[assembly: X]` attributes stay on the forwarder shell (original identity). |
| 5.50 | Assembly-level custom attributes referencing moved types | M | Attribute constructor or arg may reference moved type → TypeRef. |
| 5.51 | Compiler-generated types (closures, state machines) | H | `<>c`, `<Foo>d__1` display classes — must follow their parent method's type. |
| 5.52 | Static constructor `.cctor` per type | M | Moves with its declaring type. Runtime guarantees `.cctor` runs before access regardless of assembly. `beforefieldinit` flag preserved. |
| 5.53 | Enum backing field + members | M | Enum `value__` field + named constants — all move together as one type. |
| 5.54 | Delegate `Invoke`/`BeginInvoke`/`EndInvoke` methods | M | Delegate type is a single unit — all methods move together. |
| 5.55 | PropertyMap / EventMap tables follow types | M | Property and event metadata rows linked to their declaring type. |

### 5F — Resources (v1: all stay in forwarder shell)

| # | Scenario | Risk | Notes |
|---|----------|------|-------|
| 5.56 | All embedded resources stay in forwarder shell (v1 policy) | M | Simplest approach. `Assembly.GetManifestResourceStream` on the original assembly name still works because the forwarder shell retains its identity. |
| 5.57 | Chunk assemblies have no embedded resources | L | Verify chunks load cleanly without resource section. |
| 5.58 | Satellite resource assemblies not affected | L | Separate assemblies — ILSplit doesn't touch them. |

---

## Step 6: ManifestWriter

| # | Scenario | Risk | Notes |
|---|----------|------|-------|
| 6.1 | Correct JSON schema with version field | L | `"version": 1` for forward compatibility. |
| 6.2 | Cluster list with eager/lazy flags | M | Hot cluster marked `"eager": true`, all others `false`. |
| 6.3 | `typeToCluster` map complete and consistent | M | Every type in the original assembly appears exactly once. |
| 6.4 | Cluster `sizeBytes` accurate | L | Sum of IL method body sizes + metadata for the cluster. |
| 6.5 | Assembly with no split (below threshold) | L | Manifest says single cluster = original assembly, no splitting occurred. |

---

## Step 7: Program.cs (CLI)

| # | Scenario | Risk | Notes |
|---|----------|------|-------|
| 7.1 | Split single assembly with profile | M | Happy path: `ilsplit --input Foo.dll --profile hot.txt --output ./out`. |
| 7.2 | Split single assembly without profile | M | Namespace fallback clustering. |
| 7.3 | Split multiple assemblies in one invocation | M | `--input Foo.dll --input Bar.dll` or glob pattern. |
| 7.4 | Error on non-existent input file | L | Clear error message. |
| 7.5 | Error on R2R / mixed-mode input | L | Reject with explanation. |
| 7.6 | Dry-run mode (show clusters without writing) | L | Useful for tuning the profile. |

---

## Step 9: Round-Trip Validation

| # | Scenario | Risk | Notes |
|---|----------|------|-------|
| 9.1 | Split assembly → load forwarder shell → resolve type via forwarder → instantiate | H | Core validation: type forwarders actually work at runtime. |
| 9.2 | Cross-cluster method call | H | Type in cluster 0 calls method on type in cluster 1. |
| 9.3 | Cross-cluster field access | M | Type in cluster 0 reads field from type in cluster 1. |
| 9.4 | Cross-cluster inheritance (base in different cluster) | H | Derived type loads, calls `base.Method()`. |
| 9.5 | Cross-cluster interface implementation | H | Cast to interface defined in cluster 0, call method implemented in cluster 1. |
| 9.6 | Cross-cluster generic instantiation | H | `new List<TypeInCluster1>()` from code in cluster 0. |
| 9.7 | Reflection: `Type.GetType("Namespace.TypeName, OriginalAssembly")` resolves via forwarder | H | Reflection uses assembly-qualified name with original assembly identity. |
| 9.8 | Reflection: enumerate types in chunk assembly | M | `chunk.GetTypes()` returns moved types. |
| 9.9 | `Assembly.GetManifestResourceStream` after split | M | Resource must be in the correct chunk assembly. |
| 9.10 | `typeof(X).Assembly` returns chunk assembly, not forwarder shell | M | **Known issue (deferred).** After split, `typeof(X).Assembly.GetName().Name` is `"MyApp.0"`, not `"MyApp"`. Code comparing assembly names will break. Documented. |
| 9.11 | Serialization round-trip (JSON/XML with type names) | M | Serialized type name includes assembly — may need original assembly name. |
| 9.12 | Validate on Mono runtime | H | Primary WASM target. |
| 9.13 | Validate on CoreCLR | M | Secondary — ensures tool is runtime-agnostic. |

---

## Step 12: End-to-End WASM App

| # | Scenario | Risk | Notes |
|---|----------|------|-------|
| 12.1 | Sample Blazor WASM app builds with `WasmEnableILSplit=true` | H | Full pipeline: ILLink → ILSplit → Webcil → bundle. |
| 12.2 | App starts and renders UI correctly | H | Hot cluster loads at startup, cold clusters load on demand. |
| 12.3 | Navigation to page using cold-cluster types works | H | Lazy loading triggers cold cluster download + assembly load. |
| 12.4 | Total download size (hot cluster only) < original assembly size | M | Quantify the savings — this is the point of the tool. |
| 12.5 | Total size (all clusters + shell) overhead < 15% of original | M | Metadata duplication tax is acceptable. |

---

## Step 13: System.Private.CoreLib Splitting

| # | Scenario | Risk | Notes |
|---|----------|------|-------|
| 13.1 | VM-referenced types pinned in hot cluster | H | Types listed in `src/coreclr/vm/corelib.h` (`DEFINE_CLASS` macros, ~300 types) and `mono_defaults` / `ILLink.Descriptors.xml` (~100+ types) must remain in hot cluster (cluster 0). |
| 13.2 | CoreLib forwarder shell accepted by Mono runtime | H | Mono has special-case handling for CoreLib in the loader. The forwarder shell must still be recognized as "the" CoreLib. |
| 13.3 | CoreLib chunk assemblies loadable alongside forwarder shell | H | Runtime must not reject unknown assemblies containing types normally in CoreLib. |
| 13.4 | `System.Object`, `System.String`, `System.Int32` always in hot cluster | H | These are in the VM-referenced set — verify they are never placed in cold clusters. |
| 13.5 | Exception types (`System.Exception`, `System.NullReferenceException`) pinned | H | Runtime throws these internally — must be in hot cluster. Part of the VM-referenced set. |
| 13.6 | `System.Type`, `System.Reflection.*` types pinned | M | Reflection types used by the runtime itself — in VM-referenced set. |
| 13.7 | GC and threading types (`System.GC`, `System.Threading.Thread`) pinned | M | Runtime P/Invokes into these — in VM-referenced set. |
| 13.8 | `System.Runtime.CompilerServices.*` types (used by JIT/AOT) pinned | M | Compiler services types used during code generation. |
| 13.9 | Cold CoreLib types (not VM-referenced) successfully moved to chunks | H | Types only used by managed code (e.g., `System.Xml.*`, `System.Text.RegularExpressions.*` if present) can safely move. |
| 13.10 | Reflection on cold CoreLib types returns correct metadata | M | `typeof(ColdType).Assembly` returns the chunk assembly — known issue but should not crash. |

---

## Cross-Cutting Concerns

These apply across multiple steps.

| # | Scenario | Risk | Step | Notes |
|---|----------|------|------|-------|
| CC.1 | Assembly with 0 splittable types (only `<Module>`) | L | 3, 5 | No split possible — output original unchanged. |
| CC.2 | Assembly where all types form one SCC | M | 3, 5 | Cannot split — output original unchanged. |
| CC.3 | Very large assembly (10,000+ types) | M | 2, 3, 5 | Performance: must complete in reasonable time. |
| CC.4 | Assembly with debug symbols (PDB) | M | 5 | **Deferred.** v1 does not generate PDBs for split assemblies. Existing PDB becomes invalid (MVID mismatch). |
| CC.5 | Deterministic output | L | 5, 6 | Same input + same profile → same output (bit-for-bit). Requires deterministic MVID generation. |
| CC.6 | Unicode type names / non-ASCII characters | L | 2, 4, 5 | Metadata is UTF-8 — ensure no encoding issues. |
| CC.7 | Obfuscated assemblies (mangled names) | L | 4 | Profile matching by name fails — but graph + SCC still works. |
| CC.8 | Empty method bodies (abstract, extern, runtime) | L | 2, 5 | No IL to scan for edges; no IL to rewrite. |
