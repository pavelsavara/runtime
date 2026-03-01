#!/usr/bin/env python3
"""Phase 2: Cross-cluster dependency analysis from topCallees call graph data."""
import json
from collections import defaultdict

with open('method-cost-full.json', 'r') as f:
    data = json.load(f)

methods = data['methods']
SCC_SIZE = 163058

# Build lookup: method name -> method record
method_lookup = {}
for m in methods:
    method_lookup[m['name']] = m

scc = [m for m in methods if m['transitiveSize'] == SCC_SIZE]
scc_names = {m['name'] for m in scc}

# --- classify() function (copied from analyze_scc.py) ---
def classify(type_name):
    t = type_name
    if 'System.Runtime.Intrinsics' in t:
        if 'Scalar' in t: return '1F-i-Scalar'
        return '1F-ii-Vector'
    if 'System.Numerics' in t:
        return '1F-iii-GenericNumerics'
    if 'System.Reflection.Emit' in t:
        if 'TypeBuilder' in t or 'EnumBuilder' in t or 'GenericTypeParameter' in t:
            return '1D-i-TypeConstruction'
        if 'ILGenerator' in t or 'DynamicMethod' in t or 'DynamicIL' in t or 'DynamicResolver' in t or 'DynamicScope' in t:
            return '1D-ii-ILGeneration'
        return '1D-iii-EmitSupport'
    if 'System.Reflection' in t:
        if 'Metadata' in t: return '1C-iv-Metadata'
        if 'RuntimeType' in t or 'Type' == t.split('.')[-1] or 'TypeHandle' in t or 'RuntimeTypeCache' in t or 'MemberInfoCache' in t:
            return '1C-i-TypeSystem'
        if 'Method' in t or 'Field' in t or 'Property' in t or 'Constructor' in t or 'Invoker' in t or 'CustomAttribute' in t or 'Binder' in t or 'ParameterInfo' in t:
            return '1C-ii-Members'
        if 'Assembly' in t or 'Module' in t or 'AssemblyName' in t:
            return '1C-iii-Assembly'
        return '1C-ii-Members'
    if 'System.Globalization' in t:
        if 'CultureInfo' in t or 'CultureData' in t or 'GlobalizationMode' in t:
            return '1B-i-CultureInfra'
        return '1B-ii-Formatting'
    if 'System.Text' in t and 'Json' not in t:
        if 'StringBuilder' in t or 'ValueStringBuilder' in t or 'InterpolatedStringHandler' in t:
            return '1E-i-StringBuilder'
        if 'Encoding' in t or 'Encoder' in t or 'Decoder' in t or 'Fallback' in t:
            return '1E-ii-Encoding'
        return '1E-iii-Unicode'
    if 'System.Threading' in t:
        if 'Task' in t or 'ValueTask' in t or 'Awaiter' in t or 'AsyncMethodBuilder' in t:
            return '1H-iii-Async'
        if 'Thread' == t.split('.')[-1] or 'ThreadPool' in t or 'Monitor' in t or 'Lock' in t:
            return '1H-i-ThreadPrimitives'
        if 'SemaphoreSlim' in t or 'WaitHandle' in t or 'ManualResetEvent' in t or 'CancellationToken' in t or 'Timer' in t:
            return '1H-ii-Synchronization'
        return '1H-i-ThreadPrimitives'
    if 'System.Collections' in t:
        if 'Dictionary' in t or 'Hashtable' in t or 'HashSet' in t:
            return '1G-i-Dictionary'
        if 'Comparer' in t or 'EqualityComparer' in t or 'NonRandomized' in t:
            return '1G-ii-Comparer'
        if 'List' in t or 'Queue' in t or 'ReadOnly' in t or 'ValueListBuilder' in t:
            return '1G-iii-Lists'
        return '1G-i-Dictionary'
    if 'System.Buffers' in t:
        if 'ArrayPool' in t or 'SharedArrayPool' in t:
            return '1J-i-ArrayPool'
        if 'SearchValues' in t or 'IndexOfAny' in t or 'ProbabilisticMap' in t or 'AsciiChar' in t:
            return '1J-ii-SearchValues'
        return '1J-iii-BinaryPrimitives'
    if 'System.Runtime.CompilerServices' in t:
        if 'RuntimeHelpers' in t or 'CastHelpers' in t or 'CastCache' in t or 'MethodTable' in t or 'TypeHandle' in t:
            return '1K-i-RuntimeHelpers'
        if 'AsyncTaskMethodBuilder' in t or 'AsyncValueTaskMethodBuilder' in t or 'Pooling' in t:
            return '1K-ii-AsyncBuilders'
        return '1K-iii-Other'
    if 'System.Runtime.InteropServices' in t:
        if 'Marshal' in t and 'Marshalling' not in t: return '1L-i-Marshal'
        if 'SafeHandle' in t or 'GCHandle' in t or 'NativeMemory' in t or 'NativeLibrary' in t:
            return '1L-ii-SafeHandle'
        if 'Marshalling' in t: return '1L-iii-Marshalling'
        return '1L-i-Marshal'
    if 'Microsoft.Win32.SafeHandles' in t:
        return '1L-ii-SafeHandle'
    if 'System.IO' in t:
        if 'Stream' in t or 'BinaryReader' in t or 'MemoryStream' in t or 'UnmanagedMemory' in t:
            return '1I-i-Streams'
        if 'File' in t or 'Directory' in t or 'Path' in t:
            return '1I-ii-FileSystem'
        return '1I-i-Streams'
    if 'System.Diagnostics' in t:
        if 'StackTrace' in t or 'StackFrame' in t: return '1M-i-StackTrace'
        return '1M-ii-EventSource'
    if 'ResourceManager' in t or 'ResourceReader' in t:
        return '1N-i-Resources'
    if 'Serialization' in t:
        return '1N-ii-Serialization'
    reflection_in_system = {'RuntimeType', 'Type', 'RuntimeTypeHandle', 'RuntimeMethodHandle',
                            'RuntimeFieldHandle', 'DefaultBinder', 'Activator', 'SignatureType',
                            'SignatureConstructedGenericType', 'SignatureArrayType', 'SignaturePointerType',
                            'SignatureByRefType', 'SignatureHasElementType'}
    type_base = t.split('.')[-1].split('/')[0].split('`')[0]
    top_type = t.split('/')[0].split('.')[-1].split('`')[0] if '/' in t else type_base
    if t.startswith('System.') and (type_base in reflection_in_system or top_type in reflection_in_system):
        if type_base in ('RuntimeType', 'Type', 'RuntimeTypeHandle', 'SignatureType',
                         'SignatureConstructedGenericType', 'SignatureArrayType', 'SignaturePointerType',
                         'SignatureByRefType', 'SignatureHasElementType'):
            return '1C-i-TypeSystem'
        if type_base == 'DefaultBinder': return '1C-ii-Members'
        if type_base == 'Activator': return '1C-ii-Members'
        return '1C-i-TypeSystem'
    if t.startswith('System.') and '.' not in t[7:].replace('`1','').replace('`2',''):
        name = t.split('/')[-1].split('`')[0]
        type_base_simple = t.split('.')[-1].split('/')[0].split('`')[0]
        numeric_types = {'Int16','Int32','Int64','Int128','UInt16','UInt32','UInt64','UInt128',
                         'Single','Double','Half','Decimal','Byte','SByte','Number','Convert',
                         'Math','MathF','BitConverter','Boolean','Char','IntPtr','UIntPtr','NFloat'}
        if type_base_simple in numeric_types or 'Number' in t:
            return '1A-i-Numeric'
        string_types = {'String','Enum','Array','Object','ValueType','Delegate','MulticastDelegate',
                        'Guid','DateTime','TimeSpan','TimeZoneInfo','DateOnly','TimeOnly','DateTimeOffset',
                        'Range','Index','Attribute','Version','Uri','Console','Environment',
                        'TypedReference','ArgIterator','Tuple','ValueTuple','Span','ReadOnlySpan',
                        'Memory','ReadOnlyMemory','MemoryExtensions'}
        if type_base_simple in string_types:
            return '1A-ii-CoreTypes'
        infra_types = {'ThrowHelper','SR','Buffer','SpanHelpers','HashCode','Marvin','Random',
                       'Exception','SystemException','ArgumentException','ArgumentNullException',
                       'FormatException','OverflowException','GC','WeakReference','Lazy',
                       'EventArgs','EventHandler','Action','Func','Predicate','ParamsArray',
                       'HexConverter','ConsoleEncoding','MetadataImport'}
        if type_base_simple in infra_types:
            return '1A-iii-Infrastructure'
        return '1A-ii-CoreTypes'
    if 'RuntimeHelpers' in t or 'CastHelpers' in t:
        return '1K-i-RuntimeHelpers'
    if t == 'System.SR' or t == 'Interop':
        return '1A-iii-Infrastructure'
    if t.startswith('Interop'):
        return '1L-i-Marshal'
    if 'AssemblyLoadContext' in t or 'System.Runtime.Loader' in t:
        return '1C-iii-Assembly'
    return 'UNKNOWN'

