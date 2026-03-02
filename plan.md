# ILSplit — IL Assembly Splitter Plan

## Goal

Build a post-ILLink tool that splits trimmed .NET assemblies into smaller DLLs, enabling on-demand (lazy) loading at runtime. Primary target: **WASM browser apps** where network download cost dominates startup time. For most assemblies, the tool produces standard .NET assemblies with type forwarders that work on any runtime. For `System.Private.CoreLib`, targeted VM/binder changes in CoreCLR enable splitting of the largest framework assembly.

## Key Insight: Profile-Guided Splitting

A text file listing class names that are used during typical app startup determines **which types belong in the "hot" (eagerly loaded) chunk** vs. "cold" (lazy loaded) chunks. This groups types by actual co-usage patterns rather than by namespace.

## Scope

- **ILSplit tool** — splits assemblies into chunks using type forwarders.
- **CoreCLR VM fix** — relax Debug assertions in `CoreLibBinder` for types resolved from chunk modules. No binder changes needed; the existing type forwarder + binder path already handles lazy loading.
- **Any assembly** can be split, including `System.Private.CoreLib.dll` and framework assemblies.
- **Type forwarders** preserve the original assembly name so external references resolve without rewriting.
- On-demand loading is handled externally (e.g., JSPI for WASM). The tool's job is to produce valid split assemblies, not to manage async loading.

---

## Decisions

| Question | Decision |
|---|---|
| Runtime changes? | **Minimal — CoreCLR VM only.** Relax 2 Debug assertions in `CoreLibBinder` (`binder.cpp` lines 99, 239). No binder changes. The existing type forwarder + assembly binder path already supports lazy chunk loading — the binder is fully initialized before `LoadBaseSystemClasses()`. |
| Profile input | **Text file** with one class name per line (v1). `.mibc` support later. |
| Which assemblies? | **Any**, including `System.Private.CoreLib`. |
| Split mechanism | **Type forwarders** for all assemblies, including CoreLib. The binder is initialized before `LoadBaseSystemClasses()`, so standard type forwarder resolution works. Cold chunks load lazily on first access. |
| Async loading | **Out of scope.** JSPI handles it on the WASM side. |
| Pipeline position | ILLink → **ILSplit** → Webcil → Bundle |
| Granularity | **Auto-clustering**, driven by the profile class list. |
| Entry point type | **Pinned** in the hot chunk (`MyApp.0.dll`) — never moved to cold clusters. |
| Resources (v1) | **All embedded resources stay in the forwarder shell.** |
| CoreLib VM types | **Pinned** in hot cluster — types referenced by VM native code (`corelib.h`, `mono_defaults`) are never moved to cold clusters. |
| Strong names | **Deferred.** Strong-name handling is a known issue for later. |
| PDB / debug symbols | **Deferred.** Not generated for split assemblies in v1. |

---

## Architecture Overview

```
dotnet publish
    │
    ▼
 ILLink trim          (existing — removes dead code)
    │
    ▼
 ILSplit              (NEW — splits assemblies + generates manifest)
    │
    ├── MyApp.0.dll         (hot: startup classes)
    ├── MyApp.1.dll         (cold cluster 1)
    ├── MyApp.2.dll         (cold cluster 2)
    ├── MyApp.dll            (thin shell — type forwarders only)
    └── ilsplit-manifest.json
    │
    ▼
 Webcil conversion    (existing — wraps DLLs as .wasm)
    │
    ▼
 WasmAppBuilder       (existing — generates boot config + app bundle)
```

The output is a **drop-in replacement**: the original `MyApp.dll` still exists (as a forwarder shell), so all existing `AssemblyRef`s from other assemblies resolve without modification. The runtime follows the type forwarders to the actual chunk assembly.

**CoreLib special case**: `System.Private.CoreLib.dll` is also split into shell + chunks. The existing type forwarder resolution works during bootstrap because the `DefaultAssemblyBinder` IS fully initialized before `LoadBaseSystemClasses()` runs. The hot chunk (`.0.dll`) contains all ~2357 VM-pinned types and is loaded on-demand during bootstrap when `CoreLibBinder::GetClass()` follows type forwarders. Cold chunks are **never loaded during bootstrap** — they load lazily when managed code first accesses a cold type. Only minor Debug assertion fixes are needed in `CoreLibBinder`. See Phase 4 for details.

**Dependency orientation:** The forwarder shell contains only `ExportedType` entries pointing to **all** chunk assemblies (including hot). The hot chunk (`MyApp.0.dll`) has **no** type forwarders and **no** `AssemblyRef` to cold chunks. Cold chunks reference the hot chunk (and possibly other cold chunks) via `AssemblyRef` + `TypeRef`, forming a **DAG** where hard dependencies always flow from children (cold) toward the root (hot) — never from hot to cold. If they go from cold to cold, they never form a cycle.

---

## Phase 1: Core Tool — Cecil-based Assembly Splitter

### 1.1 Project Structure

