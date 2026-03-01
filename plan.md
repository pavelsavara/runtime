# System.Private.CoreLib SCC Research Plan

## Context

**Goal:** Break the main SCC in System.Private.CoreLib for browser/WASM targets into sub-clusters, characterize each, map inter-cluster dependencies, and identify safe dependency-cut candidates to reduce size as much as possible. The browser sample has a 1,090-method CoreLib SCC (178.4 KB, 31.1% of total IL). The Blazor app has no single dominant CoreLib SCC; its cost is driven by external assemblies (Linq.Expressions, Text.Json, Components). The largest Blazor SCC is 99 methods in Linq.Expressions.Compiler (316.2 KB). Zero overlap between browser and Blazor SCC cores.

**Target:** IL-trimmed `System.Private.CoreLib.dll` for browser/WASM (CoreCLR build). The sample app folder name contains "mono" but the actual build is CoreCLR.

**Scope:** Managed C# code only. Native code, JS interop glue, and emscripten dependencies are out of scope.

**Assumptions:**
- Non-invariant globalization (invariant-mode cuts are already well-known)
- Reflection and Reflection.Emit must remain functional (Blazor depends on them)
- Satellite methods outside the core SCC are out of scope — how to cut those is already known
- Focus is on identifying **what** to cut safely; implementation details (ILLink directives, feature switches, `#if` guards) come after

**Source directories:**
- `src/libraries/System.Private.CoreLib/src/`
- `src/coreclr/System.Private.CoreLib/src/`

**Key metrics from method-cost tool:**

| Metric | Browser Sample | Blazor WASM |
|--------|---------------|-------------|
| Assemblies | 4 | 40 |
| Total methods | 10,821 | 25,373 |
| Total IL size | 587,152 bytes (573.4 KB) | 1,503,162 bytes (1,467.9 KB) |
| CoreLib IL size | 576,116 bytes (562.6 KB) | 766,356 bytes (748.4 KB) |
| Max transitive size | 261,988 bytes (255.8 KB, 44.6%) | 460,727 bytes (449.9 KB, 30.7%) |
| Largest super-SCC | 1,090 methods | 1,046 methods |
| CoreLib max transitive | 261,988 bytes (255.8 KB) | 332,347 bytes (324.6 KB) |
| Tarjan direct SCCs (multi) | 8 | 31 |
| Tarjan super-SCCs (multi) | 15 | 81 |

## Browser SCC Core Namespace Distribution (1,090 methods, 182,691 bytes = 178.4 KB)

