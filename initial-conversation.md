Is there a tool that can split IL assemblies into smaller ones ? It could be after ILLink trimming of publishing the app. 


---

Great idea! Let's design this tool from scratch. Here's a comprehensive architecture:

---

## 🏗️ Tool Design: **ILSplit** — IL Assembly Splitter for On-Demand Class Loading

---

### 🎯 Goals
- **Input:** One or more (possibly ILLink-trimmed) .NET assemblies
- **Output:** Many smaller assemblies, each containing a logical group of types
- **Runtime:** A loader that resolves and loads only the assemblies needed for the current execution path

---

### 🧱 High-Level Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                        ILSplit Tool                         │
│                                                             │
│  ┌──────────────┐   ┌───────────────┐   ┌───────────────┐  │
│  │  IL Parser   │──▶│  Dependency   │──▶│   Splitter    │  │
│  │ (Mono.Cecil) │   │  Graph Builder│   │   Engine      │  │
│  └──────────────┘   └───────────────┘   └───────┬───────┘  │
│                                                  │          │
│                             ┌────────────────────┘          │
│                             ▼                               │
│                   ┌──────────────────┐                      │
│                   │  Assembly Writer  │                      │
│                   │  + Manifest Gen  │                      │
│                   └──────────────────┘                      │
└─────────────────────────────────────────────────────────────┘
                             │
                             ▼
┌─────────────────────────────────────────────────────────────┐
│              Runtime: ILSplit Loader                        │
│  AssemblyLoadContext + generated manifest + lazy resolver   │
└─────────────────────────────────────────────────────────────┘
```

---

### 📦 Phase 1 — IL Parsing & Type Dependency Graph

Use **Mono.Cecil** to read the input assembly and build a directed type dependency graph.

```csharp name=DependencyGraphBuilder.cs
using Mono.Cecil;
using System.Collections.Generic;

public class TypeDependencyGraph
{
    // type full name → set of types it directly depends on
    public Dictionary<string, HashSet<string>> Edges { get; } = new();

    public void AddDependency(string fromType, string toType)
    {
        if (!Edges.TryGetValue(fromType, out var deps))
            Edges[fromType] = deps = new HashSet<string>();
        deps.Add(toType);
    }
}

public class DependencyGraphBuilder
{
    public TypeDependencyGraph Build(AssemblyDefinition assembly)
    {
        var graph = new TypeDependencyGraph();

        foreach (var module in assembly.Modules)
        foreach (var type in module.Types)
            VisitType(type, graph);

        return graph;
    }

    private void VisitType(TypeDefinition type, TypeDependencyGraph graph)
    {
        // Base type
        if (type.BaseType != null)
            graph.AddDependency(type.FullName, type.BaseType.FullName);

        // Interfaces
        foreach (var iface in type.Interfaces)
            graph.AddDependency(type.FullName, iface.InterfaceType.FullName);

        // Fields
        foreach (var field in type.Fields)
            graph.AddDependency(type.FullName, field.FieldType.FullName);

        // Methods: parameters, return type, locals, IL operands
        foreach (var method in type.Methods)
        {
            graph.AddDependency(type.FullName, method.ReturnType.FullName);
            foreach (var param in method.Parameters)
                graph.AddDependency(type.FullName, param.ParameterType.FullName);

            if (!method.HasBody) continue;
            foreach (var local in method.Body.Variables)
                graph.AddDependency(type.FullName, local.VariableType.FullName);
            foreach (var instr in method.Body.Instructions)
                if (instr.Operand is TypeReference tr)
                    graph.AddDependency(type.FullName, tr.FullName);
                else if (instr.Operand is MethodReference mr)
                    graph.AddDependency(type.FullName, mr.DeclaringType.FullName);
                else if (instr.Operand is FieldReference fr)
                    graph.AddDependency(type.FullName, fr.DeclaringType.FullName);
        }
    }
}
```

---

### ✂️ Phase 2 — Splitting Strategy

Types need to be grouped into **partitions**. Multiple strategies are supported:

```csharp name=SplitStrategy.cs
public interface ISplitStrategy
{
    /// <summary>
    /// Returns a partition key for the given type.
    /// Types with the same key end up in the same output assembly.
    /// </summary>
    string GetPartitionKey(TypeDefinition type, TypeDependencyGraph graph);
}