```
src/tools/ilsplit/
├── src/
│   ├── ILSplit/
│   │   ├── ILSplit.csproj              (main tool — console app)
│   │   ├── Program.cs                  (CLI entry point)
│   │   ├── SplitEngine.cs              (orchestrator)
│   │   ├── DependencyGraph.cs          (type dependency graph builder)
│   │   ├── ProfileReader.cs            (reads text class list; .mibc later)
│   │   ├── ClusterStrategy.cs          (SCC + profile-guided clustering)
│   │   ├── AssemblyRewriter.cs         (creates split assemblies + type forwarders)
│   │   └── ManifestWriter.cs           (writes ilsplit-manifest.json)
│   ├── ILSplit.Tasks/
│   │   ├── ILSplit.Tasks.csproj        (MSBuild task wrapper)
│   │   └── ILSplitTask.cs             (MSBuild task)
│   └── ILSplit.Tests/
│       ├── ILSplit.Tests.csproj
│       ├── DependencyGraphTests.cs
│       ├── ClusterStrategyTests.cs
│       └── SplitRoundtripTests.cs
```

### 1.2 Dependencies

| Dependency | Purpose | Source |
|---|---|---|
| Mono.Cecil | IL reading, writing, type-forwarder creation | NuGet (same version as ILLink) |
| System.CommandLine | CLI parsing | NuGet |
| System.Text.Json | Manifest serialization | In-box |

### 1.3 DependencyGraph — Type-Level Dependency Analysis

Build a directed graph where each node is a `TypeDefinition` (from a single assembly being split) and edges represent "type A needs type B to be loaded":

**Edge sources:**
- Base type / interfaces
- Field types
- Method signatures (parameters, return type)
- Method body IL operands (type refs, method refs, field refs)
- Custom attributes
- Generic type arguments / constraints
- Nested types → declaring type (bidirectional, always co-located)

**Key Cecil patterns (from ILLink codebase):**
```csharp
// Reading assemblies (memory-mapped, same as ILLink's AssemblyResolver)
var readerParams = new ReaderParameters { ReadSymbols = false, ReadWrite = false };
var assembly = AssemblyDefinition.ReadAssembly(path, readerParams);

// Iterating types
foreach (var type in assembly.MainModule.Types) { /* top-level */ }
foreach (var nested in type.NestedTypes) { /* nested */ }

// Examining method bodies
foreach (var instr in method.Body.Instructions)
{
    switch (instr.Operand)
    {
        case TypeReference tr: /* type usage */ break;
        case MethodReference mr: /* mr.DeclaringType */ break;
        case FieldReference fr: /* fr.DeclaringType */ break;
    }
}

// Writing assemblies (same as ILLink's OutputStep)
assembly.Write(outputPath, new WriterParameters { ... });

// Creating type forwarders
var exportedType = new ExportedType(ns, name, module, scope) {
    Attributes = TypeAttributes.Forwarder
};
module.ExportedTypes.Add(exportedType);
```

### 1.4 Profile-Guided Clustering

**Input:** A text file with one class name per line — classes that are used at startup (or during a profiled scenario).

