# System.Private.CoreLib SCC Research Plan

## Context

**Goal:** Break the main SCC in System.Private.CoreLib for browser/WASM targets into sub-clusters, characterize each, map inter-cluster dependencies, and identify safe dependency-cut candidates to reduce size as much as possible. The browser sample has a 942-method CoreLib SCC (159.2 KB, 34.8% of total IL). The Blazor app has no single dominant CoreLib SCC; its cost is driven by external assemblies (Linq.Expressions, Text.Json, Components). The largest Blazor SCC is 99 methods in Linq.Expressions.Compiler (316.2 KB). With matching globalization flags (InvariantGlobalization=true, PredefinedCulturesOnly=true), the browser SCC core is 100% contained in Blazor — any SCC-breaking work on the browser sample directly benefits Blazor.

**Target:** IL-trimmed `System.Private.CoreLib.dll` for browser/WASM (CoreCLR build). The sample app folder name contains "mono" but the actual build is CoreCLR.

**Scope:** Managed C# code only. Native code, JS interop glue, and emscripten dependencies are out of scope.

**Assumptions:**
- Invariant globalization (InvariantGlobalization=true, PredefinedCulturesOnly=true — matching Blazor defaults)
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
| Total methods | 10,339 | 25,373 |
| Total IL size | 554,647 bytes (541.6 KB) | 1,503,162 bytes (1,467.9 KB) |
| Max transitive size | 241,207 bytes (235.6 KB, 43.5%) | 460,727 bytes (449.9 KB, 30.7%) |
| Largest super-SCC | 942 methods | 1,046 methods |
| SCC transitive size | 163,058 bytes (159.2 KB, 34.8%) | — |
| Tarjan direct SCCs (multi) | 7 | 31 |
| Tarjan super-SCCs (multi) | 15 | 81 |

*Browser sample built with InvariantGlobalization=true, PredefinedCulturesOnly=true (matching Blazor defaults).*

## Browser SCC Core Namespace Distribution (942 methods, 163,058 bytes = 159.2 KB)