/// <summary>One assembly per namespace.</summary>
public class ByNamespaceSplitStrategy : ISplitStrategy
{
    public string GetPartitionKey(TypeDefinition type, TypeDependencyGraph graph)
        => string.IsNullOrEmpty(type.Namespace) ? "__global__" : type.Namespace;
}

/// <summary>One assembly per top-level namespace segment (e.g. MyApp.Data.* → MyApp.Data).</summary>
public class ByRootNamespaceSplitStrategy : ISplitStrategy
{
    private readonly int _depth;
    public ByRootNamespaceSplitStrategy(int depth = 2) => _depth = depth;

    public string GetPartitionKey(TypeDefinition type, TypeDependencyGraph graph)
    {
        var parts = type.Namespace?.Split('.') ?? [];
        return string.Join('.', parts.Take(_depth));
    }
}

/// <summary>
/// Graph clustering: strongly-connected components stay together.
/// Useful to avoid circular cross-assembly references.
/// </summary>
public class SCCBasedSplitStrategy : ISplitStrategy
{
    private readonly Dictionary<string, string> _typeToComponent;

    public SCCBasedSplitStrategy(TypeDependencyGraph graph)
        => _typeToComponent = TarjanSCC.Compute(graph);

    public string GetPartitionKey(TypeDefinition type, TypeDependencyGraph graph)
        => _typeToComponent.TryGetValue(type.FullName, out var comp) ? comp : type.FullName;
}
```

> **Key insight:** Types in a **Strongly Connected Component** (SCC) of the dependency graph *must* stay together — splitting them would create circular assembly references, which .NET doesn't support.

---

### ⚙️ Phase 3 — Assembly Writer

```csharp name=AssemblySplitter.cs
using Mono.Cecil;
using System.Collections.Generic;
using System.Linq;

public class AssemblySplitter
{
    private readonly ISplitStrategy _strategy;

    public AssemblySplitter(ISplitStrategy strategy) => _strategy = strategy;

    public SplitResult Split(AssemblyDefinition source)
    {
        var graph = new DependencyGraphBuilder().Build(source);

        // 1. Assign every type to a partition
        var partitions = new Dictionary<string, List<TypeDefinition>>();
        foreach (var module in source.Modules)
        foreach (var type in module.Types.Where(t => t.IsPublic || t.IsNestedPublic))
        {
            var key = _strategy.GetPartitionKey(type, graph);
            if (!partitions.TryGetValue(key, out var bucket))
                partitions[key] = bucket = new List<TypeDefinition>();
            bucket.Add(type);
        }

        // 2. Create one output assembly per partition
        var outputs = new List<AssemblyDefinition>();
        var typeToAssembly = new Dictionary<string, string>(); // fullName → output assembly name

        foreach (var (key, types) in partitions)
        {
            var asmName = SanitizeAssemblyName(source.Name.Name, key);
            var outAsm = AssemblyDefinition.CreateAssembly(
                new AssemblyNameDefinition(asmName, source.Name.Version),
                asmName, ModuleKind.Dll);

            foreach (var type in types)
            {
                // Deep-clone the type into the new module (handles IL, attributes, generics)
                var cloned = TypeCloner.Clone(type, outAsm.MainModule);
                outAsm.MainModule.Types.Add(cloned);
                typeToAssembly[type.FullName] = asmName;
            }

            outputs.Add(outAsm);
        }

        // 3. Fix up cross-assembly references
        ReferenceFixup.Fix(outputs, typeToAssembly);

        // 4. Generate the split manifest
        var manifest = new SplitManifest(typeToAssembly);

        return new SplitResult(outputs, manifest);
    }