```
# hot-classes.txt — classes used during startup
System.String
System.Object
System.Int32
System.Collections.Generic.List`1
MyApp.Program
MyApp.Startup
MyApp.Services.AuthService
```

**Algorithm:**
1. Parse the text file to get a set of hot class names.
2. For each assembly being split, mark types whose full name appears in the hot set.
3. Compute the transitive closure of hot types' dependencies **within the same assembly** → these form the **hot cluster** (cluster 0, loaded eagerly at startup).
4. Remaining types: run **Tarjan's SCC algorithm** on the dependency subgraph to identify strongly connected components (types in an SCC must stay together to avoid intra-cluster circular refs).
5. Merge small SCCs into larger clusters until each cluster exceeds a minimum size threshold (default: 100 KB estimated IL size).
6. The result is a partition: `{ hot_cluster, cold_cluster_1, cold_cluster_2, ... }`.

**Without a profile:** Fall back to namespace-based clustering with SCC merging.

**Future:** Add `.mibc` profile reader that extracts method tokens and maps them back to containing types.

### 1.5 Assembly Rewriter

For each input assembly being split, the tool produces three tiers of output:

1. **Forwarder shell** (`OriginalName.dll`) — a thin compatibility shim:
   - Keeps the original assembly name so external `AssemblyRef`s still resolve
   - Contains **only** `ExportedType` entries (type forwarders) pointing to the chunk that owns each type — including hot types in `OriginalName.0.dll`
   - All embedded resources stay in the shell (v1)
   - Contains `AssemblyRef` entries to every chunk assembly (needed as scope for the forwarders)
   - These `AssemblyRef` entries are **not hard dependencies** — they are only resolved lazily when a forwarded type is actually requested

2. **Hot chunk** (`OriginalName.0.dll`) — eagerly loaded at startup:
   - Contains hot `TypeDef`s (profile-selected + transitive closure)
   - Has **no** type forwarders (`ExportedType`) — it is a leaf in the forwarder chain
   - Has **no** `AssemblyRef` to cold chunk assemblies — cold types are unreachable from hot IL because the transitive closure guarantees all hot type dependencies are self-contained
   - This is the **root** of the hard-dependency DAG

3. **Cold chunks** (`OriginalName.1.dll`, `OriginalName.2.dll`, ...) — loaded on demand:
   - Contains cold `TypeDef`s grouped by SCC + minimum-size merging
   - Has `AssemblyRef` + `TypeRef` to the hot chunk (since cold types often reference hot types like `System.Object`, `System.String`, etc.)
   - May have `AssemblyRef` + `TypeRef` to other cold chunks — this is allowed and forms a **DAG** (directed acyclic graph) of hard dependencies
   - Has **no** type forwarders

4. **No changes to other assemblies** — type forwarders in the shell mean any existing `AssemblyRef` to the original assembly still resolves. The runtime follows the forwarder chain automatically.

#### Hard Dependency DAG

A "hard dependency" is an `AssemblyRef` + `TypeRef` that causes the runtime to load the referenced assembly when the referencing type is used. The split output forms an oriented DAG:

```
           ┌─────────────────────────────────────────────┐
           │  MyApp.dll  (FORWARDER SHELL — thin)        │
           │  ─ ExportedType → MyApp.0.dll (hot types)   │
           │  ─ ExportedType → MyApp.1.dll (cold types)  │
           │  ─ ExportedType → MyApp.2.dll (cold types)  │
           │  ─ Resources                                │
           │  ─ No TypeDefs (except <Module>)             │
           └──────────────────────────────────────────────┘
                  (forwarders only — not part of the DAG)

    ┌─────────────────┐
    │ MyApp.0.dll      │
    │ (HOT — root)     │
    │ ─ Hot TypeDefs   │
    │ ─ NO forwarders  │
    │ ─ NO AssemblyRef │
    │   to cold chunks │
    └─────────────────┘
           ▲                  ▲
           │ (hard dep)       │ (hard dep)
           │                  │
    ┌──────┴──────────┐  ┌────┴────────────┐
    │ MyApp.1.dll     │  │ MyApp.2.dll     │
    │ (COLD)          │  │ (COLD)          │
    │ ─ Cold TypeDefs │  │ ─ Cold TypeDefs │
    │ ─ AssemblyRef → │  │ ─ AssemblyRef → │
    │   MyApp.0       │  │   MyApp.0       │
    │ ─ AssemblyRef → │  │                 │
    │   MyApp.2       │  │                 │
    └─────────────────┘  └─────────────────┘
           │ (hard dep)
           └──────────────────┘
    Cold→cold references form a DAG (no cycles).