| Methods | Namespace | Notes |
|---------|-----------|-------|
| 336 | System | Primitives, Number, String, Array, DateTime, Enum, Convert, etc. |
| 119 | System.Text | StringBuilder, Encoding, UTF8, Unicode utilities |
| 102 | System.Runtime.Intrinsics | Scalar\`1, Vector128/256/512 helpers |
| 87 | System.Reflection | RuntimeType, CustomAttribute, MethodBase, FieldInfo, etc. |
| 68 | System.Reflection.Emit | TypeBuilder, ILGenerator, ModuleBuilder, SignatureHelper |
| 68 | System.Globalization | DateTimeFormatInfo, NumberFormatInfo, CultureInfo (calendars trimmed) |
| 27 | System.Threading | Thread, ThreadPool, Lock, Monitor, Timer |
| 27 | System.Collections.Generic | Dictionary, List, HashSet, Comparer, EqualityComparer |
| 23 | System.Reflection.Metadata | MetadataReader (external assembly) |
| 22 | System.Buffers | ArrayPool, SearchValues, IndexOfAnyAsciiSearcher |
| 15 | System.Runtime.CompilerServices | RuntimeHelpers, CastHelpers |
| 14 | System.Numerics | Vector\`1, BitOperations |
| 13 | System.Runtime.InteropServices | Marshal, GCHandle, SafeHandle |
| 9 | System.Text.Unicode | Utf8Utility, Utf16Utility |
| 4 | System.Runtime.InteropServices.Marshalling | SafeHandleMarshaller, Utf8StringMarshaller |
| 3 | System.Runtime.Loader | AssemblyLoadContext |
| 2 | System.Collections | Non-generic collections |
| 1 | System.Buffers.Text | Utf8Formatter |
| 1 | Microsoft.Win32.SafeHandles | SafeFileHandle |

*Compared to non-invariant build: SCC dropped from 1,090 to 942 methods. Globalization went from 163 to 68 methods (95 calendar/CultureData methods trimmed). System dropped from 375 to 336 (39 fewer).*

---

## Phase 0: Method-Cost Analysis

### 0A. Browser sample app [DONE]
- [x] Run method-cost tool on `d:\runtime2\src\mono\sample\wasm\browser\bin\publish\wwwroot\_framework`
- [x] Report saved to `d:\runtime2\method-cost-full.json` (n=5000)
- [x] Built with InvariantGlobalization=true, PredefinedCulturesOnly=true (matching Blazor defaults)
- [x] 4 assemblies, 10,339 methods, 541.6 KB total IL
- [x] Largest super-SCC: 942 methods (identical transitiveSize=163,058 bytes = 159.2 KB, 34.8%)
- [x] Max transitive size: 241,207 bytes (235.6 KB, 43.5%) — Sample.PrintMeaning async -> Stream -> SemaphoreSlim -> threading/IO chain
- [x] Top methods by transitive: PrintMeaning (235.6 KB), Stream.BeginWrite (232.1 KB), SemaphoreSlim.WaitUntil (231.6 KB)
- [x] Mapped namespace distribution (see table above)
- [x] **Previous run (InvariantGlobalization=false):** 10,821 methods, 573.4 KB, SCC=1,090 methods/178.4 KB. InvariantGlobalization trimmed 148 SCC methods (mostly calendars/CultureData) and 19.2 KB from SCC.

### 0B. Blazor WASM app [DONE]
- [x] Run method-cost tool on `d:\samples\blazorwasmruntime\bin\Release\net11.0\publish\wwwroot\_framework\`
- [x] Report saved to `d:\runtime2\method-cost-full-blazor.json` (n=17000)
- [x] 40 assemblies, 25,373 methods (16,629 in report), 1,467.9 KB total IL
- [x] Largest super-SCC: 1,046 methods, max transitive 460,727 bytes (449.9 KB, 30.7%)

**Key findings:**

1. **With matching flags, browser is nearly a strict superset of Blazor's CoreLib subset.**
   After rebuilding with InvariantGlobalization=true and PredefinedCulturesOnly=true (matching Blazor defaults),
   only **9 browser-only methods** remain (excl. 8 app-specific). These are minor: System (5),
   Runtime.CompilerServices (2), Threading.Tasks (2). **100% of the browser SCC core (934 methods) is present in Blazor.**
   This means any SCC-breaking work on the browser sample directly benefits Blazor apps.
   - Previous run (non-invariant): 323 browser-only methods (297 globalization) — that gap is now closed.

2. **No single dominant CoreLib SCC in Blazor.** The browser sample has a clean 942-method CoreLib SCC.
   In Blazor, the CoreLib methods are fragmented across many smaller groups (largest: 54 methods in Reflection at 231.0 KB).

3. **Blazor cost is driven by external assemblies:**
   - Linq.Expressions: 170.2 KB own, max transitive 432.9 KB
   - Text.Json: 161.8 KB own, max transitive 432.8 KB
   - Components: 91.0 KB own, max transitive 449.9 KB
   - Net.Http: 55.6 KB own, max transitive 399.9 KB

4. **Blazor true SCCs (identical transitiveSize = definite mutual recursion):**
   - 61 methods in Linq.Expressions.Interpreter (354.0 KB)
   - 99 methods in Linq.Expressions.Compiler (316.2 KB)
   - 97 methods in Linq.Expressions core (304.1 KB)
   - 42 methods in Linq.Expressions.Compiler (283.7 KB)
   - 32 methods in Text.Json.Nodes (284.5 KB, two groups of 16)
   - 29 methods in IO/Net.Http (263.2 KB)
   - 23 methods in Components.RenderTree/Rendering (277.6 KB)
   - 22 methods in Threading.Tasks (189.2 KB)
   - 54 methods in Reflection (231.0 KB)
   - 20 methods in Linq.Expressions.Interpreter (336.4 KB)
   - 15 methods in Text.Json.Serialization (297.6 KB)
   - 14 methods in Text.Json.Serialization.Converters (332.2 KB)
   - 11 methods in Collections.Frozen (180.1 KB, two groups)
   - 11 methods in DI.ServiceLookup (258.0 KB)

5. **Zero overlap** between browser SCC core (934 unique names) and Blazor SCC core (100 names).
   Browser SCC = CoreLib primitives/text/reflection/intrinsics.
   Blazor SCC = Linq.Expressions.Compiler (99) + 1 Components method.
   However, all 934 browser SCC methods are individually present in Blazor (just not forming a single SCC there).

6. **Assembly cost ranking in Blazor (by max transitive):**
   Components 449.9 KB > Linq.Expressions 432.9 KB > Text.Json 432.8 KB > Net.Http 399.9 KB > CoreLib 324.6 KB

7. **Implication for SCC-breaking strategy:** Focus the CoreLib SCC analysis on the browser sample's 942-method SCC.
   For Blazor, the biggest wins come from breaking Linq.Expressions and Text.Json SCCs, not CoreLib.
   With matching globalization flags, the browser SCC core is 100% contained in Blazor — any cuts apply to both.

8. **Effect of InvariantGlobalization=true on browser sample:**
   - Methods: 10,821 → 10,339 (482 fewer, ~4.5%)
   - Total IL: 573.4 KB → 541.6 KB (31.8 KB savings)
   - SCC: 1,090 → 942 methods (148 fewer, -13.6%)
   - SCC transitive: 178.4 KB → 159.2 KB (19.2 KB savings, -10.8%)
   - Globalization in SCC: 163 → 68 methods (95 calendar/CultureData methods trimmed)
   - System namespace in SCC: 375 → 336 (39 fewer)

---

## Phase 1: Inventory & Categorize into Sub-Clusters

For each major namespace area, break into 2-3 sub-areas and map to source files.

### 1A. System Primitives (736 methods — 336 in SCC + ~400 satellite)

Sub-areas:
- **1A-i. Numeric types & formatting** — Int32, UInt32, Int64, Double, Single, Half, Decimal, Int128, UInt128, Number (formatting/parsing), Convert
- **1A-ii. String & core types** — String, Char, Enum, Array, Object, ValueType, Delegate, Guid, DateTime, TimeSpan, TimeZoneInfo, DateOnly, TimeOnly
- **1A-iii. Infrastructure** — ThrowHelper, SR, Buffer, SpanHelpers, MemoryExtensions, HashCode, Marvin, BitConverter, Random

### 1B. Globalization (68 SCC methods — invariant mode, calendars trimmed)

Sub-areas:
- **1B-i. Culture infrastructure** — CultureInfo, CultureData (minimal), GlobalizationMode
- **1B-ii. Number & date formatting** — NumberFormatInfo, DateTimeFormatInfo, DateTimeFormat, TimeSpanFormat, TimeSpanParse
- ~~**1B-iii. Calendars**~~ — trimmed by InvariantGlobalization=true

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

### Phase 1 Results: SCC Method-to-File Inventory

**942 methods, 100,349 bytes own IL, 162 distinct types, 29 sub-clusters**

#### Summary Table

| Sub-Cluster | Methods | Own IL | Types | Key Source Files |
|-------------|--------:|-------:|------:|------------------|
| 1C-i TypeSystem | 116 | 11,190B | 8 | RuntimeType*.cs, Type*.cs |
| 1A-ii CoreTypes | 96 | 10,594B | 17 | Enum.cs, MemoryExtensions.cs, String*.cs, Array.cs, HexConverter.cs |
| 1A-iii Infrastructure | 55 | 10,436B | 7 | SpanHelpers*.cs (9,774B), ThrowHelper.cs, SR.cs |
| 1F-i Scalar | 11 | 8,177B | 1 | Scalar.cs (single type, huge per-method IL) |
| 1E-iii Unicode | 30 | 7,944B | 4 | Utf8Utility*.cs, Ascii*.cs, Utf16Utility*.cs, Utf8.cs |
| 1C-ii Members | 78 | 7,933B | 18 | TypeNameResolver*.cs, DefaultBinder.cs, CustomAttribute*.cs, Associates.cs |
| 1A-i Numeric | 59 | 6,669B | 14 | Number.*.cs (4,836B), UInt32.cs, UInt64.cs, ParseNumbers.cs |
| 1B-ii Formatting | 51 | 4,693B | 8 | NumberFormatInfo.cs, Ordinal.cs, InvariantModeCasing.cs, TextInfo*.cs |
| 1E-ii Encoding | 68 | 4,673B | 11 | Encoding*.cs, UTF8Encoding*.cs, *FallbackBuffer files |
| 1F-ii Vector | 91 | 4,338B | 9 | Vector128*.cs, Vector64.cs, Vector256.cs, Vector512.cs, VectorMath.cs |
| 1C-iii Assembly | 19 | 3,709B | 5 | AssemblyNameParser.cs, RuntimeAssembly.cs, RuntimeModule.cs |
| 1C-iv Metadata | 26 | 2,822B | 5 | TypeNameParser*.cs, TypeName.cs, AssemblyNameInfo.cs (System.Reflection.Metadata) |
| 1G-i Dictionary | 19 | 2,659B | 3 | Dictionary.cs, HashSet.cs, Hashtable.cs |
| 1E-i StringBuilder | 30 | 2,561B | 3 | StringBuilder*.cs, ValueStringBuilder.AppendFormat.cs |
| 1D-iii EmitSupport | 31 | 2,032B | 4 | TypeNameBuilder.cs, SymbolType.cs, RuntimeModuleBuilder.cs |
| 1J-i ArrayPool | 13 | 1,404B | 5 | SharedArrayPool.cs, ArrayPool.cs |
| 1D-i TypeConstruction | 35 | 1,353B | 3 | RuntimeTypeBuilder.cs, TypeBuilderInstantiation.cs |
| 1H-i ThreadPrimitives | 16 | 1,244B | 3 | Lock.cs (1,112B), Monitor*.cs, Thread*.cs |
| 1K-iii CompilerServices | 15 | 1,210B | 3 | DefaultInterpolatedStringHandler.cs, ConditionalWeakTable.cs, Unsafe.cs |
| 1H-ii Synchronization | 11 | 899B | 5 | Interlocked*.cs, WaitHandle*.cs, EventWaitHandle*.cs |
| 1B-i CultureInfra | 17 | 781B | 2 | CultureInfo*.cs, CultureData*.cs |
| 1F-iii GenericNumerics | 14 | 755B | 4 | Vector.cs, BitOperations.cs, INumberBase.cs |
| 1G-iii Lists | 10 | 703B | 2 | List.cs, ValueListBuilder.cs |
| 1L-i Marshal | 7 | 519B | 3 | MemoryMarshal*.cs, Marshal*.cs, Interop.Libraries.cs |
| 1L-iii Marshalling | 4 | 348B | 4 | Utf8StringMarshaller.cs, Utf16StringMarshaller.cs, ReadOnlySpanMarshaller.cs |
| 1L-ii SafeHandle | 8 | 274B | 2 | SafeHandle.cs, SafeFileHandle*.cs |
| 1J-ii SearchValues | 7 | 269B | 6 | Any1-5SearchValues.cs, ProbabilisticMap.cs |
| 1J-iii BinaryPrimitives | 3 | 117B | 2 | FormattingHelpers*.cs, Utilities.cs |
| 1D-ii ILGeneration | 2 | 43B | 1 | DynamicMethod*.cs |

#### Top 20 Types by Own IL (core cost drivers)

| Type | Own IL | Methods | Sub-Cluster |
|------|-------:|--------:|-------------|
| SpanHelpers | 9,774B | 31 | 1A-iii Infrastructure |
| Scalar\`1 | 8,177B | 11 | 1F-i Scalar |
| Number | 4,836B | 25 | 1A-i Numeric |
| RuntimeType | 4,522B | 52 | 1C-i TypeSystem |
| MemberInfoCache\`1 | 4,116B | 18 | 1C-i TypeSystem |
| Utf8Utility | 3,749B | 5 | 1E-iii Unicode |
| Ascii | 3,425B | 21 | 1E-iii Unicode |
| MemoryExtensions | 2,654B | 20 | 1A-ii CoreTypes |
| Enum | 2,048B | 10 | 1A-ii CoreTypes |
| String | 1,834B | 25 | 1A-ii CoreTypes |
| AssemblyNameParser | 1,775B | 8 | 1C-iii Assembly |
| Encoding | 1,682B | 20 | 1E-ii Encoding |
| TypeNameResolver | 1,459B | 8 | 1C-ii Members |
| DefaultBinder | 1,374B | 8 | 1C-ii Members |
| CustomAttribute | 1,365B | 13 | 1C-ii Members |
| Dictionary\`2 | 1,328B | 9 | 1G-i Dictionary |
| UTF8Encoding | 1,308B | 19 | 1E-ii Encoding |
| StringBuilder | 1,284B | 14 | 1E-i StringBuilder |
| NumberFormatInfo | 1,230B | 13 | 1B-ii Formatting |
| HashSet\`1 | 1,156B | 8 | 1G-i Dictionary |

#### Detailed Source File Mapping

**1C-i TypeSystem** (116 methods, 11,190B) — RuntimeType, Type, RuntimeTypeHandle, RuntimeTypeCache
- `src/coreclr/System.Private.CoreLib/src/System/RuntimeType.CoreCLR.cs` — RuntimeType coreclr-specific (52m, 4,522B)
- `src/coreclr/System.Private.CoreLib/src/System/RuntimeType.ActivatorCache.cs` — Activator cache
- `src/coreclr/System.Private.CoreLib/src/System/RuntimeType.BoxCache.cs` — Generic boxing cache
- `src/coreclr/System.Private.CoreLib/src/System/RuntimeType.GenericCache.cs` — IGenericCacheEntry (4m, 476B)
- `src/libraries/System.Private.CoreLib/src/System/RuntimeType.cs` — Shared RuntimeType logic
- `src/libraries/System.Private.CoreLib/src/System/Type.cs` — System.Type base (22m, 1,168B)
- `src/coreclr/System.Private.CoreLib/src/System/RuntimeHandles.cs` — RuntimeTypeHandle, RuntimeMethodHandle, RuntimeFieldHandle

**1A-ii CoreTypes** (96 methods, 10,594B) — String, Enum, Array, MemoryExtensions, Span, Delegate, etc.
- `src/libraries/System.Private.CoreLib/src/System/MemoryExtensions*.cs` — MemoryExtensions (20m, 2,654B)
- `src/libraries/System.Private.CoreLib/src/System/Enum.cs` + `Enum.EnumInfo.cs` — Enum formatting/parsing (10m, 2,048B)
- `src/libraries/System.Private.CoreLib/src/System/String.*.cs` — String ops (25m, 1,834B)
- `src/libraries/Common/src/System/HexConverter.cs` — Hex encode/decode (6m, 937B)
- `src/libraries/System.Private.CoreLib/src/System/Array.cs` — Array.Copy etc. (4m, 685B)
- `src/coreclr/System.Private.CoreLib/src/System/RuntimeHandles.cs` — ModuleHandle (5m, 572B)
- `src/libraries/System.Private.CoreLib/src/System/Span.cs` — Span\`1 (6m, 304B)

**1A-iii Infrastructure** (55 methods, 10,436B) — SpanHelpers, ThrowHelper, SR, exceptions
- `src/libraries/System.Private.CoreLib/src/System/SpanHelpers.*.cs` — **SpanHelpers (31m, 9,774B)** — dominates this cluster
- `src/libraries/System.Private.CoreLib/src/System/ThrowHelper.cs` — Exception throwers (10m, 169B)
- `src/libraries/System.Private.CoreLib/src/System/SR.cs` — String resources (4m, 207B)

**1F-i Scalar** (11 methods, 8,177B) — Scalar\`1 generic SIMD scalar ops
- `src/libraries/System.Private.CoreLib/src/System/Runtime/Intrinsics/Scalar.cs` — Single file, all 8.2KB
- Top methods: AddSaturate (1,097B), SubtractSaturate (1,084B), Min (797B), Add (755B)

**1E-iii Unicode** (30 methods, 7,944B) — Ascii, Utf8Utility, Utf16Utility, Utf8
- `src/libraries/System.Private.CoreLib/src/System/Text/Unicode/Utf8Utility*.cs` — (5m, 3,749B)
- `src/libraries/System.Private.CoreLib/src/System/Text/Ascii*.cs` — (21m, 3,425B)
- `src/libraries/System.Private.CoreLib/src/System/Text/Unicode/Utf16Utility*.cs` — (2m, 467B)
- `src/libraries/System.Private.CoreLib/src/System/Text/Unicode/Utf8.cs` — (2m, 303B)

**1C-ii Members** (78 methods, 7,933B) — Reflection members: methods, fields, binder, custom attributes
- `src/libraries/System.Private.CoreLib/src/System/Reflection/TypeNameResolver.cs` + CoreCLR variant — (8m, 1,459B)
- `src/libraries/System.Private.CoreLib/src/System/DefaultBinder.cs` — (8m, 1,374B)
- `src/coreclr/System.Private.CoreLib/src/System/Reflection/RuntimeCustomAttributeData.cs` — CustomAttribute (13m, 1,365B)
- `src/coreclr/System.Private.CoreLib/src/System/Reflection/Associates.cs` — Property/event accessors (2m, 656B)
- `src/libraries/System.Private.CoreLib/src/System/Reflection/SignatureTypeExtensions.cs` — (9m, 713B)
- `src/coreclr/System.Private.CoreLib/src/System/Reflection/PseudoCustomAttribute` — (7m, 674B)
- `src/coreclr/System.Private.CoreLib/src/System/Reflection/RuntimePropertyInfo.cs` — (5m, 392B)
- `src/coreclr/System.Private.CoreLib/src/System/Reflection/RuntimeParameterInfo.cs` — (5m, 372B)

**1A-i Numeric** (59 methods, 6,669B) — Number formatting/parsing, Convert, Math, numeric primitives
- `src/libraries/System.Private.CoreLib/src/System/Number.*.cs` — Number (25m, 4,836B)
- `src/libraries/System.Private.CoreLib/src/System/UInt32.cs` — (5m, 476B)
- `src/libraries/System.Private.CoreLib/src/System/ParseNumbers.cs` — (3m, 462B)
- `src/libraries/System.Private.CoreLib/src/System/UInt64.cs` — (2m, 445B)

**1B-ii Formatting** (51 methods, 4,693B) — NumberFormatInfo, CompareInfo, TextInfo, casing
- `src/libraries/System.Private.CoreLib/src/System/Globalization/NumberFormatInfo.cs` — (13m, 1,230B)
- `src/libraries/System.Private.CoreLib/src/System/Globalization/Ordinal*.cs` — (5m, 967B)
- `src/libraries/System.Private.CoreLib/src/System/Globalization/InvariantModeCasing.cs` — (7m, 962B)
- `src/libraries/System.Private.CoreLib/src/System/Globalization/TextInfo*.cs` — (6m, 682B)
- `src/libraries/System.Private.CoreLib/src/System/Globalization/CharUnicodeInfo.cs` — (12m, 434B)

**1E-ii Encoding** (68 methods, 4,673B) — Encoding framework, UTF8, fallback buffers
- `src/libraries/System.Private.CoreLib/src/System/Text/Encoding*.cs` — Base Encoding (20m, 1,682B)
- `src/libraries/System.Private.CoreLib/src/System/Text/UTF8Encoding*.cs` — UTF8 (19m, 1,308B)
- Fallback buffers: EncoderFallbackBuffer (6m, 393B), DecoderFallbackBuffer (3m, 253B), etc.
- `src/libraries/Common/src/System/Text/ConsoleEncoding.cs` — ConsoleEncoding wrapper (10m, 161B)

**1F-ii Vector** (91 methods, 4,338B) — Vector128/256/512/64 SIMD operations
- `src/libraries/System.Private.CoreLib/src/System/Runtime/Intrinsics/Vector128*.cs` — Vector128 (50m, 1,709B)
- `src/libraries/System.Private.CoreLib/src/System/Runtime/Intrinsics/Vector64.cs` — Vector64 (22m, 1,429B)
- `src/libraries/System.Private.CoreLib/src/System/Runtime/Intrinsics/Vector256.cs` — (9m, 531B)
- `src/libraries/System.Private.CoreLib/src/System/Runtime/Intrinsics/Vector512.cs` — (9m, 491B)
- `src/libraries/System.Private.CoreLib/src/System/Runtime/Intrinsics/VectorMath.cs` — (1m, 178B)

**1C-iii Assembly** (19 methods, 3,709B) — Assembly/module resolution, name parsing
- `src/libraries/Common/src/System/Reflection/AssemblyNameParser.cs` — (8m, 1,775B)
- `src/coreclr/System.Private.CoreLib/src/System/Reflection/RuntimeAssembly.cs` — (5m, 762B)
- `src/coreclr/System.Private.CoreLib/src/System/Reflection/RuntimeModule.cs` — (3m, 594B)
- `src/libraries/Common/src/System/Reflection/AssemblyNameFormatter.cs` — (2m, 559B)

**1C-iv Metadata** (26 methods, 2,822B) — System.Reflection.Metadata type name parsing
- `src/libraries/System.Reflection.Metadata/src/System/Reflection/Metadata/TypeNameParserHelpers.cs` — (11m, 894B)
- `src/libraries/System.Reflection.Metadata/src/System/Reflection/Metadata/TypeNameParser.cs` — (3m, 695B)
- `src/libraries/System.Reflection.Metadata/src/System/Reflection/Metadata/TypeName.cs` — (6m, 652B)
- `src/libraries/System.Reflection.Metadata/src/System/Reflection/Metadata/AssemblyNameInfo.cs` — (3m, 277B)
- `src/coreclr/System.Private.CoreLib/src/System/Reflection/MdImport.cs` — MetadataImport (3m, 304B)

**1G-i Dictionary** (19 methods, 2,659B) — Dictionary, HashSet, Hashtable
- `src/libraries/System.Private.CoreLib/src/System/Collections/Generic/Dictionary.cs` — (9m, 1,328B)
- `src/libraries/System.Private.CoreLib/src/System/Collections/Generic/HashSet.cs` — (8m, 1,156B)
- `src/libraries/System.Private.CoreLib/src/System/Collections/Hashtable.cs` — (2m, 175B)

**1E-i StringBuilder** (30 methods, 2,561B)
- `src/libraries/System.Private.CoreLib/src/System/Text/StringBuilder.cs` — (14m, 1,284B)
- `src/libraries/System.Private.CoreLib/src/System/Text/ValueStringBuilder.AppendFormat.cs` — (11m, 733B)
- `src/libraries/System.Private.CoreLib/src/System/Text/StringBuilder.cs` — AppendInterpolatedStringHandler (5m, 544B)

**1D-iii EmitSupport** (31 methods, 2,032B) — Type name building, symbol types, module/assembly builders
- `src/libraries/System.Private.CoreLib/src/System/Reflection/Emit/TypeNameBuilder.cs` — (15m, 1,047B)
- `src/libraries/System.Private.CoreLib/src/System/Reflection/Emit/SymbolType.cs` — (12m, 928B)

**1D-i TypeConstruction** (35 methods, 1,353B)
- `src/coreclr/System.Private.CoreLib/src/System/Reflection/Emit/RuntimeTypeBuilder.cs` — (13m, 773B)
- `src/libraries/System.Private.CoreLib/src/System/Reflection/Emit/TypeBuilderInstantiation.cs` — (17m, 519B)

**1J-i ArrayPool** (13 methods, 1,404B) — SharedArrayPool, partitioning, trimming
- `src/libraries/System.Private.CoreLib/src/System/Buffers/SharedArrayPool.cs` — (5m, 855B + 3m, 358B partitions)

**1H-i ThreadPrimitives** (16 methods, 1,244B) — Lock, Monitor, Thread
- `src/libraries/System.Private.CoreLib/src/System/Threading/Lock.cs` — Lock class (11m, 1,112B)

**1K-iii CompilerServices** (15 methods, 1,210B)
- `src/libraries/System.Private.CoreLib/src/System/Runtime/CompilerServices/DefaultInterpolatedStringHandler.cs` — (13m, 1,066B)

**1H-ii Synchronization** (11 methods, 899B) — Interlocked, WaitHandle, EventWaitHandle
- `src/libraries/System.Private.CoreLib/src/System/Threading/Interlocked.cs` — (2m, 454B)
- `src/libraries/System.Private.CoreLib/src/System/Threading/WaitHandle*.cs` — (4m, 309B)

**1B-i CultureInfra** (17 methods, 781B) — CultureInfo, CultureData
- `src/libraries/System.Private.CoreLib/src/System/Globalization/CultureInfo*.cs` — (13m, 675B)
- `src/libraries/System.Private.CoreLib/src/System/Globalization/CultureData*.cs` — (4m, 106B)

**1F-iii GenericNumerics** (14 methods, 755B) — Vector\`1, BitOperations
- `src/libraries/System.Private.CoreLib/src/System/Numerics/Vector.cs` — (8m, 490B)
- `src/libraries/System.Private.CoreLib/src/System/Numerics/BitOperations.cs` — (5m, 185B)

**Remaining small clusters:** 1G-iii Lists (703B), 1L-i Marshal (519B), 1L-iii Marshalling (348B), 1L-ii SafeHandle (274B), 1J-ii SearchValues (269B), 1J-iii BinaryPrimitives (117B), 1D-ii ILGeneration (43B)

#### Key Observations from Phase 1

1. **SpanHelpers dominates infrastructure** — 9,774B (9.7% of total SCC IL) in a single utility class. It's pulled in transitively by virtually every span/string/memory operation.

2. **Scalar\`1 is disproportionately expensive** — 8,177B for just 11 methods. Each method has huge type-switch IL for all numeric types. This is the generic math bridge.

3. **Reflection is the largest domain** — 1C-i + 1C-ii + 1C-iii + 1C-iv + 1D-i + 1D-ii + 1D-iii = 328 methods, 29,747B (29.6%). Breaking Reflection-Emit from Reflection-Core would be the single biggest win.

4. **Text processing is #2** — 1E-i + 1E-ii + 1E-iii = 128 methods, 15,178B (15.1%). Encoding framework (4.6KB) is pulled in even when only UTF8 is needed on WASM.

5. **Numeric formatting is tightly coupled** — Number.cs (4,836B) + NumberFormatInfo (1,230B) + Ordinal/Casing (2,000B) form a chain from Int32.ToString() through CultureInfo.

6. **Vector types are shallow** — 91 Vector methods but only 4,338B total; they exist because SpanHelpers and Ascii call into them. The real cost is Scalar\`1 (8,177B).

7. **Collections are modest** — 2,659B for Dictionary/HashSet. These are in the SCC because EqualityComparer pulls in RuntimeType for generic dispatch.

8. **External assembly code in SCC** — System.Reflection.Metadata types (TypeNameParser, TypeName, AssemblyNameInfo) total 2,822B. These are compiled into CoreLib and pulled in by TypeNameResolver for Type.GetType().

---

## Phase 2: Dependency Analysis per Sub-Cluster

For each sub-cluster above (parallel sub-agents):

1. **List source files** — Map sub-cluster types to actual .cs files (managed C# only)
2. **Scan outbound refs** — grep for type names from OTHER sub-clusters referenced in this sub-cluster's source
3. **Scan inbound refs** — grep for this sub-cluster's type names in other sub-cluster source files
4. **Identify coupling methods** — the specific methods/properties that create cross-cluster edges
5. **Note `#if TARGET_BROWSER`, `#if FEATURE_WASM_MANAGED_THREADS`** — conditionally compiled code
6. **Estimate cut safety** — for each cross-cluster edge, assess whether breaking it would affect Blazor scenarios