    private static string SanitizeAssemblyName(string baseName, string key)
        => $"{baseName}.{key.Replace('<', '_').Replace('>', '_')}";
}
```

---

### 🗺️ Phase 4 — Split Manifest

A JSON manifest is generated alongside the split assemblies. The runtime loader uses it to know *which assembly to load for a requested type*, without scanning all files.

```json name=ilsplit.manifest.json
{
  "originalAssembly": "MyApp",
  "version": "1.0.0",
  "typeMap": {
    "MyApp.Services.UserService":       "MyApp.Services.dll",
    "MyApp.Services.EmailService":      "MyApp.Services.dll",
    "MyApp.Data.UserRepository":        "MyApp.Data.dll",
    "MyApp.Data.OrderRepository":       "MyApp.Data.dll",
    "MyApp.Models.User":                "MyApp.Models.dll",
    "MyApp.Models.Order":               "MyApp.Models.dll",
    "MyApp.Utils.StringHelper":         "MyApp.Utils.dll"
  },
  "assemblies": {
    "MyApp.Services.dll": ["MyApp.Data.dll", "MyApp.Models.dll"],
    "MyApp.Data.dll":     ["MyApp.Models.dll"],
    "MyApp.Models.dll":   [],
    "MyApp.Utils.dll":    []
  }
}
```

---

### 🚀 Phase 5 — Runtime Lazy Loader

```csharp name=ILSplitLoader.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;

public class ILSplitLoader
{
    private readonly string _assemblyDir;
    private readonly SplitManifest _manifest;
    private readonly Dictionary<string, Assembly> _loaded = new();
    private readonly AssemblyLoadContext _context;

    public ILSplitLoader(string manifestPath)
    {
        _assemblyDir = Path.GetDirectoryName(manifestPath)!;
        _manifest = JsonSerializer.Deserialize<SplitManifest>(
            File.ReadAllText(manifestPath))!;
        _context = new AssemblyLoadContext("ILSplitContext", isCollectible: true);

        // Hook into the runtime type resolver
        AppDomain.CurrentDomain.TypeResolve += OnTypeResolve;
    }

    /// <summary>
    /// Eagerly loads the assembly for a given type name (if not yet loaded).
    /// </summary>
    public Assembly LoadForType(string typeFullName)
    {
        if (!_manifest.TypeMap.TryGetValue(typeFullName, out var asmFile))
            throw new TypeLoadException($"Type '{typeFullName}' not found in split manifest.");

        return LoadAssembly(asmFile);
    }

    private Assembly LoadAssembly(string asmFile)
    {
        if (_loaded.TryGetValue(asmFile, out var cached)) return cached;

        // Load dependencies first (topological order via manifest)
        foreach (var dep in _manifest.Assemblies[asmFile])
            LoadAssembly(dep);

        var path = Path.Combine(_assemblyDir, asmFile);
        var asm = _context.LoadFromAssemblyPath(path);
        _loaded[asmFile] = asm;
        return asm;
    }

    private Assembly? OnTypeResolve(object? sender, ResolveEventArgs args)
    {
        // Intercept CLR type resolution failures and load the right split assembly
        if (_manifest.TypeMap.TryGetValue(args.Name, out var asmFile))
            return LoadAssembly(asmFile);
        return null;
    }
}
```

---

### 🔄 Full Pipeline

```
dotnet publish -c Release
       │
       ▼
   ILLink trim          ← removes dead code
       │
       ▼
   ilsplit              ← splits assemblies + generates manifest
       │
       ├── MyApp.Services.dll
       ├── MyApp.Data.dll
       ├── MyApp.Models.dll
       ├── MyApp.Utils.dll
       └── ilsplit.manifest.json
       │
       ▼
   App starts → ILSplitLoader → loads only needed .dlls on demand