| Methods | Namespace | Notes |
|---------|-----------|-------|
| 375 | System | Primitives, Number, String, Array, DateTime, Enum, Convert, etc. |
| 163 | System.Globalization | CultureData, CompareInfo, DateTimeFormatInfo, Calendars, TextInfo |
| 121 | System.Text | StringBuilder, Encoding, UTF8, Unicode utilities |
| 103 | System.Runtime.Intrinsics | Scalar\`1, Vector128/256/512 helpers |
| 87 | System.Reflection | RuntimeType, CustomAttribute, MethodBase, FieldInfo, etc. |
| 68 | System.Reflection.Emit | TypeBuilder, ILGenerator, ModuleBuilder, SignatureHelper |
| 29 | System.Collections.Generic | Dictionary, List, HashSet, Comparer, EqualityComparer |
| 28 | System.Buffers | ArrayPool, SearchValues, IndexOfAnyAsciiSearcher |
| 27 | System.Threading | Thread, ThreadPool, Lock, Monitor, Timer |
| 23 | System.Reflection.Metadata | MetadataReader (external assembly) |
| 15 | System.Runtime.CompilerServices | RuntimeHelpers, CastHelpers |
| 14 | System.Numerics | Vector\`1, BitOperations |
| 14 | System.Runtime.InteropServices | Marshal, GCHandle, SafeHandle |
| 9 | System.Text.Unicode | Utf8Utility, Utf16Utility |
| 4 | System.Runtime.InteropServices.Marshalling | SafeHandleMarshaller, Utf8StringMarshaller |
| 3 | System.Runtime.Loader | AssemblyLoadContext |
| 2 | System.Collections | Non-generic collections |
| 1 | System.Buffers.Text | Utf8Formatter |
| 1 | Microsoft.Win32.SafeHandles | SafeFileHandle |

---

## Phase 0: Method-Cost Analysis

### 0A. Browser sample app [DONE]
- [x] Run method-cost tool on `d:\runtime2\src\mono\sample\wasm\browser\bin\publish\wwwroot\_framework`
- [x] Report saved to `d:\runtime2\method-cost-full.json` (n=5000)
- [x] 4 assemblies, 10,821 methods, 573.4 KB total IL
- [x] Largest super-SCC: 1,090 methods (all with transitiveMethodCount=3234, identical transitiveSize=182,691 bytes = 178.4 KB, 31.1%)
- [x] Max transitive size: 261,988 bytes (255.8 KB, 44.6%) — DateOnly feeds into the SCC + DateTime/IO/async chain
- [x] Top types by transitive: DateOnly (255.8 KB), TimeOnly (255.5 KB), DateTime (254.5 KB), Stream (249.8 KB), JavaScriptExports (248.8 KB)
- [x] Mapped namespace distribution (see table above)

### 0B. Blazor WASM app [DONE]
- [x] Run method-cost tool on `d:\samples\blazorwasmruntime\bin\Release\net11.0\publish\wwwroot\_framework\`
- [x] Report saved to `d:\runtime2\method-cost-full-blazor.json` (n=5000)
- [x] 40 assemblies, 25,373 methods, 1,467.9 KB total IL
- [x] Largest super-SCC: 1,046 methods, max transitive 460,727 bytes (449.9 KB, 30.7%)

**Key findings:**

1. **No single dominant CoreLib SCC in Blazor.** The browser sample has a clean 1,090-method CoreLib SCC.
   In Blazor, the CoreLib methods are fragmented across many smaller groups (largest: 55 methods at Reflection).

2. **Blazor cost is driven by external assemblies:**
   - Linq.Expressions: 170.2 KB own, max transitive 432.9 KB
   - Text.Json: 161.8 KB own, max transitive 432.8 KB
   - Components: 91.0 KB own, max transitive 449.9 KB
   - Net.Http: 55.6 KB own, max transitive 399.9 KB

3. **Blazor true SCCs (identical transitiveSize = definite mutual recursion):**
   - 61 methods in Linq.Expressions.Interpreter (354.0 KB)
   - 99 methods in Linq.Expressions.Compiler (316.2 KB)
   - 97 methods in Linq.Expressions core (304.1 KB)
   - 42 methods in Linq.Expressions.Compiler (283.7 KB)
   - 33 methods in Text.Json.Nodes (284.5 KB)
   - 22 methods in Threading.Tasks (189.2 KB)
   - 54 methods in Reflection (231.0 KB)
   - 29 methods in IO/Net.Http (263.2 KB)
   - 14 methods in Text.Json.Serialization.Converters (332.2 KB)
   - 11 methods in Collections.Frozen (180.1 KB)

4. **Zero overlap** between browser SCC core (1,082 unique names) and Blazor SCC core (100 names).
   Browser SCC = CoreLib primitives/globalization/text/reflection.
   Blazor SCC = Linq.Expressions.Compiler (99) + 1 Components method.

5. **Assembly cost ranking in Blazor (by max transitive):**
   Components 449.9 KB > Linq.Expressions 432.9 KB > Text.Json 432.8 KB > Net.Http 399.9 KB > CoreLib 324.6 KB

6. **CoreLib is smaller in Blazor's transitive ranking** (324.6 KB max) vs browser sample (255.8 KB max)
   because Blazor's CoreLib methods feed into larger cross-assembly chains rather than forming their own isolated SCC.

7. **Implication for SCC-breaking strategy:** Focus the CoreLib SCC analysis on the browser sample's 1,090-method SCC.
   For Blazor, the biggest wins come from breaking Linq.Expressions and Text.Json SCCs, not CoreLib.

---

## Phase 1: Inventory & Categorize into Sub-Clusters

For each major namespace area, break into 2-3 sub-areas and map to source files.

### 1A. System Primitives (864 methods)

Sub-areas:
- **1A-i. Numeric types & formatting** — Int32, UInt32, Int64, Double, Single, Half, Decimal, Int128, UInt128, Number (formatting/parsing), Convert
- **1A-ii. String & core types** — String, Char, Enum, Array, Object, ValueType, Delegate, Guid, DateTime, TimeSpan, TimeZoneInfo, DateOnly, TimeOnly
- **1A-iii. Infrastructure** — ThrowHelper, SR, Buffer, SpanHelpers, MemoryExtensions, HashCode, Marvin, BitConverter, Random

### 1B. Globalization (482 methods)

Sub-areas:
- **1B-i. Culture infrastructure** — CultureInfo, CultureData, CompareInfo, TextInfo, GlobalizationMode
- **1B-ii. Number & date formatting** — NumberFormatInfo, DateTimeFormatInfo, DateTimeFormat, TimeSpanFormat, TimeSpanParse
- **1B-iii. Calendars** — GregorianCalendar, HebrewCalendar, HijriCalendar, JapaneseCalendar, PersianCalendar, UmAlQuraCalendar, CalendricalCalculationsHelper, CalendarData

### 1C. Reflection (278 methods)

Sub-areas:
- **1C-i. Core type system** — RuntimeType, Type, RuntimeTypeHandle, RuntimeTypeCache, MemberInfoCache
- **1C-ii. Members & invocation** — MethodBase, MethodBaseInvoker, FieldInfo, PropertyInfo, ConstructorInfo, FieldAccessor, InvokerEmitUtil, CustomAttribute
- **1C-iii. Assembly & module** — Assembly, RuntimeAssembly, AssemblyLoadContext, Module, RuntimeModule, AssemblyName

### 1D. Reflection.Emit (177 methods)

Sub-areas:
- **1D-i. Type construction** — RuntimeTypeBuilder, RuntimeEnumBuilder, RuntimeGenericTypeParameterBuilder, TypeBuilderInstantiation
- **1D-ii. IL generation** — RuntimeILGenerator, DynamicMethod, DynamicILGenerator, DynamicResolver, DynamicScope
- **1D-iii. Support** — RuntimeModuleBuilder, RuntimeAssemblyBuilder, SignatureHelper, RuntimeMethodBuilder, RuntimeConstructorBuilder

### 1E. Text & Encoding (180 methods)

Sub-areas:
- **1E-i. StringBuilder & formatting** — StringBuilder, ValueStringBuilder, StringBuilderCache, DefaultInterpolatedStringHandler
- **1E-ii. Encoding framework** — Encoding, UTF8Encoding, UnicodeEncoding, Encoder/Decoder, fallbacks
- **1E-iii. Unicode utilities** — Ascii, Utf8Utility, Utf16Utility, Rune, UnicodeDebug

### 1F. Runtime Intrinsics & Numerics (142 methods)

Sub-areas:
- **1F-i. Scalar\`1 and numeric interfaces** — Scalar\`1, INumberBase, IBinaryInteger, IFloatingPoint
- **1F-ii. Vector and SIMD** — Vector128/256/512/64, VectorMath, PackedSimd, WasmBase
- **1F-iii. Generic numerics** — Vector\`1, BitOperations, Vector (non-generic)

### 1G. Collections (93 methods)

Sub-areas:
- **1G-i. Dictionary-family** — Dictionary\`2, ConcurrentDictionary\`2, HashSet\`1, Hashtable
- **1G-ii. Comparer infrastructure** — EqualityComparer, Comparer, sorting helpers, NonRandomizedStringEqualityComparer
- **1G-iii. Lists & queues** — List\`1, Queue\`1, ValueListBuilder, ReadOnlyCollection

### 1H. Threading & Tasks (79 methods in Threading, 3 in Tasks SCC core)

Sub-areas:
- **1H-i. Thread primitives** — Thread, ThreadPool, Monitor, Lock, Volatile, Interlocked
- **1H-ii. Synchronization** — WaitHandle, SemaphoreSlim, ManualResetEventSlim, CancellationToken, Timer
- **1H-iii. Async machinery** — Task, ValueTask, async method builders, TaskScheduler, TaskAwaiter

### 1I. IO & FileSystem (131 methods)

Sub-areas:
- **1I-i. Stream hierarchy** — Stream, MemoryStream, UnmanagedMemoryStream, BinaryReader
- **1I-ii. File system** — File, FileStream, BufferedFileStreamStrategy, Path, Directory, FileSystemEnumerator
- **1I-iii. File handles & interop** — SafeFileHandle, FileStatus, RandomAccess, OSFileStreamStrategy

### 1J. Buffers & Search (63 methods)

Sub-areas:
- **1J-i. ArrayPool** — ArrayPool\`1, SharedArrayPool, SharedArrayPoolPartitions
- **1J-ii. SearchValues** — SearchValues, IndexOfAnyAsciiSearcher, ProbabilisticMap, AsciiCharSearchValues
- **1J-iii. Binary primitives** — BinaryPrimitives, SpanAction, MemoryManager

### 1K. Runtime CompilerServices (52 methods)

Sub-areas:
- **1K-i. Runtime helpers** — RuntimeHelpers, CastHelpers, CastCache, MethodTable, TypeHandle
- **1K-ii. Async builders** — AsyncTaskMethodBuilder, AsyncValueTaskMethodBuilder, PoolingAsyncValueTaskMethodBuilder
- **1K-iii. Interop/marshalling** — Unsafe, ConditionalWeakTable, FormattableStringFactory

### 1L. Interop & Marshalling (38 methods)

Sub-areas:
- **1L-i. Marshal core** — Marshal, GCHandle, NativeMemory, NativeLibrary
- **1L-ii. SafeHandle hierarchy** — SafeHandle, SafeFileHandle, SafeWaitHandle, SafeHandleZeroOrMinusOneIsInvalid
- **1L-iii. JS Interop** — JSMarshalerArgument, JSHostImplementation, JavaScriptExports

### 1M. Diagnostics & Tracing (17 methods)

Sub-areas:
- **1M-i. Stack traces** — StackTrace, StackFrame, StackFrameHelper
- **1M-ii. EventSource** — EventSource, NativeRuntimeEventSource, FrameworkEventSource

### 1N. Resources & Serialization (small)

- Resources — ResourceManager, ResourceReader, SR
- Serialization — SerializationInfo

---

## Phase 2: Dependency Analysis per Sub-Cluster

For each sub-cluster above (parallel sub-agents):

1. **List source files** — Map sub-cluster types to actual .cs files (managed C# only)
2. **Scan outbound refs** — grep for type names from OTHER sub-clusters referenced in this sub-cluster's source
3. **Scan inbound refs** — grep for this sub-cluster's type names in other sub-cluster source files
4. **Identify coupling methods** — the specific methods/properties that create cross-cluster edges
5. **Note `#if TARGET_BROWSER`, `#if FEATURE_WASM_MANAGED_THREADS`** — conditionally compiled code
6. **Estimate cut safety** — for each cross-cluster edge, assess whether breaking it would affect Blazor scenarios