### Phase 2 Results: Cross-Cluster Dependency Analysis

#### 2.1 Cluster-Level SCC Structure

All 29 sub-clusters form **one giant SCC** at the cluster level with **160 directed edges**.
At the coarse parent level (1A, 1B, ..., 1L), all **11 parent clusters** also form a single SCC with **56 directed edges**.

**Implication:** There is no easy "peel off" — every sub-cluster is reachable from every other sub-cluster.

#### 2.2 Cross-Cluster Dependency Matrix (Top Edges by Count)

| From | To | Edges | Key Coupling Pattern |
|------|----|------:|----------------------|
| 1A-iii Infrastructure | 1F-ii Vector | 64 | HexConverter uses Vector128 ops |
| 1E-iii Unicode | 1F-ii Vector | 49 | Utf8Utility/Ascii uses Vector128 for SIMD text |
| 1C-ii Members | 1C-i TypeSystem | 46 | TypeInfo.IsAssignableFrom, CustomAttribute -> Type |
| 1D-i TypeConstruction | 1C-i TypeSystem | 33 | TypeBuilderInstantiation -> RuntimeType virtuals |
| 1C-i TypeSystem | 1D-i TypeConstruction | 29 | RuntimeType.IsAssignableFrom -> TypeBuilder |
| 1A-ii CoreTypes | 1A-iii Infrastructure | 27 | Span, ReadOnlySpan -> ThrowHelper |
| 1C-ii Members | 1D-i TypeConstruction | 25 | TypeInfo, CustomAttribute -> TypeBuilder |
| 1C-i TypeSystem | 1A-ii CoreTypes | 24 | RuntimeType uses Span, Array.Copy |
| 1B-ii Formatting | 1E-ii Encoding | 21 | NumberFormatInfo.TChar() -> Encoding.GetBytes |
| 1E-ii Encoding | 1A-ii CoreTypes | 21 | Encoding -> ReadOnlySpan, ArgumentOutOfRange |
| 1A-ii CoreTypes | 1C-i TypeSystem | 21 | Enum, MulticastDelegate -> RuntimeType |
| 1F-ii Vector | 1A-iii Infrastructure | 21 | Vector128 -> ThrowHelper |
| 1C-iii Assembly | 1A-ii CoreTypes | 21 | AssemblyNameParser -> String.Equals |
| 1E-i StringBuilder | 1A-ii CoreTypes | 17 | ValueStringBuilder -> Span |
| 1C-i TypeSystem | 1C-ii Members | 16 | RuntimeType -> CustomAttribute, MemberInfo |