```

---

### ⚠️ Key Challenges to Solve

| Challenge | Mitigation |
|---|---|
| **Circular type dependencies** | Use SCC algorithm; keep cycles in same assembly |
| **Generics / open types** | Clone generic parameters carefully with Mono.Cecil |
| **Reflection-based type loading** | Preserve manifest entries; hook `TypeResolve` event |
| **Nested types** | Keep nested types with their declaring type |
| **Static initializers / module initializers** | Analyze and preserve ordering |
| **InternalsVisibleTo** | Re-emit friendship attributes in outputs |
| **Resources / embedded data** | Assign to the assembly that uses them |

---

Great question! Let's think through this carefully from multiple angles.

---

## 📏 Size Overhead Analysis

### 1. 📦 Per-Assembly Metadata Overhead

Every .NET assembly — no matter how small — carries a fixed metadata overhead:

| Metadata Section | Typical Size |
|---|---|
| PE/COFF headers | ~512 bytes |
| CLR header (`_IMAGE_COR20_HEADER`) | ~72 bytes |
| Assembly identity (name, version, culture, public key token) | ~100–300 bytes |
| Module table | ~20 bytes |
| TypeRef table (references to external types) | **~6 bytes × N referenced types** |
| AssemblyRef table (references to other assemblies) | **~12 bytes × N referenced assemblies** |
| Minimal `#Strings`, `#GUID`, `#Blob` heap baseline | ~200–500 bytes |
| **Total fixed overhead per assembly** | **~1–4 KB** |

So if you split 1 assembly into **100 smaller assemblies**, you're looking at roughly **100–400 KB of pure metadata overhead**, even if the actual type content is unchanged.

---

### 2. 🔁 Cross-Reference Duplication

When a type from partition A is referenced in partition B, the **TypeRef + AssemblyRef rows** must be emitted in partition B's metadata tables:

```
Original (1 assembly):
  TypeRef "MyApp.Models.User" → 6 bytes (internal ref, may be optimized away)

Split (N assemblies):
  Every assembly that uses User must emit:
    - AssemblyRef to MyApp.Models.dll  → ~40 bytes (first time per assembly)
    - TypeRef for MyApp.Models.User    → ~12 bytes
```

For a highly connected type graph, this can multiply quickly. A "hub" type (e.g., a `User` model referenced everywhere) will cause an AssemblyRef entry in **every** split assembly that uses it.

---

### 3. 📊 Rough Estimate for a Real App

Let's model a mid-size app: **500 types**, split into **50 assemblies** (~10 types each):

| Factor | Estimate |
|---|---|
| Fixed overhead per assembly (×50) | ~150 KB |
| Average TypeRef duplication (×500 cross-refs) | ~50 KB |
| AssemblyRef entries (~5 deps per assembly × 50) | ~10 KB |
| String heap growth (type/method name repetition) | ~30 KB |
| **Total overhead** | **~240 KB** |

Original single trimmed assembly size for a mid-size app: typically **1–5 MB**.
So overhead is roughly **5–15% of the trimmed assembly size**.

---

### 4. 💾 Disk vs. Memory Overhead

These are actually **different problems**:

| Dimension | Overhead | Notes |
|---|---|---|
| **Disk size** | +5–15% | Metadata duplication across split assemblies |
| **Memory at startup** | ✅ **Reduced** | Only loaded assemblies are mapped |
| **Memory at full load** | ~same + overhead | All assemblies loaded = original + metadata cost |
| **JIT memory** | ✅ **Reduced per run** | Only methods in loaded assemblies are JIT-compiled |
| **GC pressure** | ✅ **Reduced** | Fewer types registered with the GC at startup |

> 💡 The key insight: **disk overhead goes slightly up, but runtime memory usage goes down** — which is the actual goal.

---

### 5. 🧮 Overhead vs. Granularity Tradeoff

The finer the split, the higher the overhead:

```
Granularity          | Assemblies | Disk overhead | Load-time savings
─────────────────────┼────────────┼───────────────┼──────────────────
1 type  / assembly   |    500     |   High (~MB)  |   Maximum
10 types / assembly  |     50     |   Medium      |   High
50 types / assembly  |     10     |   Low (~KB)   |   Moderate
1 namespace/assembly |    5–20    |   Minimal     |   Low–Moderate
```