---

## Phase 3: Characterize Key Coupling Points — 30 Theories

### Already-known coupling chains

1. **Exception.ToString() -> StackTrace -> Reflection** — every exception type drags in StackTrace which uses Reflection to format frames
2. **RuntimeType -> Reflection.Emit** — RuntimeType uses Emit for dynamic invocation
3. **CultureInfo <-> Number formatting <-> all numeric primitives** — every Int32.ToString() needs NumberFormatInfo -> CultureInfo -> CultureData
4. **Thread/ThreadPool -> Task -> async builders** — threading primitives are coupled to the entire async chain
5. **String <-> CompareInfo <-> CultureInfo** — string comparison ops route through globalization
6. **SafeFileHandle -> ThreadPool (async IO completion)** — file handle async ops register with ThreadPool (managed code path)
7. **Array.Sort -> Comparer -> generic interface dispatch** — sorting pulls in comparer infrastructure
8. **Type.GetType() -> AssemblyLoadContext -> Assembly -> Reflection** — type loading chain

### New theories to investigate

9. **Enum.ToString() -> RuntimeType -> Reflection** — Enum formatting uses reflection to get names
10. **DefaultBinder -> RuntimeType -> all of Reflection** — method resolution pulls in complete type system
11. **Convert class -> every numeric type + DateTime + String** — universal conversion hub
12. **DateTime.ToString() -> DateTimeFormat -> CultureInfo -> CalendarData -> ALL calendars** — date formatting pulls in all calendar implementations
13. **Scalar\`1 (10,240 bytes) <-> all numeric types** — generic SIMD scalar bridges to every INumber implementor
14. **StringBuilder.AppendFormat -> IFormattable -> all formattable types** — format infrastructure spans the whole SCC
15. **Encoding.GetEncoding -> all Encoding subclasses** — encoding registry pulls in UTF8, Unicode, etc.
16. **Stream virtual methods -> FileStream -> FileSystem -> Interop -> SafeHandle** — stream hierarchy forces file system inclusion
17. **AssemblyLoadContext -> NativeLibrary -> Marshal** — assembly loading drags in native interop
18. **DynamicMethod -> RuntimeILGenerator -> SignatureHelper -> RuntimeType** — DM creation is a reflection cycle
19. **ThrowHelper -> every exception type -> Exception -> StackTrace** — ThrowHelper is a universal SCC entry point
20. **CalendricalCalculationsHelper (3,059 bytes) -> pulled in by DateTimeFormatInfo** — large calendar math included for all cultures
21. **CompareInfo -> Ordinal/OrdinalCasing -> Char -> Unicode tables** — comparison pulls in casing tables
22. **ConcurrentDictionary -> uses Lock/Monitor & EqualityComparer** — ties threading to collections to type system
23. **GC -> Thread -> ThreadPool -> Timer** — GC infrastructure connected to threading
24. **MetadataReader (external, 6,148 bytes) -> pulled in by Reflection.Metadata -> Reflection.Emit** — external assembly MetadataReader in the SCC
25. **SerializationInfo -> RuntimeType -> activator** — serialization requires type activation
26. **FieldAccessor -> Reflection.Emit (InvokerEmitUtil)** — field access uses dynamic codegen
27. **Resource loading -> Assembly.GetManifestResourceStream -> Stream** — resource loading couples to IO
28. ~~**Random -> Interop.Sys (Unix random)**~~ — out of scope (native interop)
29. **TimeZoneInfo (14,512 bytes) -> IO (file reading) + Globalization** — timezone loading needs file access and culture
30. **IO.Enumeration -> PathInternal -> String operations -> MemoryExtensions** — directory enumeration pulls in span/string infrastructure

---

## Phase 4: Browser/WASM Context Analysis

1. **Audit `#if` conditionals** for `TARGET_BROWSER`, `TARGET_WASI`, `FEATURE_WASM_MANAGED_THREADS` (managed C# only)
2. **Review ILLink substitution files** — `src/libraries/System.Private.CoreLib/src/ILLink/` — understand what is already stubbed out
3. **Check .csproj/.projitems** for browser-specific file inclusions/exclusions
4. **Review existing feature switches** — catalog which trimmer-friendly switches exist and which are already active for browser publishes
5. **Note browser specifics:**
   - Single-threaded (no real Thread.Start, but ThreadPool exists)
   - Minimal filesystem (MEMFS or no real FS)
   - No native library loading
   - Non-invariant globalization (ICU via JS or system ICU)