#### 2.3 Bidirectional Coupling (Strongest Cycles)

| Cluster Pair | Fwd+Bwd | Total | Cycle Nature |
|-------------|---------|------:|--------------|
| 1F-ii Vector <-> 1A-iii Infrastructure | 21+64 | 85 | Vector ThrowHelper <-> HexConverter vectorized |
| 1C-i TypeSystem <-> 1D-i TypeConstruction | 29+33 | 62 | RuntimeType <-> TypeBuilder mutual virtuals |
| 1C-i TypeSystem <-> 1C-ii Members | 16+46 | 62 | Type <-> CustomAttribute/MemberInfo |
| 1A-ii CoreTypes <-> 1C-i TypeSystem | 21+24 | 45 | Enum/Span <-> RuntimeType |
| 1A-ii CoreTypes <-> 1A-iii Infrastructure | 27+5 | 32 | Span <-> ThrowHelper/SR |
| 1E-ii Encoding <-> 1A-ii CoreTypes | 21+5 | 26 | Encoding <-> String/Span |
| 1C-ii Members <-> 1D-i TypeConstruction | 25+1 | 26 | TypeInfo/CustomAttr <-> TypeBuilder |
| 1E-i StringBuilder <-> 1A-ii CoreTypes | 17+4 | 21 | ValueStringBuilder <-> Span |
| 1B-ii Formatting <-> 1A-ii CoreTypes | 12+8 | 20 | CompareInfo <-> String/MemoryExtensions |
| 1D-iii EmitSupport <-> 1D-i TypeConstruction | 6+12 | 18 | SymbolType <-> TypeBuilderInstantiation |

#### 2.4 `Type.op_Equality` / `typeof(T)` Bottleneck

**18 out of 29 clusters** call `Type::op_Equality`, `Type::op_Inequality`, `Type::get_IsValueType`, or `Type::get_IsEnum` — pulling in the entire TypeSystem cluster (1C-i). This is the **#1 coupling mechanism** holding the SCC together.

| Calling Cluster | # Calls | Pattern |
|----------------|--------:|---------|
| 1C-i TypeSystem (self) | 27 | RuntimeType internal logic |
| 1C-ii Members | 24 | TypeInfo.IsAssignableFrom, CustomAttribute |
| 1D-i TypeConstruction | 14 | TypeBuilder.IsTypeEqual |
| 1A-ii CoreTypes | 11 | Enum.TryFormat, Array.Copy typeof chains |
| 1F-i Scalar | 11 | Scalar&lt;T&gt;.Add — `typeof(T) == typeof(byte)` chains |
| 1B-ii Formatting | 10 | NumberFormatInfo generic TChar dispatch |
| 1G-i Dictionary | 10 | `typeof(TKey).IsValueType` checks |
| 1F-ii Vector | 9 | Vector128.AddSaturate type checks |
| 1H-i ThreadPrimitives | 7 | Interlocked generic type guards |
| 1A-i Numeric | 6 | UInt64.CreateTruncating generic chains |

**Root cause**: All generic `typeof(T) ==` pattern matching compiles to `Type.op_Equality(Type.GetTypeFromHandle, Type.GetTypeFromHandle)` at the IL level. The JIT eliminates these at runtime, but the **linker must conservatively preserve them** because the T is open at trim time.

#### 2.5 Coarse Parent-Level Dependencies (1A..1L)

All 11 parent clusters form a single SCC. Strongest bidirectional couplings:

| Parent Pair | Total Edges | Primary Mechanism |
|------------|------------:|-------------------|
| 1D Emit <-> 1C Reflection | 123 | TypeBuilder/SymbolType <-> RuntimeType mutual |
| 1F Intrinsics <-> 1A Primitives | 115 | Vector ops -> ThrowHelper; HexConverter -> Vector128 |
| 1A Primitives <-> 1C Reflection | 113 | Enum/typeof -> RuntimeType; RuntimeType -> Span/Array |
| 1E Text <-> 1A Primitives | 58 | Encoding -> Span; String -> ValueStringBuilder |
| 1B Globalization <-> 1A Primitives | 39 | NumberFormatInfo <-> String/AppContext |
| 1E Text <-> 1C Reflection | 25 | Encoding -> Type.op_Equality (via generic code) |
| 1G Collections <-> 1C Reflection | 23 | Dictionary typeof(TKey).IsValueType |

#### 2.6 Critical SCC-Breaking Edges (Single-Edge Removals)

Only **4 out of 160 edges** are true bottleneck edges where removal reduces the SCC:

| Edge | Reduction | Method-Level Edges | Feasibility |
|------|-----------|--------------------|-------------|
| **1H-i ThreadPrimitives -> 1H-ii Synchronization** | 29→27 (-2) | 3: Lock -> WaitHandle/EventWaitHandle | MODERATE — Lock.CreateWaitEvent/SignalWaiter; could potentially use an interface indirection |
| **1A-ii CoreTypes -> 1J-ii SearchValues** | 29→28 (-1) | 6: String.MakeSeparatorList, MemoryExtensions.IndexOfAny -> SearchValues | LOW — hot path for String.Split and span search |
| **1J-ii SearchValues -> 1A-iii Infrastructure** | 29→28 (-1) | 6: SearchValues -> SpanHelpers.IndexOfAny/Contains | LOW — fundamental helper dependency |
| **1H-ii Synchronization -> 1L-ii SafeHandle** | 29→28 (-1) | 5: WaitHandle/EventWaitHandle -> SafeHandle ops | MODERATE — SafeHandle.DangerousAddRef/Release |

**Key insight**: The SCC is **extremely well-connected**. Removing single edges at the cluster level barely reduces it. The fundamental problem is that `Type.op_Equality` (called from 18 clusters via `typeof(T)` patterns) creates a hub that connects everything through 1C-i TypeSystem.

#### 2.7 Cluster Coupling Summary