---

### 6. ✅ Mitigation Strategies

1. **Minimum size threshold** — don't split partitions smaller than e.g. 10 KB; keep tiny type groups together
2. **Hub type detection** — types referenced by >50% of partitions stay in a `MyApp.Core.dll` shared assembly (like a "shared kernel"), avoiding repeated AssemblyRef entries
3. **Compress manifests** — the `ilsplit.manifest.json` itself is tiny (~10 KB) and can be brotli-compressed for network delivery
4. **ReadyToRun per split assembly** — each small assembly can have its own R2R pre-compiled code, improving first-load speed further

---

### 🏁 Bottom Line

| Scenario | Disk overhead | Memory benefit |
|---|---|---|
| Coarse split (by top namespace, ~10 assemblies) | **< 1%** | Moderate |
| Medium split (by namespace, ~50 assemblies) | **~5–10%** | Good |
| Fine split (per class, ~500 assemblies) | **~15–30%** | Maximum, but diminishing returns |

The **sweet spot** is a **namespace-level or SCC-cluster split** (~10–50 assemblies), where disk overhead is negligible and runtime memory savings are meaningful. The per-class granularity is only worth it for **very large, cold-path-heavy** applications (e.g. CLI tools, plugin hosts, MAUI apps on mobile).

---

Would you like to model this against a specific app size, or explore how the **hub type extraction** strategy would work in the splitter design?

---

### 🛠️ Recommended Tech Stack

| Component | Technology |
|---|---|
| IL reading/writing | **Mono.Cecil** |
| SCC graph algorithm | **Tarjan's algorithm** (pure C#) |
| Manifest format | **System.Text.Json** |
| Build integration | **MSBuild custom task** |
| Runtime loading | **AssemblyLoadContext** |
| CLI tool | **System.CommandLine** |

---

Would you like to dive deeper into any specific phase — e.g., the **type cloner**, the **SCC-based partitioning**, or the **MSBuild integration**?

---


Excellent question — the two goals (SCC splitting and IL trimming) have significant overlap but are not identical. Let me analyse each of the 5 proposals through the trimming lens.

---

## 🔍 How IL Trimming Works (Quick Recap)

ILLink trims by **reachability analysis** from roots. A type or method is kept if it's reachable from:
- Entry point (`Main`)
- Explicitly preserved roots (`[DynamicDependency]`, descriptors)
- Transitively from any kept type's fields, method signatures, base types, interfaces

The key insight for our analysis:

> **A static field of type `T` forces `T` to be rooted**, even if the field is never read at runtime. An **injected delegate `Func<...>?`** (nullable, set lazily) has **no such forcing effect** — ILLink can trim the entire target type if nothing else references it.

---

## 📊 Analysis of Each Proposal

---

### #1 — `Exception` ↔ `StackTrace`/`MethodBase` — Lazy Delegate
### #3 — `Exception` ↔ `Reflection` (`Source`, `GetClassName`) — Lazy Delegate

These two are best analysed together since they share the same pattern and the same trimming benefit.

**Current state — what trimming *cannot* remove today:**

```
Exception (always rooted — everything throws)
  └─► new StackTrace(this)         ← StackTrace always kept
        └─► StackFrame             ← always kept
              └─► MethodBase       ← always kept
                    └─► MethodInfo, ConstructorInfo, ParameterInfo...
                          └─► entire Reflection emission graph
  └─► GetType().ToString()         ← RuntimeType always kept
  └─► TargetSite → MethodBase      ← see above
  └─► Source → Module.Assembly     ← Assembly, Module always kept
```

Because `Exception` is always reachable (every `catch` block roots it), **the entire diagnostics and reflection stack is currently unkillable by ILLink** — even in deeply trimmed apps that never use reflection.

**After the lazy delegate fix:**