```

The hot chunk is the root — it never triggers loading of any cold assembly. Loading a cold chunk may trigger loading the hot chunk and/or other cold chunks, but only in a DAG pattern (no cycles, because SCCs are kept together).

#### Why the Hot Chunk Has No Cold Dependencies

The clustering algorithm computes a **transitive closure** of all hot type dependencies within the assembly. If any hot type references TypeX in a method body, field, or signature, TypeX is pulled into the hot cluster. This guarantees the hot chunk is **self-contained** — it has no `TypeRef` pointing to cold types and therefore no `AssemblyRef` to cold chunk assemblies.

This is critical: loading the hot chunk must never trigger loading of a cold chunk. The hot chunk is the root of the hard-dependency DAG.

#### Cold-to-Cold References and the DAG Property

Cold chunks may reference types in other cold chunks (e.g., a cold utility type used by another cold type). These cross-cold references create `AssemblyRef` + `TypeRef` edges between cold chunks, forming a **DAG** (directed acyclic graph).

The DAG property (no cycles between chunk assemblies) is enforced by **Tarjan's SCC algorithm**: types that mutually depend on each other are placed in the same SCC and therefore the same cluster. After SCC grouping, any remaining cross-cluster edge is guaranteed to be one-directional.

**The SCC algorithm** serves two purposes:
1. **Ensures the DAG property** — mutually dependent types stay together, preventing circular `AssemblyRef` between chunks
2. **Minimizes cross-cluster references** — each cross-ref adds metadata overhead (AssemblyRef + TypeRef rows), so types that heavily reference each other should stay together

**Complexity notes:**
- **Nested types** always stay with their declaring type
- **Generic types** with complex constraints need careful handling — clone the full GenericParameter set
- **Entry point type** (containing `Main`) is always pinned in the hot cluster (`MyApp.0.dll`). The forwarder shell's entry point token is not used — it is a pure forwarder.
- **Resources** (v1): all embedded resources stay in the forwarder shell. Future: assign resources to the chunk containing the type that references them.
- **Module initializers** stay in the hot cluster (cluster 0)
- `[InternalsVisibleTo]` attributes must be replicated across all cluster assemblies
- **`System.Private.CoreLib` specifics**: Both Mono and CoreCLR reference hundreds of CoreLib types directly from native VM code. These types **must be pinned in the hot cluster** (never moved to cold clusters):
  - **CoreCLR**: ~300 classes defined via `DEFINE_CLASS` macros in `src/coreclr/vm/corelib.h`
  - **Mono**: types referenced via `mono_defaults` struct and listed in `src/mono/System.Private.CoreLib/src/ILLink/ILLink.Descriptors.xml`
  - Common pinned set: `System.Object`, `System.String`, `System.Array`, all primitives, `System.Exception` hierarchy, `System.Delegate`, `System.Type`, `System.GC`, `System.RuntimeType/Handle`, reflection types, etc.
  - The tool must parse `corelib.h` and `ILLink.Descriptors.xml` (or maintain a hardcoded list) to identify VM-referenced types and force them into the hot cluster.

### 1.6 Manifest

```json
{
  "version": 1,
  "originalAssembly": "MyApp",
  "clusters": [
    {
      "name": "MyApp.0.dll",
      "eager": true,
      "types": ["MyApp.Program", "MyApp.Startup", "MyApp.Services.AuthService"],
      "sizeBytes": 45200
    },
    {
      "name": "MyApp.1.dll",
      "eager": false,
      "types": ["MyApp.Reports.ReportGenerator", "MyApp.Reports.PdfExporter"],
      "sizeBytes": 32100
    }
  ],
  "typeToCluster": {
    "MyApp.Program": "MyApp.0.dll",
    "MyApp.Startup": "MyApp.0.dll",
    "MyApp.Reports.ReportGenerator": "MyApp.1.dll"
  }
}
```

---

## Phase 2: MSBuild Integration

### 2.1 ILSplit MSBuild Task

Similar to how ILLink integrates (see `src/tools/illink/src/ILLink.Tasks/`):

```xml
<!-- ILSplit.targets — imported after ILLink -->
<Target Name="ILSplit"
        Condition="'$(WasmEnableILSplit)' == 'true'"
        AfterTargets="ILLink"
        BeforeTargets="_ConvertDllsToWebcil">

  <ILSplitTask
    InputAssemblies="@(_LinkedResolvedFileToPublish)"
    HotClassListPath="$(ILSplitProfilePath)"
    OutputDirectory="$(IntermediateILSplitDir)"
    MinClusterSize="$(ILSplitMinClusterSize)"
    AssembliesToSplit="@(ILSplitAssembly)">
    <Output TaskParameter="SplitAssemblies" ItemName="_ILSplitOutput" />
    <Output TaskParameter="ManifestPath" PropertyName="_ILSplitManifestPath" />
  </ILSplitTask>

  <!-- Replace original assemblies with split output -->
  <ItemGroup>
    <ResolvedFileToPublish Remove="@(ILSplitAssembly)" />
    <ResolvedFileToPublish Include="@(_ILSplitOutput)" />
  </ItemGroup>
</Target>
```

### 2.2 WASM SDK Integration

In `src/mono/wasm/build/WasmApp.Common.targets`, the pipeline is:

```
_WasmBuildAppCoreDependsOn:
  PrepareInputsForWasmBuild
  → _WasmResolveReferences
  → _WasmBuildNativeCore      (includes AOT, strip, native link)
  → WasmGenerateAppBundle
  → _EmitWasmAssembliesFinal
