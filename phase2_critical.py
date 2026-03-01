#!/usr/bin/env python3
"""Detailed analysis of the 4 critical SCC-breaking edges and combination cuts."""
import json
from collections import defaultdict

with open('method-cost-full.json', 'r') as f:
    data = json.load(f)

methods = data['methods']
SCC_SIZE = 163058
scc = [m for m in methods if m['transitiveSize'] == SCC_SIZE]
scc_names = {m['name'] for m in scc}
method_lookup = {m['name']: m for m in methods}

def classify(t):
    if 'System.Runtime.Intrinsics' in t:
        if 'Scalar' in t: return '1F-i-Scalar'
        return '1F-ii-Vector'
    if 'System.Numerics' in t: return '1F-iii-GenericNumerics'
    if 'System.Reflection.Emit' in t:
        if 'TypeBuilder' in t or 'EnumBuilder' in t or 'GenericTypeParameter' in t: return '1D-i-TypeConstruction'
        if 'ILGenerator' in t or 'DynamicMethod' in t or 'DynamicIL' in t or 'DynamicResolver' in t or 'DynamicScope' in t: return '1D-ii-ILGeneration'
        return '1D-iii-EmitSupport'
    if 'System.Reflection' in t:
        if 'Metadata' in t: return '1C-iv-Metadata'
        if 'RuntimeType' in t or 'Type' == t.split('.')[-1] or 'TypeHandle' in t or 'RuntimeTypeCache' in t or 'MemberInfoCache' in t: return '1C-i-TypeSystem'
        if 'Method' in t or 'Field' in t or 'Property' in t or 'Constructor' in t or 'Invoker' in t or 'CustomAttribute' in t or 'Binder' in t or 'ParameterInfo' in t: return '1C-ii-Members'
        if 'Assembly' in t or 'Module' in t or 'AssemblyName' in t: return '1C-iii-Assembly'
        return '1C-ii-Members'
    if 'System.Globalization' in t:
        if 'CultureInfo' in t or 'CultureData' in t or 'GlobalizationMode' in t: return '1B-i-CultureInfra'
        return '1B-ii-Formatting'
    if 'System.Text' in t and 'Json' not in t:
        if 'StringBuilder' in t or 'ValueStringBuilder' in t or 'InterpolatedStringHandler' in t: return '1E-i-StringBuilder'
        if 'Encoding' in t or 'Encoder' in t or 'Decoder' in t or 'Fallback' in t: return '1E-ii-Encoding'
        return '1E-iii-Unicode'
    if 'System.Threading' in t:
        if 'Task' in t or 'ValueTask' in t or 'Awaiter' in t or 'AsyncMethodBuilder' in t: return '1H-iii-Async'
        if 'Thread' == t.split('.')[-1] or 'ThreadPool' in t or 'Monitor' in t or 'Lock' in t: return '1H-i-ThreadPrimitives'
        if 'SemaphoreSlim' in t or 'WaitHandle' in t or 'ManualResetEvent' in t or 'CancellationToken' in t or 'Timer' in t: return '1H-ii-Synchronization'
        return '1H-i-ThreadPrimitives'
    if 'System.Collections' in t:
        if 'Dictionary' in t or 'Hashtable' in t or 'HashSet' in t: return '1G-i-Dictionary'
        if 'Comparer' in t or 'EqualityComparer' in t or 'NonRandomized' in t: return '1G-ii-Comparer'
        if 'List' in t or 'Queue' in t or 'ReadOnly' in t or 'ValueListBuilder' in t: return '1G-iii-Lists'
        return '1G-i-Dictionary'
    if 'System.Buffers' in t:
        if 'ArrayPool' in t or 'SharedArrayPool' in t: return '1J-i-ArrayPool'
        if 'SearchValues' in t or 'IndexOfAny' in t or 'ProbabilisticMap' in t or 'AsciiChar' in t: return '1J-ii-SearchValues'
        return '1J-iii-BinaryPrimitives'
    if 'System.Runtime.CompilerServices' in t:
        if 'RuntimeHelpers' in t or 'CastHelpers' in t or 'CastCache' in t or 'MethodTable' in t or 'TypeHandle' in t: return '1K-i-RuntimeHelpers'
        if 'AsyncTaskMethodBuilder' in t or 'AsyncValueTaskMethodBuilder' in t or 'Pooling' in t: return '1K-ii-AsyncBuilders'
        return '1K-iii-Other'
    if 'System.Runtime.InteropServices' in t:
        if 'Marshal' in t and 'Marshalling' not in t: return '1L-i-Marshal'
        if 'SafeHandle' in t or 'GCHandle' in t or 'NativeMemory' in t or 'NativeLibrary' in t: return '1L-ii-SafeHandle'
        if 'Marshalling' in t: return '1L-iii-Marshalling'
        return '1L-i-Marshal'
    if 'Microsoft.Win32.SafeHandles' in t: return '1L-ii-SafeHandle'
    if 'System.IO' in t:
        if 'Stream' in t or 'BinaryReader' in t or 'MemoryStream' in t or 'UnmanagedMemory' in t: return '1I-i-Streams'
        if 'File' in t or 'Directory' in t or 'Path' in t: return '1I-ii-FileSystem'
        return '1I-i-Streams'
    if 'System.Diagnostics' in t:
        if 'StackTrace' in t or 'StackFrame' in t: return '1M-i-StackTrace'
        return '1M-ii-EventSource'
    if 'ResourceManager' in t or 'ResourceReader' in t: return '1N-i-Resources'
    if 'Serialization' in t: return '1N-ii-Serialization'
    reflection_in_system = {'RuntimeType','Type','RuntimeTypeHandle','RuntimeMethodHandle','RuntimeFieldHandle','DefaultBinder','Activator','SignatureType','SignatureConstructedGenericType','SignatureArrayType','SignaturePointerType','SignatureByRefType','SignatureHasElementType'}
    type_base = t.split('.')[-1].split('/')[0].split('`')[0]
    top_type = t.split('/')[0].split('.')[-1].split('`')[0] if '/' in t else type_base
    if t.startswith('System.') and (type_base in reflection_in_system or top_type in reflection_in_system):
        if type_base in ('RuntimeType','Type','RuntimeTypeHandle','SignatureType','SignatureConstructedGenericType','SignatureArrayType','SignaturePointerType','SignatureByRefType','SignatureHasElementType'): return '1C-i-TypeSystem'
        if type_base == 'DefaultBinder': return '1C-ii-Members'
        if type_base == 'Activator': return '1C-ii-Members'
        return '1C-i-TypeSystem'
    if t.startswith('System.') and '.' not in t[7:].replace('`1','').replace('`2',''):
        tb = t.split('.')[-1].split('/')[0].split('`')[0]
        if tb in {'Int16','Int32','Int64','Int128','UInt16','UInt32','UInt64','UInt128','Single','Double','Half','Decimal','Byte','SByte','Number','Convert','Math','MathF','BitConverter','Boolean','Char','IntPtr','UIntPtr','NFloat'}: return '1A-i-Numeric'
        if tb in {'String','Enum','Array','Object','ValueType','Delegate','MulticastDelegate','Guid','DateTime','TimeSpan','TimeZoneInfo','DateOnly','TimeOnly','DateTimeOffset','Range','Index','Attribute','Version','Uri','Console','Environment','TypedReference','ArgIterator','Tuple','ValueTuple','Span','ReadOnlySpan','Memory','ReadOnlyMemory','MemoryExtensions'}: return '1A-ii-CoreTypes'
        if tb in {'ThrowHelper','SR','Buffer','SpanHelpers','HashCode','Marvin','Random','Exception','SystemException','ArgumentException','ArgumentNullException','FormatException','OverflowException','GC','WeakReference','Lazy','EventArgs','EventHandler','Action','Func','Predicate','ParamsArray','HexConverter','ConsoleEncoding','MetadataImport'}: return '1A-iii-Infrastructure'
        return '1A-ii-CoreTypes'
    if 'RuntimeHelpers' in t or 'CastHelpers' in t: return '1K-i-RuntimeHelpers'
    if t == 'System.SR' or t == 'Interop': return '1A-iii-Infrastructure'
    if t.startswith('Interop'): return '1L-i-Marshal'
    if 'AssemblyLoadContext' in t or 'System.Runtime.Loader' in t: return '1C-iii-Assembly'
    return 'UNKNOWN'