```csharp name=Exception.Trimming.cs
// ILLink sees:
internal static Func<Exception, bool, string?>? s_getStackTraceString; // = null unless registered
internal static Func<Exception, string>?        s_getClassName;        // = null unless registered

// StackTrace, MethodBase, RuntimeType are now only reachable if:
// (a) something explicitly calls new StackTrace(), OR
// (b) the diagnostics assembly registers the delegate at startup
// In a trimmed app with [PublishTrimmed] + no diagnostics, (b) never happens
// → ILLink can now remove StackTrace, StackFrame, MethodBase entirely ✅
```

**Trimming gain:**
| Component | Size (approx.) | Trimmable after fix? |
|---|---|---|
| `StackTrace` + `StackFrame` | ~40 KB | ✅ Yes |
| `MethodBase`, `MethodInfo`, `ConstructorInfo` | ~120 KB | ✅ Yes (if no other reflection use) |
| `RuntimeType` internals | ~200 KB | ✅ Partially |
| `Module`, `Assembly` (diagnostic paths) | ~60 KB | ✅ Yes |

**Verdict: 🏆 Highest trimming impact of all 5 proposals.** These two changes together could save **300–400 KB** in trimmed apps that don't use diagnostics/reflection.

---

### #2 — `String` ↔ `CultureInfo`/`CompareInfo` — Injected Interface

**Current state:**

```
String (always rooted — it's used everywhere)
  └─► CultureInfo.CurrentCulture   ← CultureInfo always kept
        └─► CultureData            ← always kept (~200 types worth of data)
              └─► Calendar types   ← all 14 calendar implementations kept
              └─► RegionInfo       ← always kept
        └─► CompareInfo            ← always kept
              └─► SortVersion      ← always kept
  └─► String.Compare(CultureInfo)  ← forces CultureInfo parameter type kept
```

For a **trimmed app** that:
- Never calls `String.Compare(str, str, CultureInfo)` explicitly
- Never formats dates with culture info
- Uses only ordinal comparisons

...`CultureInfo` and its entire data tree (~500 KB) is **still kept today** because `String` has static references to it.

**After the `IStringComparer` injection:**

```csharp name=String.Trimming.cs
// ILLink sees in String.Comparison.cs:
internal static IStringComparer? s_currentCultureComparer; // null unless Globalization loaded

// The CurrentCulture path in String.Compare is now:
private static int CompareWithCurrentCulture(string a, string b, bool ignoreCase) =>
    (s_currentCultureComparer ?? s_invariantComparer).Compare(a.AsSpan(), b.AsSpan(), ignoreCase);

// ILLink analysis:
// - s_currentCultureComparer is nullable → no forced root on CultureInfo
// - s_invariantComparer is OrdinalComparer (tiny, no globalization) → always kept
// - CultureInfo is only kept if app actually uses it
// → In a trimmed console app: CultureInfo, CultureData, all Calendar types → trimmed ✅
```

**BUT** — there's an important nuance. The `String.Compare(string, string, CultureInfo)` overload takes `CultureInfo` as a **parameter type**. ILLink currently keeps any type that appears in a kept method's signature. So you'd also need to:

```csharp name=String.Comparison.Trimming.cs
// These overloads that take CultureInfo directly must be annotated:
[RequiresUnreferencedCode("Culture-aware comparison requires globalization data")]
// OR moved to a separate extension class in the Globalization assembly:
public static class StringGlobalizationExtensions
{
    public static int Compare(string? a, string? b, CultureInfo culture, CompareOptions opts)
        => (culture ?? CultureInfo.CurrentCulture).CompareInfo.Compare(a, b, opts);
}
// → Overloads that take CultureInfo parameter are no longer in String → ILLink can cut CultureInfo
```

**Trimming gain:**
| Component | Size (approx.) | Trimmable after fix? |
|---|---|---|
| `CultureInfo` core + `CultureData` | ~150 KB | ✅ Yes (for ordinal-only apps) |
| Calendar implementations (×14) | ~300 KB | ✅ Yes |
| `CompareInfo` NLS/ICU bindings | ~80 KB | ✅ Yes |
| `RegionInfo` | ~20 KB | ✅ Yes |