```

ILSplit inserts between ILLink output and Webcil conversion. The Webcil conversion happens inside `WasmGenerateAppBundle` via the `ConvertDllsToWebcil` task in the Blazor SDK.

**Integration point:**
```
ILLink → ILSplit → _WasmAssembliesInternal updated → Webcil conversion → bundle
```

The split chunk assemblies (`.0.dll`, `.1.dll`, ...) are added to the assembly list. The forwarder shell (original `.dll`) stays in the list. The existing boot config generation picks up all DLLs automatically — no boot config schema changes needed.

### 2.3 MSBuild Properties

| Property | Default | Description |
|---|---|---|
| `WasmEnableILSplit` | `false` | Enable assembly splitting |
| `ILSplitProfilePath` | (none) | Path to text file with hot class names (one per line) |
| `ILSplitMinClusterSize` | `102400` | Minimum cluster size in bytes (below this, merge with neighbors) |
| `@(ILSplitAssembly)` | all managed assemblies | Item group of assemblies to split (defaults to all) |

---

## Phase 3: Validation & Testing

### 3.1 Unit Tests
- `DependencyGraph`: Verify edge discovery (base types, interfaces, method body refs, generics)
- `ClusterStrategy`: Verify SCC computation, hot/cold partitioning, minimum size enforcement
- `AssemblyRewriter`: Round-trip test — split an assembly, load all clusters, verify all types are resolvable via type forwarders
- `ProfileReader`: Verify text class list parsing, matching against assembly types

### 3.2 Integration Tests
- Split a real trimmed assembly (e.g., `System.Private.CoreLib.dll`) and verify the forwarder shell + chunks load correctly on Mono
- Build a sample WASM app with ILSplit enabled, verify it runs
- Verify that circular cross-cluster references resolve at runtime

### 3.3 Size Regression Tests
- Compare total disk size (all chunks + forwarder shell) vs. original assembly — overhead should be < 15%
- Measure hot cluster size vs. original — this is the startup download savings

### 3.4 E2E Smoke Test (CoreCLR, x64 Windows)
- **Done.** Published a self-contained HelloWorld console app, ran ILSplit on all framework DLLs (excluding CoreLib), tested with `corerun.exe`.
- 258 assemblies split successfully, `Hello, World!` prints with exit code 0.
- CoreLib excluded — split CoreLib requires chunk assemblies on TPA and Debug assertion fix (see Phase 4).

---

## Phase 4: CoreCLR VM Changes for Split CoreLib

### 4.0 Problem Statement (Revised)

`System.Private.CoreLib.dll` is the largest managed assembly (~5.6 MB IL-only) and a high-value splitting target. Our initial experiment produced a CoreLib shell + 6 chunks but failed at runtime with `0x80070002` (file not found) during `coreclr_initialize`.

**Initial (wrong) diagnosis**: "The binder isn't initialized during CoreLib bootstrap, so type forwarders can't be resolved."

**Actual diagnosis after code review**: The `DefaultAssemblyBinder` IS fully initialized before `LoadBaseSystemClasses()` runs. The initialization order is:

```
coreclr_initialize
  → EEStartup
    → AppDomain::Init() + CreateDefaultBinder()   ← binder ready here
    → SystemDomain::Init()
      → LoadBaseSystemClasses()
        → PEAssembly::OpenSystem()                 ← loads CoreLib shell
        → DefaultDomain()->LoadAssembly(...)        ← partial load
        → CoreLibBinder::AttachModule()             ← registers module
        → CoreLibBinder::GetClass(CLASS__OBJECT)    ← first type lookup
          → LoadTypeByNameThrowing()
            → FindClassModuleThrowing()
              → EEClassHashTable lookup finds ExportedType
              → FindModuleByExportedType()
                → Module::LoadAssembly(mdAssemblyRef)  ← binder IS ready
                  → PEAssembly::LoadAssembly()
                    → AppDomain::BindAssemblySpec()
                      → DefaultAssemblyBinder::BindUsingAssemblyName()
                        → AssemblyBinderCommon::BindAssembly()  ← TPA search
```

The type forwarder resolution chain works — `FindModuleByExportedType` calls `LoadAssembly` which goes through the fully operational binder. The actual failure was that **chunk assemblies weren't on the TPA list** or in the probing path during the experiment.

**Key insight: cold chunks are inherently lazy.** During bootstrap, `CoreLibBinder::GetClass()` only loads VM-pinned types (Object, String, ValueType, etc.) — all of which are in the hot chunk. Cold chunk assemblies are never touched until managed code requests a cold type. The existing type forwarder mechanism already provides lazy, on-demand loading. **No pre-loading of chunks is needed.**

### 4.1 Design: Minimal VM Changes for Split CoreLib

The existing type forwarder resolution already supports lazy loading. Only two small VM fixes are needed:

#### 4.1.1 Fix CoreLibBinder Debug Assertions

`CoreLibBinder::LookupClassLocal` ([binder.cpp](src/coreclr/vm/binder.cpp#L99)) has:
```cpp
_ASSERTE(pMT->GetModule() == GetModule());
```

When a VM-pinned type (e.g., `System.Object`) is forwarded from the CoreLib shell to the hot chunk (`System.Private.CoreLib.0.dll`), the type's module is the chunk module, not the shell module. This assertion fails in Debug builds.

Similarly, `CoreLibBinder::GetClassIfExist` ([binder.cpp](src/coreclr/vm/binder.cpp#L239)):
```cpp
_ASSERTE((pMT == NULL) || (pMT->GetModule() == GetModule()));
```

**Fix**: Relax these assertions to allow the type's module to be any module belonging to the CoreLib assembly graph (shell or any chunk). The simplest approach:
```cpp
// Allow types in CoreLib chunk assemblies (split CoreLib via type forwarders)
_ASSERTE(pMT->GetModule() == GetModule() ||
         pMT->GetModule()->GetAssembly()->GetModule()->LoadAssembly(...) /* is chunk of CoreLib */);