# Build method-level cross-edges
method_cluster = {m['name']: classify(m['type']) for m in scc}
cross_edges = defaultdict(list)
for m in scc:
    src = method_cluster[m['name']]
    for callee in m.get('topCallees', []):
        if callee['name'] in scc_names:
            rec = method_lookup.get(callee['name'])
            if rec:
                dst = classify(rec['type'])
                if src != dst:
                    cross_edges[(src, dst)].append((m['name'], callee['name']))

# The 4 critical edges:
critical = [
    ('1H-i-ThreadPrimitives', '1H-ii-Synchronization'),
    ('1A-ii-CoreTypes', '1J-ii-SearchValues'),
    ('1J-ii-SearchValues', '1A-iii-Infrastructure'),
    ('1H-ii-Synchronization', '1L-ii-SafeHandle'),
]

print("=" * 100)
print("DETAILED ANALYSIS OF 4 CRITICAL SCC-BREAKING EDGES")
print("=" * 100)

for s, d in critical:
    edges_list = cross_edges.get((s, d), [])
    print(f"\n{'='*80}")
    print(f"EDGE: {s} -> {d}  ({len(edges_list)} method-level edges)")
    print(f"{'='*80}")
    for caller, callee in edges_list:
        ct = caller.split('::')[0].split('.')[-1]
        cm = caller.split('::')[-1][:60]
        dt = callee.split('::')[0].split('.')[-1]
        dm = callee.split('::')[-1][:60]
        print(f"  {ct}::{cm}")
        print(f"    -> {dt}::{dm}")
    if not edges_list:
        print("  (No method-level edges found in topCallees — edge exists through deeper call chain)")