**Verdict: 🥈 Second highest trimming impact.** Especially valuable for **MAUI, Blazor WASM, and console tools** that use only ordinal string operations. This aligns directly with what `[InvariantGlobalization]` already tries to do — but currently requires a build-time switch; this change would make it automatic via trimming.

---

### #4 — `CultureInfo` ↔ `Thread`/`AsyncLocal` — `ICultureContext` Interface

**Current state:**

```
AsyncLocal<CultureInfo>              ← forces CultureInfo kept whenever AsyncLocal<T> is kept
ExecutionContext                     ← always kept (async/await uses it)
  └─► stores CultureInfo reference  ← CultureInfo always kept in any async app
Thread.CurrentCulture                ← CultureInfo always kept
```

`ExecutionContext` is kept in **every async app** because `await` uses it. And currently `ExecutionContext` stores a direct `CultureInfo` reference — so any async app can never trim `CultureInfo`.

**After the `ICultureContext` interface:**

```csharp name=ExecutionContext.Trimming.cs
// BEFORE: ExecutionContext has a field of type CultureInfo → always kept
internal CultureInfo? _culture;

// AFTER: ExecutionContext has a field of type ICultureContext → interface kept, CultureInfo optional
internal ICultureContext? _culture;

// ILLink analysis:
// - ICultureContext is in CoreLib.Primitives → always kept (it's tiny)
// - CultureInfo (which implements ICultureContext) is only kept if:
//   (a) app calls CultureInfo.CurrentCulture directly, OR
//   (b) app uses culture-aware APIs
// → Pure async apps with ordinal string ops can now trim CultureInfo ✅
```

**However**, this is the **most constrained** proposal from a trimming perspective, because:

1. `ExecutionContext` is a CLR-special type — changes here need runtime buy-in
2. The benefit overlaps heavily with #2 — if #2 is done, most CultureInfo trimming is already achieved
3. The `Thread` ↔ `CultureInfo` binding is partly in the native runtime, not just managed code

**Trimming gain:** Significant for **async apps** specifically, but largely **subsumed by #2** for non-async apps.

**Verdict: 🥉 Good, but incremental on top of #2. Most valuable for async-heavy workloads (ASP.NET Core, Blazor).**

---

### #5 — `String` ↔ `Encoding`/`UTF8Encoding` — `IUtf8Decoder` Interface

**Current state:**

```
String (always rooted)
  └─► Encoding.UTF8 (static field)    ← UTF8Encoding always kept
        └─► EncoderFallback           ← always kept
              └─► EncoderReplacementFallback ← always kept
        └─► DecoderFallback           ← always kept
  └─► Encoding.GetString()           ← Encoding class always kept
        └─► EncoderFallbackBuffer     ← always kept
        └─► all Encoding subclasses (via reflection in Encoding.GetEncoding)
```

In the current runtime, `Encoding.UTF8` is a **static readonly field** on `Encoding`. This means ILLink sees `Encoding` as always-referenced from `String`, and transitively keeps the entire text encoding stack.

**After the `IUtf8Decoder` injection:**

```csharp name=String.Encoding.Trimming.cs
// BEFORE: static call to Encoding.UTF8 in CreateStringForSByteConstructor
// → Encoding class always kept

// AFTER: injected nullable delegate
internal static IUtf8Decoder? s_utf8Decoder; // null unless Text assembly loads

// ILLink analysis for a trimmed app that never uses String(sbyte*) constructor:
// - s_utf8Decoder is nullable and never assigned → ILLink can see it's never set
// - Encoding, UTF8Encoding, EncoderFallback → potentially trimmable ✅

// BUT: String.Ctor(sbyte*, int, int, Encoding enc) still takes Encoding as a parameter
// → Encoding type is kept if this overload is kept
// This overload is rarely used in modern .NET code → ILLink's linker step can trim it
// if annotated with [RequiresUnreferencedCode] or placed behind a feature switch
```