```

Or more practically, just remove the assertion since it's a Debug-only check and the type is already correctly resolved.

#### 4.1.2 Chunk Assembly Probing

Chunk assemblies must be findable by the standard binder. No new probing logic is needed — the existing TPA list and app path probing in `AssemblyBinderCommon::BindAssembly` handles this. The MSBuild integration ensures chunks are included in the TPA list / deployment directory.

For the CoreLib case specifically, chunks should be placed **in the same directory as the CoreLib shell** (the system directory next to `coreclr.dll`, or on the TPA list). The binder already searches both locations.

#### 4.1.3 No Pre-Loading Required

The previous design (4.1 in the old plan) proposed pre-loading all CoreLib chunks during `LoadBaseSystemClasses`. This is **wrong** — it defeats lazy loading. The correct approach:

- **Hot chunk** (`System.Private.CoreLib.0.dll`): Loaded on-demand during bootstrap when `CoreLibBinder::GetClass(CLASS__OBJECT)` follows the type forwarder. Contains all ~2357 VM-pinned types and their transitive dependencies. Loaded once, early, via the normal binder path.
- **Cold chunks** (`System.Private.CoreLib.1.dll` through `.N.dll`): **Never loaded during bootstrap.** Only loaded when managed code first accesses a cold type (e.g., `System.Security.Cryptography.Aes`). This is true lazy loading — the binder resolves on first use.

**Bootstrap type loading flow (no VM changes needed here):**
1. `CoreLibBinder::GetClass(CLASS__OBJECT)` → `LoadTypeByNameThrowing`
2. Shell's `EEClassHashTable` finds `System.Object` as ExportedType → `mdtAssemblyRef` to hot chunk
3. `FindModuleByExportedType` → `Module::LoadAssembly` → binder finds `System.Private.CoreLib.0.dll`
4. Hot chunk assembly loaded, type resolved. Cached for all subsequent lookups.
5. All other VM-pinned type lookups hit the same hot chunk (already loaded).
6. Cold chunks remain unloaded until needed.

#### 4.1.4 Module Identity Considerations

Chunk modules have `Module::IsSystem() == false` (since `PEAssembly::Open` sets `isSystem=false`). Places this affects:

- **`SKIP_TYPE_VALIDATION`** (ceeload.cpp): CoreLib skips type validation for performance. Chunk modules won't get this flag. Minor perf impact, no correctness issue. Can optionally set flag for known chunk modules.
- **`ILStubCache`** (ceeload.cpp): Non-system modules use per-LoaderAllocator cache. Chunk modules will use this path. Correct behavior, no issue.
- **`AppDomain::BindAssemblySpec`** (appdomain.cpp): Has a special case `if (boundAssembly->GetAssemblyName()->IsCoreLib())` to avoid rebinding CoreLib. Chunk assemblies have different names (`System.Private.CoreLib.0`), so `IsCoreLib()` returns false. Correct — chunks ARE different assemblies and should go through normal binding.
- **`DefaultAssemblyBinder::BindAssemblyByNameWorker`** (defaultassemblybinder.cpp): Debug assert `_ASSERTE(!pAssemblyName->IsCoreLib())` — not triggered for chunk assemblies (different name). No issue.

**Conclusion**: No `IsSystem()` changes needed. Chunk modules behave correctly as regular (non-system) assemblies.

#### 4.1.5 What About the `Check()` and `CheckExtended()` Debug Methods?

`CoreLibBinder::Check()` runs in Debug builds at the end of `LoadBaseSystemClasses()`. It iterates types from `DEFINE_CLASS_U` macros (types with native layout). ALL such types are VM-pinned → in the hot chunk. `Check()` will load these types, which resolves type forwarders to the hot chunk. Cold chunks are not touched.

`CoreLibBinder::CheckExtended()` only runs when `INTERNAL_ConsistencyCheck` config is set. It iterates ALL binder classes/methods/fields. ALL binder-registered items are VM-pinned → in the hot chunk. Cold chunks still not touched.

**No cold chunks loaded during any Debug validation.**

### 4.2 Deployment: Chunk Assembly Placement

For CoreLib chunks to be found by the binder, they must be:
1. **On the TPA list** — the MSBuild integration already adds all chunk assemblies to the deployment output, and hosts like `corerun` build TPA from the app directory. OR
2. **Next to `coreclr.dll`** (system directory) — for self-contained deployments where CoreLib lives next to the runtime.

No binder code changes are needed. The existing `AssemblyBinderCommon::BindAssembly` searches TPA and app paths, which covers both scenarios.

### 4.3 Future: WASM Lazy Download via JSPI

For WASM targets, cold chunk loading can be combined with JSPI to enable true lazy downloading:

1. When resolving a type forwarder to a cold chunk, the binder detects the assembly isn't available locally.
2. The binder signals the JS host to fetch the chunk `.wasm` file over the network.
3. JSPI suspends the WebAssembly execution while the fetch completes.
4. The fetched assembly is registered with the binder and loading resumes.

This requires a callback mechanism: binder → managed → JS → fetch → binder resume. This is a larger change deferred to a future phase. The current ILSplit design produces the correct split assemblies — the WASM integration is purely about the delivery mechanism.

---

## Implementation Order

| Step | Description | Effort | Status |
|---|---|---|---|
| **1** | Scaffold project structure (`ILSplit.csproj`, `ILSplit.Tasks.csproj`, `ILSplit.Tests.csproj`) | Small | **Done** |
| **2** | `DependencyGraph.cs` — build type-level dependency graph from a Cecil `AssemblyDefinition` | Medium | **Done** |
| **3** | `ClusterStrategy.cs` — Tarjan SCC + namespace-based fallback clustering | Medium | **Done** |
| **4** | `ProfileReader.cs` — parse text file of hot class names | Small | **Done** |
| **5** | `AssemblyRewriter.cs` — create split assemblies + type forwarder shell | Large (hardest) | **Done** |
| **6** | `ManifestWriter.cs` — write JSON manifest | Small | **Done** |
| **7** | `Program.cs` — CLI entry point with System.CommandLine | Small | **Done** |
| **8** | Unit tests for steps 2-6 | Medium | 14 tests passing |
| **9** | Round-trip validation — split + load on Mono and CoreCLR | Medium | **Done** — 4 tests |
| **10** | `ILSplitTask.cs` — MSBuild task wrapper | Small | **Done** |
| **11** | WASM SDK targets integration | Medium | **Done** |
| **12** | E2E smoke test — publish + split + corerun on x64 Windows | Medium | **Done** — 258 assemblies, Hello World works |
| **13** | Fix `CoreLibBinder` Debug assertions for split CoreLib (`binder.cpp`) | Small | Not started |
| **14** | E2E test: split CoreLib + framework DLLs, verify Hello World with corerun | Medium | Not started |
| **15** | Performance measurement — size overhead, load time impact | Medium | Not started |
| **16** | `.mibc` profile reader (future) | Medium | Not started |
| **17** | Lazy chunk download / JSPI integration (future) | Large | Not started |

### Step 1 — Completed

Project scaffold created and registered in the build system. All projects compile with 0 warnings/errors, 6 unit tests pass.

**Files created:**
```
src/tools/ilsplit/
├── Directory.Build.props
├── src/
│   ├── ILSplit/
│   │   ├── ILSplit.csproj
│   │   ├── Program.cs
│   │   ├── SplitEngine.cs
│   │   ├── DependencyGraph.cs
│   │   ├── ProfileReader.cs
│   │   ├── ClusterStrategy.cs
│   │   ├── AssemblyRewriter.cs
│   │   └── ManifestWriter.cs
│   └── ILSplit.Tasks/
│       ├── ILSplit.Tasks.csproj
│       └── ILSplitTask.cs
└── test/
    └── ILSplit.Tests/
        ├── ILSplit.Tests.csproj
        ├── ProfileReaderTests.cs
        ├── DependencyGraphTests.cs
        ├── ClusterStrategyTests.cs
        └── SplitRoundtripTests.cs