---

## Phase 5: Document Sub-Clusters

Write `sub-clusters.md` with for each sub-cluster:
1. **Name** — what it does
2. **Member types** — list of types
3. **Own IL size** — sum of own sizes
4. **Dependencies in** — which sub-clusters depend on this one
5. **Dependencies out** — which sub-clusters this one depends on
6. **Coupling methods** — specific methods creating cross-cluster edges
7. **Browser relevance** — how critical this is for Blazor WASM scenarios

---

## Phase 6: Propose Safe Cuts

For each identified coupling point, produce a prioritized list of cut candidates:
1. **Cut description** — which cross-cluster edge to break and how
2. **IL savings estimate** — how many bytes/methods would become trimmable
3. **Safety assessment** — would Blazor still work? Would reflection still work? Any observable behavior changes?
4. **Confidence level** — high/medium/low based on how well-understood the coupling is
5. **Prerequisites** — does this cut depend on another cut being made first?

Prioritize by: (IL savings) × (safety confidence) — biggest safe wins first.

---

## Phase 7: Validate Cuts

1. **Re-run method-cost** on modified builds to confirm SCC breakage
2. **Measure published Blazor WASM app size** before/after
3. **Run library test suites** for affected areas to catch regressions
4. **Iterate** — if a cut doesn't break the SCC as expected, investigate why and adjust

---

## Execution Strategy

- Phase 0B: Run method-cost on Blazor app, compare with browser sample
- Phase 1: Single pass, categorize from method-cost JSON + source file mapping
- Phase 2: **Parallel sub-agents** — one per major area (1A through 1N), each:
  - Maps types to source files (managed C# only)
  - Greps for outbound/inbound references
  - Identifies coupling methods
  - Assesses cut safety for cross-cluster edges
- Phase 3: After Phase 2 data, validate/refute theories using actual cross-references
- Phase 4: Single focused pass on browser-specific conditionals and existing trimming
- Phase 5: Consolidate into sub-clusters document
- Phase 6: Produce prioritized cut proposals ranked by savings × safety
- Phase 7: Validate top cuts with actual builds and tests