# --- Now analyze "Type.op_Equality bottleneck" ---
print()
print("=" * 100)
print("TYPE_EQUALITY BOTTLENECK ANALYSIS")
print("Type::op_Equality and Type::get_IsValueType are called from many clusters")
print("=" * 100)

type_eq_callers = defaultdict(list)
for m in scc:
    src = method_cluster[m['name']]
    for callee in m.get('topCallees', []):
        if callee['name'] in scc_names:
            cname = callee['name']
            if 'Type::op_Equality' in cname or 'Type::op_Inequality' in cname or 'Type::get_IsValueType' in cname or 'Type::get_IsEnum' in cname:
                type_eq_callers[src].append((m['name'], cname))

print(f"\nClusters calling Type equality/type-check methods:")
for cluster in sorted(type_eq_callers.keys()):
    calls = type_eq_callers[cluster]
    print(f"  {cluster}: {len(calls)} calls")
    for caller, callee in calls[:3]:
        ct = caller.split('::')[0].split('.')[-1]
        cm = caller.split('::')[-1][:50]
        dm = callee.split('::')[-1][:50]
        print(f"    {ct}::{cm} -> {dm}")
    if len(calls) > 3:
        print(f"    ... and {len(calls)-3} more")

# --- Coarse parent-level analysis (1A, 1B, 1C, etc.) ---
print()
print("=" * 100)
print("COARSE PARENT-LEVEL (1A, 1B, ...) DEPENDENCY GRAPH")
print("=" * 100)

def parent(c):
    return c[:2]

parent_edges = defaultdict(int)
for (s,d), edges_list in cross_edges.items():
    ps, pd = parent(s), parent(d)
    if ps != pd:
        parent_edges[(ps, pd)] += len(edges_list)

parent_nodes = sorted(set(n for e in parent_edges for n in e))
print(f"Parent clusters: {parent_nodes}")
print(f"Parent-level edges: {len(parent_edges)}")
print()
for (s,d), count in sorted(parent_edges.items(), key=lambda x: -x[1]):
    print(f"  {s} -> {d}: {count} method-edges")

# Check for bidirectional parent edges
print()
print("Bidirectional parent-level couplings:")
seen = set()
for (s,d) in parent_edges:
    if (d,s) in parent_edges and (min(s,d), max(s,d)) not in seen:
        seen.add((min(s,d), max(s,d)))
        fwd = parent_edges[(s,d)]
        bwd = parent_edges[(d,s)]
        print(f"  {s} <-> {d}: {fwd} + {bwd} = {fwd+bwd}")

# Tarjan on parent graph
from collections import defaultdict as dd
pgraph = dd(set)
for (s,d) in parent_edges:
    pgraph[s].add(d)

index_counter = [0]; stack = []; lowlinks = {}; index = {}; on_stack = {}; result = []
def strongconnect(v):
    index[v] = index_counter[0]; lowlinks[v] = index_counter[0]; index_counter[0] += 1
    stack.append(v); on_stack[v] = True
    for w in pgraph.get(v, []):
        if w not in index:
            strongconnect(w)
            lowlinks[v] = min(lowlinks[v], lowlinks[w])
        elif on_stack.get(w, False):
            lowlinks[v] = min(lowlinks[v], index[w])
    if lowlinks[v] == index[v]:
        c = []
        while True:
            w = stack.pop(); on_stack[w] = False; c.append(w)
            if w == v: break
        result.append(sorted(c))
for v in sorted(parent_nodes):
    if v not in index:
        strongconnect(v)

print(f"\nParent-level SCCs:")
for comp in sorted(result, key=len, reverse=True):
    print(f"  Size {len(comp)}: {', '.join(comp)}")