```

**Build system:** `eng/Subsets.props` updated with `Tools.ILSplit` and `Tools.ILSplitTests` subsets.

**What's functional now:**
- CLI with `--input`, `--output`, `--profile`, `--min-cluster-size` options
- Profile reader (text file, comments, blank lines)
- Dependency graph builder (base types, interfaces, fields, method sigs, IL body refs, generics including generic type argument traversal, custom attributes)
- Tarjan's SCC algorithm + cluster merging with minimum size threshold
- Namespace-based fallback clustering when no profile is provided
- Transitive closure over hot type dependencies
- **Assembly splitting**: creates chunk assemblies with correct types, cross-chunk AssemblyRefs, and type reference remapping
- **Three-tier output**: forwarder shell (`MyApp.dll`) with ExportedType entries for ALL types (hot and cold); hot chunk (`MyApp.0.dll`) with hot TypeDefs and no cold AssemblyRefs; cold chunks with TypeDefs and AssemblyRefs forming an oriented DAG toward the hot root
- **Forwarder shell**: original assembly rewritten with ExportedType entries (top-level and nested types), resources preserved in shell, module/assembly custom attributes cleared
- **Type reference remapping**: handles GenericInstanceType, ArrayType, ByReferenceType, PointerType, PinnedType, RequiredModifierType, OptionalModifierType, FunctionPointerType, method/field references, CallSites, exception handler catch types, local variable types
- **Custom attribute remapping**: comprehensive remapping on all providers — types, methods, parameters, fields, properties, events, generic parameters, constraints, module, assembly
- **Nested type handling**: nested types stay with their parent type (can't split across assemblies); keepTypes expanded transitively for nested types
- **New MethodBody creation**: forced resolved write path for Cecil (avoids raw token patching of unresolved bodies referencing removed types)
- **ChunkAssemblyResolver**: resolves chunk assembly references back to original during Cecil Write operations (for constant/enum type resolution)
- JSON manifest generation (source-generated serializer)
- **MSBuild task** (ILSplitTask) — full in-process execution on .NETCoreApp, calls SplitEngine pipeline, returns split/forwarder items with metadata
- **WASM SDK integration** — `ILSplit.targets` hooks between `PrepareInputsForWasmBuild` and `WasmGenerateAppBundle`, replaces `_WasmAssembliesInternal` with split output. Also hooks into Blazor SDK pipeline (`AfterTargets="ILLink"`) for CoreCLR WASM. Opt-in via `WasmEnableILSplit=true`. Integrated into both in-tree (`WasmApp.InTree.targets/props`) and local builds (`WasmApp.LocalBuild.targets/props`)
- **Nested property forwarding**: `WasmEnableILSplit`, `ILSplitProfilePath`, `ILSplitMinClusterSize` forwarded through nested MSBuild publish invocations

**E2E smoke test (Step 12 — Done):**
- Published self-contained HelloWorld console app (`SelfContained=true`, `PublishReadyToRun=false`).
- Overlaid IL-only framework DLLs from `artifacts/bin/runtime/` over R2R copies (Mono.Cecil cannot write R2R assemblies).
- ILSplit processed all DLLs except `System.Private.CoreLib.dll` (excluded — requires VM changes, see Phase 4).
- **Result**: 258 assemblies split into shells + chunks. `corerun.exe ILSplit.E2ETest.0.dll` → `Hello, World!` with exit code 0.
- **CoreLib splitting attempted**: ILSplit successfully produces shell + 6 chunks, but `coreclr_initialize` failed with `0x80070002` in the initial experiment. Root cause: chunk assemblies weren't on the TPA list / in the probing path (deployment issue, not VM limitation). The binder IS initialized before `LoadBaseSystemClasses()`. Next step: fix `CoreLibBinder` Debug assertions and deploy chunks correctly.

**E2E test result (Step 12 — TODO):**
- Build command: `.\dotnet.cmd build -bl /p:TargetOS=browser /p:TargetArchitecture=wasm /p:Configuration=Release /p:RuntimeFlavor=CoreCLR /p:WasmEnableILSplit=true /t:RunSample src/mono/sample/wasm/console-node`

**What's stubbed (TODO for later steps):**
- ManifestWriter overwrites for multiple assemblies (only last assembly's manifest retained)
- Forwarder shells not included in Blazor SDK publish output (need `ResolvedFileToPublish` integration)
- Strong-named assemblies, PDB/debug symbols deferred

---

## Risks and Mitigations

| Risk | Impact | Mitigation |
|---|---|---|
| Complex generic types break during split | Runtime TypeLoadException | Extensive Cecil generics testing; keep generics with their constraints |
| CoreLib chunk probing failure | `0x80070002` during bootstrap if hot chunk not on TPA | Ensure MSBuild integration places chunks alongside CoreLib and includes them in TPA. Verified: binder IS initialized before `LoadBaseSystemClasses`. |
| `CoreLibBinder` Debug assertion fires for chunk types | Debug build crashes on `pMT->GetModule() == GetModule()` | Relax assertion in `binder.cpp` (lines 99, 239) to allow types in chunk modules. |
| Type forwarder resolution overhead | Slight startup regression for hot cluster | Measure; type forwarders are well-optimized in CoreCLR. Hot chunk loaded once during bootstrap, cached for all subsequent lookups. |
| Cecil API limitations for moving types | Can't clone certain metadata | Study ILLink's MarkStep/SweepStep approach — they modify in-place rather than clone |
| Cross-cluster metadata overhead too large | Total size exceeds original by >15% | SCC clustering minimizes cross-refs; minimum cluster size prevents over-fragmentation |
| Profile staleness | Suboptimal hot/cold split | Design for acceptable degradation without profile (namespace fallback) |
| `typeof(X).Assembly` returns chunk assembly | Code comparing `Assembly.GetName().Name` to the original name breaks | **Known issue (deferred).** Document that `typeof(X).Assembly` returns the chunk assembly, not the forwarder shell. |
| Strong-named assemblies | Splitting invalidates the signature | **Known issue (deferred).** v1 produces unsigned output. Re-signing or delay-signing to be addressed later. |
| Debug symbols / PDB | PDBs tied to original MVID; split breaks the link | **Deferred.** v1 does not produce PDBs for split assemblies. |
| VM changes must not break non-split scenarios | All existing tests must pass | Only change is relaxing Debug assertions. If CoreLib has no ExportedTypes (unsplit), the assertion never fires because `pMT->GetModule() == GetModule()` is naturally true. |

---

## Alternative Approaches Considered

### A: Method-level splitting (not type-level)
Split individual methods across assemblies. More granular but .NET metadata model doesn't support this — a type's methods must all be in the same assembly.

### B: ILLink integration (split during trimming)
Integrate directly into ILLink rather than a separate post-ILLink step. Rejected because ILLink's architecture is tightly coupled to its marking/sweeping model, and keeping ILSplit separate allows independent iteration.

### C: Custom binary format instead of PE DLLs
Instead of splitting into PE assemblies, define a custom chunked format. Rejected because it would require runtime changes. Using standard assemblies + type forwarders works with existing runtimes.

### D: Full reference rewrite (no type forwarders)
Delete the original assembly entirely and rewrite all `AssemblyRef`/`TypeRef` in every other assembly. More complex, slightly smaller total output, but fragile — any assembly missed during rewriting breaks at load time. Type forwarders are safer and proven.