# --- Build SCC method to cluster mapping ---
method_cluster = {}
for m in scc:
    method_cluster[m['name']] = classify(m['type'])

# --- Build cross-cluster dependency matrix from topCallees ---
# Edge: (caller_cluster, callee_cluster) -> list of (caller_method, callee_method)
cross_edges = defaultdict(list)
intra_edges = defaultdict(int)
external_callees = defaultdict(list)  # callee not in SCC

for m in scc:
    caller_cluster = method_cluster[m['name']]
    for callee in m.get('topCallees', []):
        callee_name = callee['name']
        if callee_name in scc_names:
            # Get callee's type from its method record
            callee_rec = method_lookup.get(callee_name)
            if callee_rec:
                callee_cluster = classify(callee_rec['type'])
            else:
                callee_cluster = 'UNKNOWN'
            if caller_cluster != callee_cluster:
                cross_edges[(caller_cluster, callee_cluster)].append(
                    (m['name'], callee_name))
            else:
                intra_edges[caller_cluster] += 1
        else:
            external_callees[caller_cluster].append((m['name'], callee_name))

# --- Print Cross-Cluster Dependency Matrix ---
all_clusters = sorted(set(method_cluster.values()))
print("=" * 120)
print("CROSS-CLUSTER DEPENDENCY MATRIX (SCC-internal edges only)")
print("Rows = caller cluster, Cols = callee cluster, Cell = number of edges")
print("=" * 120)