**The real trimming gain** is more nuanced here. The `String(sbyte*)` constructors are:
- **`[CLSCompliant(false)]`** — rarely used
- Almost never called in modern C# code (pointer arithmetic is unsafe)
- Already trimmed away by ILLink in most apps

So the **current practical trimming gain is modest** — but it unblocks a more important follow-up:

```csharp name=Encoding.TrimAnnotation.cs
// Once String no longer statically references Encoding,
// the entire Encoding.GetEncoding(string name) codepage lookup table
// (~150 KB of codepage data) becomes trimmable for apps that use only UTF-8/Unicode:
[RequiresUnreferencedCode("Encoding.GetEncoding by name requires all encodings to be present")]
public static Encoding GetEncoding(string name) { ... }

// Feature switch to trim all non-UTF8 encodings:
// <RuntimeHostConfigurationOption Include="System.Text.Encoding.EnableUnsafeUTF7Encoding" Value="false" Trim="true" />
```

**Trimming gain:**
| Component | Size (approx.) | Trimmable after fix? |
|---|---|---|
| `EncoderFallback`/`DecoderFallback` hierarchy | ~30 KB | ✅ Yes (if sbyte* ctor trimmed) |
| Codepage lookup tables | ~150 KB | ✅ Yes (via feature switch, enabled by this) |
| Non-UTF8 `Encoding` subclasses | ~80 KB | ✅ Yes (already mostly trimmed, this helps more) |

**Verdict: 🔹 Modest direct gain, but valuable as an enabler** for further encoding trimming downstream.

---

## 🏆 Final Ranking: Trimming Impact

```
┌────┬──────────────────────────────────────────┬──────────────┬────────────────────────────┐
│ #  │ Change                                   │ Trimming     │ Primary Beneficiary        │
│    │                                          │ Saving       │                            │
├────┼──────────────────────────────────────────┼──────────────┼────────────────────────────┤
│ 1  │ Exception → StackTrace/MethodBase        │ ~300–400 KB  │ ALL trimmed apps           │
│    │ (lazy delegate)                          │ 🏆🏆🏆        │ Console, MAUI, NativeAOT   │
├────┼──────────────────────────────────────────┼──────────────┼────────────────────────────┤
│ 3  │ Exception → Reflection (Source,          │ (included    │ ALL trimmed apps           │
│    │ GetClassName) (lazy delegate)            │ above)       │ same as #1                 │
├────┼──────────────────────────────────────────┼──────────────┼────────────────────────────┤
│ 2  │ String → CultureInfo/CompareInfo         │ ~500–550 KB  │ Ordinal-string apps,       │
│    │ (IStringComparer interface)              │ 🏆🏆🏆        │ MAUI, Blazor WASM, CLI     │
├────┼──────────────────────────────────────────┼──────────────┼────────────────────────────┤
│ 4  │ CultureInfo ↔ AsyncLocal/Thread          │ ~100–150 KB  │ Async apps specifically    │
│    │ (ICultureContext interface)              │ 🏆🏆          │ incremental on top of #2   │
├────┼──────────────────────────────────────────┼──────────────┼────────────────────────────┤
│ 5  │ String → Encoding/UTF8                   │ ~50–80 KB    │ Enabler for further        │
│    │ (IUtf8Decoder interface)                 │ 🏆            │ encoding trimming          │
└────┴──────────────────────────────────────────┴──────────────┴────────────────────────────┘
```

---

## 💡 Key Insight

> Proposals **#1/#3 and #2** are orthogonal and **both should be done** — they attack completely different parts of the forced-root tree:
> - **#1/#3** frees the **Diagnostics + Reflection** cluster (~400 KB)
> - **#2** frees the **Globalization** cluster (~550 KB)
>
> Together they could reduce a typical trimmed .NET app by **~1 MB** — on top of what ILLink already trims today — simply by converting hard static references to lazy injected delegates/interfaces.

This is also exactly the direction the .NET team has been moving with **feature switches** (`RuntimeHostConfigurationOption` with `Trim="true"`) — these proposals are the deeper, structural version of that same idea.