| Cluster | Intra | OutCross | InCross | ExtOut | Total |
|---------|------:|--------:|-------:|------:|------:|
| 1C-i TypeSystem | 207 | 94 | 182 | 169 | 470 |
| 1A-ii CoreTypes | 70 | 114 | 163 | 144 | 328 |
| 1C-ii Members | 67 | 119 | 23 | 136 | 322 |
| 1E-ii Encoding | 90 | 43 | 37 | 112 | 245 |
| 1F-ii Vector | 103 | 47 | 118 | 90 | 240 |
| 1B-ii Formatting | 51 | 59 | 23 | 70 | 180 |
| 1A-iii Infrastructure | 31 | 83 | 90 | 45 | 159 |
| 1A-i Numeric | 51 | 42 | 25 | 65 | 158 |
| 1E-i StringBuilder | 28 | 25 | 36 | 56 | 109 |
| 1D-i TypeConstruction | 11 | 51 | 67 | 45 | 107 |

**Most coupled**: 1C-i TypeSystem (470 total call edges), 1A-ii CoreTypes (328), 1C-ii Members (322).
**Least coupled**: 1D-ii ILGeneration (8), 1J-iii BinaryPrimitives (7), 1L-ii SafeHandle (16), 1L-iii Marshalling (16), 1H-ii Synchronization (18).

#### 2.8 `#if TARGET_BROWSER` / `TARGET_WASI` Conditionals in SCC Files

71 conditional compilation lines across 25 files. Key clusters affected:

| File (lines) | Cluster | What Changes |
|-------------|---------|--------------|
| ThreadPoolWorkQueue.cs (8) | 1H-i | FEATURE_WASM_MANAGED_THREADS — entire ThreadPool dispatch strategy |
| ManualResetEventSlim.cs (6) | 1H-ii | TARGET_BROWSER — spin wait disabled on single-threaded WASM |
| TimeZoneInfo.Unix.NonAndroid.cs (6) | 1A-ii | TARGET_BROWSER — timezone file loading vs. embedded data |
| Thread.cs (5) | 1H-i | TARGET_BROWSER/WASI — thread creation, sleep behavior |
| Thread.CoreCLR.cs (4) | 1H-i | TARGET_BROWSER — starts in WASM context |
| Monitor.cs (4) | 1H-i | TARGET_BROWSER — Monitor disabled on ST browser |
| OperatingSystem.cs (4) | 1A-ii | TARGET_BROWSER/WASI — platform detection |
| FileStatus.Unix.cs (4) | 1I-ii | TARGET_BROWSER — file mode handling differs |
| Assembly.cs (4) | 1C-iii | TARGET_BROWSER — assembly loading restrictions |
| EventSource.cs (3) | 1M-ii | FEATURE_WASM_PERFTRACING — eventsource integration |
| CultureData.Icu.cs (2) | 1B-i | TARGET_BROWSER — ICU data loading |
| GlobalizationMode.cs (1) | 1B-i | TARGET_BROWSER/WASI — invariant globalization mode |

**Threading cluster (1H) has the most browser-conditional code** — 22 of 71 lines. This is expected: the browser WASM target can be single-threaded, which fundamentally changes ThreadPool, Monitor, Lock, and ManualResetEventSlim behavior.

#### 2.9 External Callees (Leaving the SCC)

Clusters with most external dependencies (calls to non-SCC methods):

| Cluster | Ext Calls | Top External Types |
|---------|----------:|-------------------|
| 1C-i TypeSystem | 169 | RuntimeType, SignatureType, RuntimeTypeHandle (self-recursive non-SCC) |
| 1A-ii CoreTypes | 144 | ReadOnlySpan, String, Unsafe, SpanHelpers, Object |
| 1C-ii Members | 136 | SignatureType, Type, ParameterInfo, TypeDelegator |
| 1E-ii Encoding | 112 | ThrowHelper, Encoding (self), String, Char |
| 1F-ii Vector | 90 | Unsafe (37!), Vector64 self-calls, Vector128 |

Notable: 1F-ii Vector has **37 external calls to Unsafe** (Unsafe.SizeOf, Unsafe.As, etc.) — these are JIT intrinsics and should not contribute to SCC growth.

#### 2.10 Key Findings & Implications for Phase 3

1. **The SCC cannot be split by removing small numbers of cluster-level edges.** The 29 sub-clusters form a single highly-connected SCC with 160 edges, and only 4 single-edge bottlenecks exist (each removing at most 2 clusters).

2. **`Type.op_Equality` is the universal glue.** 18/29 clusters call it via `typeof(T) ==` patterns. Any strategy to break the SCC must address this — either by:
   - Making the linker understand that `typeof(T) == typeof(X)` is a constant pattern (JIT already optimizes it)
   - Moving Type.op_Equality to a minimal "type identity" cluster that doesn't pull in full RuntimeType
   - Using feature switches to stub out Type.op_Equality for specific T instantiations