# Header
label = 'From / To'
header = f"{label:<25}"
# Use short labels
short = {c: c.split('-', 2)[-1][:8] for c in all_clusters}
for c in all_clusters:
    header += f" {short[c]:>8}"
print(header)
print("-" * 120)

for src in all_clusters:
    row = f"  {src:<23}"
    for dst in all_clusters:
        if src == dst:
            row += f" {'['+str(intra_edges.get(src,0))+']':>8}"
        else:
            count = len(cross_edges.get((src, dst), []))
            if count > 0:
                row += f" {count:>8}"
            else:
                row += f" {'·':>8}"
    print(row)
print()

# --- Print top cross-cluster edges by count ---
print("=" * 120)
print("TOP CROSS-CLUSTER EDGES (by edge count)")
print("=" * 120)
edge_counts = [(k, v) for k, v in cross_edges.items()]
edge_counts.sort(key=lambda x: -len(x[1]))
for (src, dst), edges in edge_counts[:40]:
    print(f"\n  {src} -> {dst}  ({len(edges)} edges)")
    # Show first 5 examples
    for caller, callee in edges[:5]:
        # Shorten method names for display
        c1 = caller.split('::')[-1][:50] if '::' in caller else caller[:50]
        c2 = callee.split('::')[-1][:50] if '::' in callee else callee[:50]
        t1 = caller.split('::')[0].split('.')[-1] if '::' in caller else caller
        t2 = callee.split('::')[0].split('.')[-1] if '::' in callee else callee
        print(f"    {t1}::{c1}")
        print(f"      -> {t2}::{c2}")
    if len(edges) > 5:
        print(f"    ... and {len(edges)-5} more")

# --- Cluster in/out degree summary ---
print()
print("=" * 120)
print("CLUSTER COUPLING SUMMARY")
print("=" * 120)
print(f"  {'Cluster':<25} {'Intra':>6} {'OutCross':>9} {'InCross':>8} {'ExtOut':>7} {'Total':>6}")
print("-" * 80)

for c in all_clusters:
    intra = intra_edges.get(c, 0)
    out_cross = sum(len(v) for (s,d), v in cross_edges.items() if s == c)
    in_cross = sum(len(v) for (s,d), v in cross_edges.items() if d == c)
    ext_out = len(external_callees.get(c, []))
    total_out = intra + out_cross + ext_out
    print(f"  {c:<25} {intra:>6} {out_cross:>9} {in_cross:>8} {ext_out:>7} {total_out:>6}")

# --- Identify which clusters are most coupled ---
print()
print("=" * 120)
print("BIDIRECTIONAL COUPLING (clusters that call EACH OTHER)")
print("=" * 120)
seen = set()
bidir = []
for (s, d) in cross_edges:
    if (d, s) in cross_edges and (min(s,d), max(s,d)) not in seen:
        seen.add((min(s,d), max(s,d)))
        fwd = len(cross_edges[(s,d)])
        bwd = len(cross_edges[(d,s)])
        bidir.append((s, d, fwd, bwd, fwd+bwd))
bidir.sort(key=lambda x: -x[4])
for s, d, fwd, bwd, total in bidir:
    print(f"  {s} <-> {d}: {fwd} + {bwd} = {total} edges")
    # Show example edges in each direction
    for caller, callee in cross_edges[(s,d)][:2]:
        t1 = caller.split('::')[0].split('.')[-1]
        m1 = caller.split('::')[-1][:40]
        t2 = callee.split('::')[0].split('.')[-1]
        m2 = callee.split('::')[-1][:40]
        print(f"    -> {t1}::{m1} -> {t2}::{m2}")
    for caller, callee in cross_edges[(d,s)][:2]:
        t1 = caller.split('::')[0].split('.')[-1]
        m1 = caller.split('::')[-1][:40]
        t2 = callee.split('::')[0].split('.')[-1]
        m2 = callee.split('::')[-1][:40]
        print(f"    <- {t1}::{m1} -> {t2}::{m2}")
    print()

# --- External callee summary (calls leaving SCC) ---
print("=" * 120)
print("EXTERNAL CALLEES PER CLUSTER (calls to non-SCC methods)")
print("=" * 120)
for c in all_clusters:
    ext = external_callees.get(c, [])
    if ext:
        print(f"\n  {c}: {len(ext)} external calls")
        # Summarize by callee type
        by_type = defaultdict(int)
        for caller, callee in ext:
            ctype = callee.split('::')[0] if '::' in callee else callee
            by_type[ctype] += 1
        for t, cnt in sorted(by_type.items(), key=lambda x: -x[1])[:5]:
            print(f"    {t}: {cnt}")