3. **Reflection.Emit (1D) is tightly coupled to TypeSystem (1C) with 123 bidirectional edges.** This is the second-biggest cycle. On browser/WASM, Reflection.Emit is rarely used (Blazor doesn't use it). If the linker could trim Emit away, it would remove 31 methods from the SCC.

4. **Intrinsics (1F) <-> Infrastructure (1A)** has 115 bidirectional edges but these are mostly HexConverter's vectorized implementations and ThrowHelper calls. HexConverter is used by AssemblyNameParser which is used by Reflection.

5. **Threading (1H) is the most browser-affected cluster** with 22 conditional compilation lines. The browser target fundamentally changes thread behavior, but the conditional code is already structured with `#if TARGET_BROWSER`.

6. **The real SCC-breaking strategy must work at the method level, not the cluster level.** The cluster graph is too well-connected. Phase 4 should focus on identifying specific method-level cycles that can be broken with feature switches, interface indirection, or lazy loading patterns.

#### 2.11 The `typeof(T)` Linker Optimization Opportunity

**Core question: Is this SCC "real" or an artifact of the linker's conservative analysis?**

The JIT eliminates `typeof(T) == typeof(int)` at runtime — for each concrete instantiation of `T`, the comparison becomes a constant `true`/`false`, and the dead branch is removed. This means at runtime, `Scalar<byte>.Add` never actually calls `Type.op_Equality` — the JIT inlines the constant and removes the type comparison entirely.

However, the **ILLinker cannot do this**. At trim time, it sees the IL:

```
ldtoken T                              // open generic parameter
call Type.GetTypeFromHandle(RuntimeTypeHandle)
ldtoken [System.Int32]
call Type.GetTypeFromHandle(RuntimeTypeHandle)
call Type.op_Equality(Type, Type)      // ← linker must keep this
brtrue.s LABEL
```

Because `T` is an open generic parameter, the linker cannot evaluate the comparison. It must conservatively preserve:
- `Type.GetTypeFromHandle` → pulls in `RuntimeType`
- `Type.op_Equality` → pulls in `RuntimeType.Equals` / `RuntimeTypeHandle` comparison
- All code reachable from both branches (since it can't eliminate either)

This is why 18/29 clusters call into `Type.op_Equality` — not because they genuinely depend on runtime type identity, but because the **idiomatic C# generic dispatch pattern** (`typeof(T) == typeof(X)`) compiles to calls the linker cannot fold.

**Three independent blockers in the ILLinker** (verified by code inspection):

1. **`ldtoken` is not a recognized constant** — `UnreachableBlocksOptimizer.IsConstantValue()` only recognizes `Ldc_*`, `Ldnull`, `Ldstr`. The `ldtoken` instruction is not in the list, so `GetArgumentsOnStack()` returns null and the optimizer never tries to evaluate the call chain.

2. **`Type.GetTypeFromHandle` is not an intrinsic** — `EvaluateIntrinsicCall()` only handles `String.op_Equality/Inequality/Concat`. No `System.Type` methods are recognized.

3. **`Type.op_Equality` is not an intrinsic** — Same as above; the method check is `DeclaringType.MetadataType == MetadataType.String` — Type is never matched.

4. **No generic instantiation context** — The optimizer works on `MethodDefinition` (uninstantiated), not specific instantiations. Even if it could evaluate `typeof(int) == typeof(int)`, it would need to process each instantiation separately. The cache is keyed by `MethodDefinition`, not `MethodReference`.

**What would fixing this require:**

| Change | Difficulty | Impact |
|--------|-----------|--------|
| Add `ldtoken` as constant-propagatable value | Medium | Enables the chain |
| Add `Type.GetTypeFromHandle` as intrinsic | Easy | Converts token → type identity |
| Add `Type.op_Equality/Inequality` as intrinsic | Easy | Compares two type identities |
| Per-instantiation optimization context | Hard | Required for open generic `T` |

The first three changes would immediately help code like `typeof(int) == typeof(byte)` (concrete types). But the real win requires change 4 — optimizing *per generic instantiation* — so that `Scalar<byte>.Add()` gets its own analysis where `typeof(T)` is known to be `typeof(byte)`.

**Impact estimate:** If the linker could fold all `typeof(T) == typeof(X)` patterns:
- 18/29 clusters would lose their coupling through `Type.op_Equality`
- Scalar\`1 (8,177B, 11 methods) might vanish entirely — every branch is guarded by `typeof(T) ==`
- Vector128/256/512 type-dispatch methods would slim drastically
- Dictionary/HashSet `typeof(TKey).IsValueType` checks would be folded
- NumberFormatInfo generic `TChar` dispatch would be resolved

This is potentially worth **20-40 KB** of IL savings (rough estimate: 20-40% of the 100 KB own IL) and could break the SCC into multiple disconnected components without any source-level changes to CoreLib.

**Comparison with existing substitution pattern:** The linker already folds `IntPtr.Size → 4/8`, `GlobalizationMode.Invariant → true/false`, `IsSupported → false` via XML substitution files. The `typeof(T)` pattern is conceptually similar — it's a compile-time constant that the linker doesn't currently recognize. The difference is that existing substitutions are simple method body replacements, while `typeof(T)` folding requires multi-instruction pattern matching and per-instantiation analysis.

**Existing analogy in NativeAOT:** The NativeAOT compiler (RyuJIT + ILC) already handles this — it compiles each generic instantiation separately with full type knowledge, so `typeof(T) == typeof(int)` is trivially constant-folded. The question is whether ILLink can approximate this for the shared generic code path.

**Recommendation:** This should be investigated as the **highest-leverage intervention** — it addresses the root cause rather than symptoms. Even a partial implementation (concrete types only, without per-instantiation context) would help. A full implementation would likely collapse the SCC dramatically without touching CoreLib source at all.

---

## Phase 3: Existing Trimming Infrastructure Audit [DONE]

Phase 2 showed the SCC is too well-connected for cluster-level cutting. Before proposing new cuts, we must understand what the linker already does and what mechanisms are available.

1. **Review ILLink substitution files** — `src/libraries/System.Private.CoreLib/src/ILLink/` — catalog all existing body substitutions [DONE]
2. **Review existing feature switches** — catalog which trimmer-friendly switches exist and which are already active for browser publishes [DONE]
3. **Check .csproj/.projitems** for browser-specific file inclusions/exclusions [DONE]
4. **Audit `#if` conditionals** for `TARGET_BROWSER`, `TARGET_WASI`, `FEATURE_WASM_MANAGED_THREADS` — comprehensive audit [DONE]
5. **Map the linker's current typeof(T) capabilities** — confirm the blockers identified in Phase 2 analysis [DONE]
6. **Review ILLink.Descriptors.Shared.xml** — what types/methods are force-rooted? [DONE]

### Phase 3 Results

#### 3.1 ILLink Substitution Files Catalog

**14 substitution XML files** exist across CoreLib, organized by platform/architecture:

| File | Scope | What It Stubs |
|------|-------|---------------|
| `ILLink.Substitutions.Shared.xml` | All platforms | EventSource.IsEnabled→false, GlobalizationMode.Invariant→true, PredefinedCulturesOnly→true, Task.s_asyncDebuggingEnabled→false |
| `ILLink.Substitutions.32bit.xml` | 32-bit targets | IntPtr.Size→4, UIntPtr.Size→4 |
| `ILLink.Substitutions.64bit.xml` | 64-bit targets | IntPtr.Size→8, UIntPtr.Size→8 |
| `ILLink.Substitutions.LittleEndian.xml` | LE targets | BitConverter.IsLittleEndian→true |
| `ILLink.Substitutions.NoX86Intrinsics.xml` | Non-x86 | ~60 x86 intrinsic IsSupported→false (SSE through AVX-512, Gfni, etc.) |
| `ILLink.Substitutions.NoArmIntrinsics.xml` | Non-ARM | ~20 ARM intrinsic IsSupported→false (AdvSimd, Sve, Crc32, etc.) |
| `ILLink.Substitutions.NoWasmIntrinsics.xml` | Non-WASM | PackedSimd.IsSupported→false |
| `ILLink.Substitutions.iOS.xml` | iOS | GlobalizationMode.Hybrid→true |
| **CoreCLR** `ILLink.Substitutions.xml` | CoreCLR | RuntimeFeature.IsDynamicCodeCompiled→true |
| **Mono WASM** `ILLink.Substitutions.wasm.xml` | Mono WASM | RuntimeFeature.IsDynamicCodeCompiled→false |
| **Mono iOS** `ILLink.Substitutions.iOS.xml` | Mono iOS | RuntimeFeature.IsDynamicCodeCompiled→false |
| **Mono** `ILLink.Substitutions.Intrinsics.x86.xml` | Mono non-x86 | All x86 intrinsics IsSupported→false |
| **Mono** `ILLink.Substitutions.Intrinsics.Vectors.xml` | Mono | Vector256/512.IsHardwareAccelerated→false |
| **Browser SIMD** `ILLink.Substitutions.WasmIntrinsics.xml` | Browser WASM w/ SIMD | Vector.IsHardwareAccelerated→true, Vector128.IsHardwareAccelerated→true, PackedSimd.IsSupported→true |
| **Browser no-SIMD** `ILLink.Substitutions.NoWasmIntrinsics.xml` (browser build dir) | Browser WASM w/o SIMD | Same 3 properties→false |

**Active substitutions for Browser WASM (published, non-Debug, SIMD enabled):**
- GlobalizationMode.Invariant→true (via InvariantGlobalization MSBuild property default)
- GlobalizationMode.PredefinedCulturesOnly→true
- EventSource.IsEnabled→false (via EventSourceSupport MSBuild property)
- RuntimeFeature.IsDynamicCodeCompiled→false (Mono WASM)
- Vector128.IsHardwareAccelerated→true, PackedSimd.IsSupported→true
- Vector.IsHardwareAccelerated→true
- Vector256.IsHardwareAccelerated→false, Vector512.IsHardwareAccelerated→false
- All x86 intrinsics IsSupported→false
- All ARM intrinsics IsSupported→false
- IntPtr.Size→4 (WASM is 32-bit)
- BitConverter.IsLittleEndian→true
- Task.s_asyncDebuggingEnabled→false (Debugger.IsSupported=false)

#### 3.2 Feature Switches Catalog

**`FeatureSwitchDefinition` attributes in CoreLib** (properties the linker can substitute):

| Feature Switch Name | Property | Default (Browser WASM) | Impact |
|---------------------|----------|----------------------|--------|
| `System.Diagnostics.Tracing.EventSource.IsSupported` | EventSource.IsSupported | **false** | Removes EventSource infrastructure |
| `System.Diagnostics.Debugger.IsSupported` | Debugger.IsSupported | **false** | Removes debugger attributes, async debug hooks |
| `System.Diagnostics.StackTrace.IsSupported` | StackTrace.IsSupported | true | Controls stack trace support |
| `System.Diagnostics.StackTrace.IsLineNumberSupported` | StackTrace.IsLineNumberSupported | **false** (conditional) | Line numbers in stack traces |
| `System.Diagnostics.Metrics.Meter.IsSupported` | EventSource.IsMeterSupported | **false** | Metrics/Meter support |
| `System.Globalization.Invariant` | GlobalizationMode.Invariant | **true** | Invariant globalization, trims ICU |
| `System.Globalization.PredefinedCulturesOnly` | GlobalizationMode.PredefinedCulturesOnly | **true** | Only built-in cultures |
| `System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeSupported` | RuntimeFeature.IsDynamicCodeSupported | true (via AppContext) | Dynamic code compilation |
| `System.Runtime.CompilerServices.RuntimeFeature.IsMultithreadingSupported` | RuntimeFeature.IsMultithreadingSupported | **false** (single-threaded) | Threading support |
| `System.Text.Encoding.EnableUnsafeUTF7Encoding` | LocalAppContextSwitches | **false** | UTF-7 encoding |
| `System.TimeZoneInfo.Invariant` | TimeZoneInfo invariant mode | false | Timezone data loading |
| `System.StartupHookProvider.IsSupported` | StartupHookProvider.IsSupported | true | Startup hooks |
| `System.Runtime.InteropServices.EnableConsumingManagedCodeFromNativeHosting` | ComponentActivator.IsSupported | false (mobile-like) | Native hosting |
| `System.Threading.Thread.EnableAutoreleasePool` | AutoreleasePool feature | false | Autorelease pool |
| `System.ComponentModel.DefaultValueAttribute.IsSupported` | DefaultValueAttribute.IsSupported | true | DefaultValue attribute |
| `System.Reflection.Metadata.MetadataUpdater.IsSupported` | MetadataUpdater.IsSupported | false (no hot reload in pub) | Hot reload support |

**Browser WASM effective feature switch state (published, non-Debug):**

| Switch | Value | How Set |
|--------|-------|---------|
| EventSource.IsSupported | false | `EventSourceSupport=false` (WasmApp.LocalBuild.targets) |
| Debugger.IsSupported | false | `DebuggerSupport=false` (WasmApp.LocalBuild.targets) |
| StackTrace.IsLineNumberSupported | false | `StackTraceLineNumberSupport=false` (conditional in Browser.targets) |
| Metrics.Meter.IsSupported | false | `MetricsSupport=false` (WasmApp.Common.targets) |
| Globalization.Invariant | true | `InvariantGlobalization` defaults to true for browser |
| PredefinedCulturesOnly | true | (implied by Invariant=true) |
| RuntimeFeature.IsMultithreadingSupported | false | RuntimeHostConfigurationOption when single-threaded |
| RuntimeFeature.IsDynamicCodeCompiled | false | ILLink.Substitutions.wasm.xml (Mono) |
| UseSystemResourceKeys | true | WasmApp.LocalBuild.targets |
| EnableUnsafeUTF7Encoding | false | WasmApp.LocalBuild.targets |
| HttpActivityPropagationSupport | false | WasmApp.LocalBuild.targets |

**Switches that remain TRUE for browser WASM:**
- RuntimeFeature.IsDynamicCodeSupported → true (Mono WASM supports dynamic code via interpreter)
- StackTrace.IsSupported → true
- TimeZoneInfo.Invariant → false (timezone data is available)
- StartupHookProvider.IsSupported → true (not disabled)

#### 3.3 Browser-Specific File Inclusions/Exclusions

**DefineConstants for browser/WASM builds:**

| Constant | When Defined | Where |
|----------|-------------|-------|
| `TARGET_BROWSER` | `TargetsBrowser=true` | System.Private.CoreLib.Shared.projitems |
| `TARGET_WASI` | `TargetsWasi=true` | System.Private.CoreLib.Shared.projitems |
| `TARGET_WASM` | `Platform=wasm` | Mono + CoreCLR .csproj files |
| `FEATURE_WASM_MANAGED_THREADS` | Browser/WASI + `WasmEnableThreads=true` | Mono .csproj only |
| `FEATURE_SINGLE_THREADED` | Browser/WASI + `WasmEnableThreads!=true` | Mono .csproj only |

**Derived properties:**

| Property | Value for Browser | Effect |
|----------|------------------|--------|
| `IsMobileLike` | true | Disables AssemblyDependencyResolver, cross-process mutex |
| `SupportsWasmIntrinsics` | true (Platform=wasm) | Enables WasmBase.cs, PackedSimd.cs |
| `UseMinimalGlobalizationData` | true | Minimal globalization data tables |
| `FeatureCrossProcessMutex` | false | No cross-process mutex support |
| `FeaturePortableTimer` | false (single-threaded) | Uses Browser-specific timer queue |
| `FeaturePortableThreadPool` | false (single-threaded) | Uses Browser-specific thread pool |

**Platform-specific source files for browser:**

| Category | Files Included | Notes |
|----------|---------------|-------|
| Browser-only | AppContext.Browser.cs, Environment.Browser.cs, DriveInfoInternal.Browser.cs, PersistedFiles.Browser.cs, RuntimeInformation.Browser.cs | Basic platform plumbing |
| Browser globalization | CultureData.Browser.cs | JS-based locale data (only with ICU) |
| Browser async | AsyncHelpers.Browser.cs | Browser-specific async helpers |
| Browser JS interop | Interop.Locale.CoreCLR.cs / .Mono.cs | JS locale queries |
| Browser timezone | Interop.GetTimeZoneData.Wasm.cs | Embedded TZ data |
| Browser threading (ST) | ThreadPool.Browser.cs, TimerQueue.Browser.cs, PreAllocatedOverlapped.Browser.cs, ThreadPoolBoundHandle.Browser.cs | Single-threaded stubs |
| Browser threading (MT) | ThreadPool.Browser.Threads.cs, ThreadPoolBoundHandle.Browser.Threads.cs, PortableThreadPool.Browser.Threads.cs | Multi-threaded WASM |
| WASM intrinsics | WasmBase.cs, PackedSimd.cs | Real implementations (not PlatformNotSupported) |
| Excluded for browser | RuntimeInformation.Unix.cs, Interop.OSReleaseFile.cs, RuntimeEventSource.cs (non-browser only), AssemblyDependencyResolver.cs | Replaced by browser variants or platform-not-supported stubs |

#### 3.4 `#if` Conditional Compilation Audit (Comprehensive)

**Total: ~74 conditional compilation directives** across SCC-relevant CoreLib files:

| Conditional | Occurrences | Files | Primary Pattern |
|-------------|:-:|:-:|----------------|
| `FEATURE_WASM_MANAGED_THREADS` | 31 | 11 | `[UnsupportedOSPlatform("browser")]` on blocking APIs; compile guards (#error) |
| `TARGET_BROWSER` | 16 | 10 | Platform detection, JS interop, file system stubs |
| `TARGET_WASI` | 10 | 7 | Similar to browser + WASI-specific poll/sleep |
| `TARGET_WASM` | 5 | 2 | Interlocked byte/ushort intrinsics, architecture detection |
| `FEATURE_SINGLE_THREADED` | 4 | 2 | Non-concurrent queue, IsMultithreadingSupported=false |

**By namespace (SCC-relevant):**

| Namespace | Occurrences | Key Patterns |
|-----------|:-:|-------------|
| System.Threading | 46 | Blocking API unsupported attrs, compile guards, worker limits, queue types |
| System | 12 | Timezone embedded DB, GUID randomness, OS detection, base directory |
| System.Globalization | 4 | JS interop for display names, invariant mode detection |
| System.Runtime.CompilerServices | 3 | Multithreading and dynamic code feature switches |
| System.Runtime.InteropServices | 3 | OS description, processor architecture |
| System.Reflection | 2 | Skip File.Exists on embedded platforms (LoadFile/LoadFrom) |

**Key patterns in conditional code:**

1. **`[UnsupportedOSPlatform("browser")]` guards** (~30 occurrences) — Applied via `!FEATURE_WASM_MANAGED_THREADS` on: Monitor.Wait, ManualResetEventSlim.Wait, Thread.Start, RegisteredWaitHandle, all RegisterWaitForSingleObject overloads. These are **attributes only** — the methods still exist, they just warn when called from browser-targeted code.

2. **`#error` compile guards** (4 occurrences) — TimerQueue.Browser.cs, TimerQueue.Wasi.cs, ThreadPool.Browser.cs, ThreadPool.Wasi.cs all `#error` when `FEATURE_WASM_MANAGED_THREADS` is defined, forcing use of Portable implementations.

3. **Platform detection constants** (6 occurrences) — `OperatingSystem.IsBrowser()`, `IsWasi()`, `OSDescription`, `ProcessArchitecture`, `OSPlatformName`.

4. **Embedded timezone database** (6 occurrences in TimeZoneInfo) — Browser/WASI loads TZ data via `Interop.Sys.GetTimeZoneData` from embedded resources instead of filesystem.

5. **Interlocked intrinsics** (4 occurrences) — `TARGET_WASM` enables byte/ushort Exchange/CompareExchange as direct Mono WASM intrinsics.

6. **Single-threaded fallbacks** (4 occurrences) — `FEATURE_SINGLE_THREADED` uses non-concurrent `Queue<object>` in ThreadPoolWorkQueue and disables IsMultithreadingSupported.

**Important finding**: Most `TARGET_BROWSER` conditionals are at the **attribute level** (unsupported platform warnings) or in **platform-specific file variants** (already in separate .cs files chosen by csproj). Very few inline `#if TARGET_BROWSER` blocks exist in the shared SCC-relevant code. The threading area has the most, but they're primarily `[UnsupportedOSPlatform]` attribute conditionals, not code path conditionals.

#### 3.5 ILLink.Descriptors — Force-Rooted Types

**Types/methods force-preserved by descriptor files:**

| Type | Methods | Why | When |
|------|---------|-----|------|
| ThreadPoolBoundHandle | .ctor | Interface impl workaround | Always |
| ComponentActivator | GetFunctionPointer | Error experience for native hosting | Always |
| ComponentActivator | LoadAssembly, LoadAssemblyBytes, LoadAssemblyAndGetFunctionPointer | Native hosting entry points | When EnableConsumingManagedCodeFromNativeHosting=true |
| Task | ParentForDebugger, GetDelegateContinuationsForDebugger, SetNotificationForWaitCompletion | VS debugger | When Debugger.IsSupported=true (default) |
| TaskScheduler | GetScheduledTasksForDebugger, GetTaskSchedulersForDebugger | VS debugger | When Debugger.IsSupported=true |
| AsyncMethodBuilderCore | TryGetStateMachineForDebugger | Debugger | When Debugger.IsSupported=true |
| Async*MethodBuilder (6 types) | ObjectIdForDebugger, SetNotificationForWaitCompletion | Debugger | When Debugger.IsSupported=true |
| ThreadBlockingInfo | LockOwnerManagedThreadId | Debugger | When Debugger.IsSupported=true |
| Task | GetActiveTaskFromId | VS Tasks Window | When Debugger.IsSupported=true |
| MetadataUpdater | GetCapabilities | Hot reload | When Debugger.IsSupported=true |
| Utf8StringMarshaller.ManagedToUnmanagedIn | FromManaged, ToUnmanaged, Free | GitHub issue #71847 | When Debugger.IsSupported=true |
| EventSource | InitializeDefaultEventSources | Event initialization | When EventSource.IsSupported=true |
| Thread (WASI-only) | RegisterWasiPollableHandle, RegisterWasiPollHook, PollWasiEventLoopUntil* | WASI poll hooks (accessed via UnsafeAccessor) | WASI only |

**For browser WASM (published, DebuggerSupport=false)**:
- Most debugger-rooted methods are **NOT preserved** (Debugger.IsSupported=false)
- EventSource.InitializeDefaultEventSources is **NOT preserved** (EventSource.IsSupported=false)
- ComponentActivator.GetFunctionPointer **IS preserved** (always)
- ThreadPoolBoundHandle .ctor **IS preserved** (always)
- Utf8StringMarshaller methods are **NOT preserved** (gated on Debugger.IsSupported)

**Link attributes (attribute removal):**
- When Debugger.IsSupported=false: removes DebuggableAttribute, DebuggerBrowsable, DebuggerDisplay, DebuggerHidden, DebuggerNonUserCode, DebuggerStepperBoundary, DebuggerStepThrough, DebuggerTypeProxy, DebuggerVisualizer
- When MetadataUpdater.IsSupported=false: removes MetadataUpdateHandlerAttribute
- When EventSource.IsSupported=false: removes EventSource/EventAttribute/EventData/EventField/EventIgnore/NonEvent
- When COM.IsSupported=false: removes ClassInterface, ComDefaultInterface, ComEventInterface, ComSourceInterfaces, ComVisible, DispId, InterfaceType, ProgId
- Always: removes TypeMapAttribute\`1, TypeMapAssociationAttribute\`1, TypeMapAssemblyTargetAttribute\`1

#### 3.6 ILLink typeof(T) Optimizer Capabilities — Confirmed Blockers

**Source code inspection of `src/tools/illink/src/linker/Linker.Steps/UnreachableBlocksOptimizer.cs` confirms all blockers:**

| Blocker | Status | Evidence |
|---------|--------|----------|
| **`ldtoken` not recognized as constant** | **CONFIRMED** | `IsConstantValue()` (line ~388-410) only recognizes `Ldc_*`, `Ldnull`, `Ldstr`. `Code.Ldtoken` is absent. |
| **`Type.GetTypeFromHandle` not an intrinsic** | **CONFIRMED** | `EvaluateIntrinsicCall()` (line ~326-362) only handles `System.String` methods (op_Equality, op_Inequality, Concat). No `System.Type` methods. |
| **`Type.op_Equality` not an intrinsic** | **CONFIRMED** | Same code — only String's operators are handled. |
| **No per-generic-instantiation context** | **CONFIRMED** | Cache is `Dictionary<MethodDefinition, MethodResult?>` (line ~25). Optimizer processes `MethodDefinition` not `MethodReference`. Generic parameters remain unresolved tokens on the analysis stack. |

**Interesting nuance**: The internal `ConstantExpressionMethodAnalyzer.Analyze()` (line ~1767) **does** push `ldtoken` instructions onto the analysis stack. However, when these reach a `Call` to `Type.GetTypeFromHandle`, the analyzer falls through to `TryGetMethodCallResult` which resolves the callee to its `MethodDefinition` and tries to analyze its body — which fails since `GetTypeFromHandle` is a runtime intrinsic with no analyzable IL body.

**Substitution value types supported**: The `CodeRewriterStep.CreateConstantResultInstruction` only supports bool, int, long, float, double, string, and null. **Type objects cannot be substitution values.**

**What a fix would require (ordered by difficulty):**

1. **Easy**: Add `Type.GetTypeFromHandle` as intrinsic — fold `ldtoken X` + `call GetTypeFromHandle` into a known Type constant (when X is a concrete TypeReference, not a GenericParameter)
2. **Easy**: Add `Type.op_Equality/Inequality` as intrinsic — compare two Type constants and produce `ldc.i4.0/1`
3. **Medium**: Add `ldtoken` of concrete types as constant-propagatable value in `IsConstantValue()` — enables the call chain
4. **Hard**: Per-instantiation optimization context — required for open generic `T` (e.g., `typeof(T)` in `Scalar<byte>`)

Changes 1-3 would immediately help code like `typeof(int) == typeof(byte)` (concrete types). But the real SCC-breaking win requires change 4 — optimizing each generic instantiation separately. This is fundamental because `typeof(T)` in `Scalar<T>.Add()` is only constant when we know T=byte.

**Comparison with other optimizer patterns:**
- IntPtr.Size→4/8, GlobalizationMode.Invariant→true/false use simple method body replacement (substitution XML)
- IsHardwareAccelerated→true/false, IsSupported→false same pattern
- These are all method-level: the linker replaces the entire method body with `return <constant>`
- The `typeof(T)` pattern requires **instruction-level** analysis within a method body — a qualitatively different optimization

#### 3.7 Key Findings & Implications for Later Phases

1. **The existing trimming infrastructure is mature and well-structured.** 16+ feature switches, 14 substitution files, platform-specific file selection via csproj conditions, and attribute-level rooting create a layered system.

2. **Browser WASM already gets aggressive trimming** — EventSource, Debugger, Metrics are all disabled. Globalization is invariant. Vector256/512, all x86/ARM intrinsics are stubbed false. Only Vector128 + PackedSimd are enabled.

3. **Remaining optimization gaps for browser WASM:**
   - `RuntimeFeature.IsDynamicCodeSupported` remains **true** — Reflection.Emit stays reachable. Setting this to false could allow trimming Emit types, but Blazor **uses** DynamicMethod (via Linq.Expressions), so this can't be turned off universally.
   - `StackTrace.IsSupported` remains **true** — keeps StackTrace→Reflection coupling alive.
   - `StartupHookProvider.IsSupported` remains **true** — could be disabled for published apps.
   - No feature switch exists for Reflection.Emit specifically (separate from DynamicCode).
   - No feature switch exists for DefaultBinder (pulled in by Reflection).
   - No typeof(T) folding — the #1 SCC coupling mechanism is completely unaddressed.

4. **The `#if TARGET_BROWSER` conditionals are mostly well-structured** — platform variants use separate source files (chosen by csproj), and inline conditionals are primarily attribute-level guards, not major code path changes. **No opportunities for additional compile-time trimming** were found that aren't already covered.

5. **The ILLink typeof(T) gap is the single biggest untapped optimization.** 18/29 SCC sub-clusters are connected through `typeof(T) ==` patterns that the linker cannot fold. This affects:
   - Scalar\`1 (8,177B, 11 methods) — every branch is `typeof(T) == typeof(X)`
   - Vector128/256/512 type-dispatch methods
   - Dictionary/HashSet `typeof(TKey).IsValueType` checks
   - NumberFormatInfo generic TChar dispatch
   - Enum formatting/parsing generic helpers
   - HexConverter and other generic utility methods

6. **A new feature switch for Reflection.Emit** (Strategy B from Phase 5) could be the most practical near-term win. Unlike typeof(T) folding (which requires hard ILLink work), adding `System.Reflection.Emit.IsSupported=false` could:
   - Stub TypeBuilder/ILGenerator/DynamicMethod constructors
   - Break 123 bidirectional edges between 1D (Emit) and 1C (Reflection)
   - Remove ~5.4 KB of IL from the SCC
   - However: requires confirming Blazor doesn't need it (it does use DynamicMethod via Linq.Expressions)

7. **The force-rooted types are minimal for browser WASM** — with Debugger.IsSupported=false and EventSource.IsSupported=false, most descriptor-rooted methods are already eliminated. Only ComponentActivator.GetFunctionPointer and ThreadPoolBoundHandle..ctor remain unconditionally rooted.

---

## Phase 4: Method-Level SCC Analysis

Phase 2 proved cluster-level Tarjan is useless (single SCC). We need method-level cycle analysis.

### 4A. Get Full Method-Level Call Graph

Re-run the method-cost tool with higher callee limit (topCallees truncated to 1-5 is insufficient):
- Option A: Re-run with `n=10000` or similar to get all callees per method
- Option B: Extract edges from ILLink's linker output
- Option C: Parse the msbuild.binlog or ILLink dependency trace

### 4B. Method-Level Tarjan SCC

Run Tarjan on the full method-level graph for the 942 SCC methods:
- Find actual strongly connected components (the method-cost tool's SCC may be an overestimate if topCallees is truncated)
- Identify which methods participate in real cycles vs. just transitive reachability
- Find articulation edges: specific method-to-method calls whose removal breaks the SCC

### 4C. Validate/Refute Coupling Theories

Using the full call graph, validate the 30 theories from Phase 2. For each:
- Does the chain actually exist in the trimmed assembly?
- What is the actual IL weight of the chain?
- Is there a single bottleneck method in the chain that could be cut?

### Coupling Theories (Carried from earlier analysis)

**Already-known coupling chains:**
1. Exception.ToString() -> StackTrace -> Reflection
2. RuntimeType -> Reflection.Emit (dynamic invocation)
3. CultureInfo <-> Number formatting <-> all numeric primitives
4. Thread/ThreadPool -> Task -> async builders
5. String <-> CompareInfo <-> CultureInfo
6. SafeFileHandle -> ThreadPool (async IO completion)
7. Array.Sort -> Comparer -> generic interface dispatch
8. Type.GetType() -> AssemblyLoadContext -> Assembly -> Reflection

**Theories to investigate:**
9. Enum.ToString() -> RuntimeType -> Reflection (reflection to get names)
10. DefaultBinder -> RuntimeType -> all of Reflection
11. Convert class -> every numeric type + DateTime + String
12. DateTime.ToString() -> DateTimeFormat -> CultureInfo -> CalendarData -> ALL calendars
13. Scalar\`1 (8,177B) <-> all numeric types (generic SIMD scalar bridges)
14. StringBuilder.AppendFormat -> IFormattable -> all formattable types
15. Encoding.GetEncoding -> all Encoding subclasses
16. Stream virtual methods -> FileStream -> FileSystem -> Interop -> SafeHandle
17. AssemblyLoadContext -> NativeLibrary -> Marshal
18. DynamicMethod -> RuntimeILGenerator -> SignatureHelper -> RuntimeType
19. ThrowHelper -> every exception type -> Exception -> StackTrace
20. CalendricalCalculationsHelper (3,059B) -> DateTimeFormatInfo
21. CompareInfo -> Ordinal/OrdinalCasing -> Char -> Unicode tables
22. ConcurrentDictionary -> Lock/Monitor & EqualityComparer
23. GC -> Thread -> ThreadPool -> Timer
24. MetadataReader (external, 2,822B) -> Reflection.Metadata -> Reflection.Emit
25. SerializationInfo -> RuntimeType -> activator
26. FieldAccessor -> Reflection.Emit (InvokerEmitUtil)
27. Resource loading -> Assembly.GetManifestResourceStream -> Stream
28. ~~Random -> Interop.Sys (Unix random)~~ — out of scope (native interop)
29. TimeZoneInfo (14,512B) -> IO (file reading) + Globalization
30. IO.Enumeration -> PathInternal -> String operations -> MemoryExtensions

---

## Phase 5: Propose Actionable Strategies [REVISED — was Phase 5+6]

Narrowed to 4 focused strategies based on Phase 2 findings:

### Strategy A: Linker `typeof(T)` Pattern Recognition (Highest Impact)

Teach ILLink to constant-fold `typeof(T) == typeof(X)` patterns. This would eliminate the #1 SCC coupling mechanism (18/29 clusters). See detailed analysis in **section 2.11**.

### Strategy B: Reflection.Emit Feature Switch

Add a feature switch to stub out Reflection.Emit on browser/WASM when not needed. Would decouple 1D (Emit) from 1C (Reflection) — 123 bidirectional edges, ~5.4 KB IL (1D-i + 1D-ii + 1D-iii).

### Strategy C: HexConverter → Vector Decoupling

Break the HexConverter → Vector128 chain (64 edges from Infrastructure → Vector). HexConverter is pulled in by AssemblyNameParser → Reflection. Could provide a scalar fallback gated on the linker.

### Strategy D: Method-Level Targeted Cuts

Using method-level Tarjan results from Phase 4B, identify specific methods where interface indirection, lazy loading, or feature switches break actual method-level cycles. Target the 4 bottleneck edges found in Phase 2 plus any new ones from full method-level analysis.

---

## Phase 6: Implement & Validate

1. **Prototype** top strategies (starting with highest impact-to-effort ratio)
2. **Re-run method-cost** on modified builds to confirm SCC breakage
3. **Measure published Blazor WASM app size** before/after
4. **Run library test suites** for affected areas to catch regressions
5. **Iterate** — if a cut doesn't break the SCC as expected, investigate why and adjust

---

## Execution Strategy

- Phase 0: Run method-cost on browser sample + Blazor app, compare [DONE]
- Phase 1: Single pass, categorize from method-cost JSON + source file mapping [DONE]
- Phase 2: Cross-cluster dependency analysis, Tarjan SCC, typeof(T) discovery [DONE]
- Phase 3: Audit existing ILLink substitutions, feature switches, browser conditionals
- Phase 4: Method-level Tarjan with full call graph, validate coupling theories
- Phase 5: Propose 4 focused strategies (typeof(T) linker opt, Emit switch, HexConverter decouple, method-level cuts)
- Phase 6: Implement top strategies + validate with builds and tests

---

# Opportunities
 - `StackTrace.IsSupported` remains **true** — keeps StackTrace→Reflection coupling alive.
 - `StartupHookProvider.IsSupported` remains **true** — could be disabled for published apps.
 - cut `HexConverter` -> `Vector128` https://github.com/dotnet/runtime/pull/125040
 - `SupportsWasmIntrinsics`
 - `ILLinkEqT` - typeof(T) Linker Optimization
 - `System.Reflection.Emit.IsSupported` - https://gist.github.com/pavelsavara/54d2776c5479642f02654d2b3a8afa85