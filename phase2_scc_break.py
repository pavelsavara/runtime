#!/usr/bin/env python3
"""Phase 2: Identify minimal SCC-breaking edges using condensed cluster graph."""
import json, sys
from collections import defaultdict

with open('method-cost-full.json', 'r') as f:
    data = json.load(f)

methods = data['methods']
SCC_SIZE = 163058
scc = [m for m in methods if m['transitiveSize'] == SCC_SIZE]
scc_names = {m['name'] for m in scc}
method_lookup = {m['name']: m for m in methods}

# --- classify (same as phase2_deps.py, abbreviated) ---
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

# Build cluster->cluster directed graph
method_cluster = {m['name']: classify(m['type']) for m in scc}
edges = set()
for m in scc:
    src = method_cluster[m['name']]
    for callee in m.get('topCallees', []):
        if callee['name'] in scc_names:
            rec = method_lookup.get(callee['name'])
            if rec:
                dst = classify(rec['type'])
                if src != dst:
                    edges.add((src, dst))

# Build adjacency list
graph = defaultdict(set)
for s, d in edges:
    graph[s].add(d)

all_nodes = sorted(set(n for e in edges for n in e))
print(f"Cluster graph: {len(all_nodes)} nodes, {len(edges)} directed edges")
print()

# --- Tarjan's SCC on cluster graph ---
index_counter = [0]
stack = []
lowlinks = {}
index = {}
on_stack = {}
result = []

def strongconnect(v):
    index[v] = index_counter[0]
    lowlinks[v] = index_counter[0]
    index_counter[0] += 1
    stack.append(v)
    on_stack[v] = True
    for w in graph.get(v, []):
        if w not in index:
            strongconnect(w)
            lowlinks[v] = min(lowlinks[v], lowlinks[w])
        elif on_stack.get(w, False):
            lowlinks[v] = min(lowlinks[v], index[w])
    if lowlinks[v] == index[v]:
        component = []
        while True:
            w = stack.pop()
            on_stack[w] = False
            component.append(w)
            if w == v:
                break
        result.append(sorted(component))

for v in sorted(all_nodes):
    if v not in index:
        strongconnect(v)

print("SCCs in cluster graph:")
for comp in sorted(result, key=len, reverse=True):
    print(f"  Size {len(comp)}: {', '.join(comp)}")
print()

# Find the main cluster-level SCC (largest)
main_scc = max(result, key=len)
main_scc_set = set(main_scc)
print(f"Main cluster-level SCC: {len(main_scc)} clusters")
print()

# --- Find minimal edge cuts ---
# For each edge in the cluster SCC, check if removing it breaks the SCC
print("=" * 100)
print("EDGE REMOVAL ANALYSIS: Which edges, if removed, reduce the SCC?")
print("=" * 100)

def find_scc_size(graph_edges, nodes):
    """Tarjan's to find largest SCC size."""
    g = defaultdict(set)
    for s, d in graph_edges:
        g[s].add(d)
    idx_ctr = [0]
    stk = []
    ll = {}
    idx = {}
    on_stk = {}
    res = []
    def sc(v):
        idx[v] = idx_ctr[0]; ll[v] = idx_ctr[0]; idx_ctr[0] += 1
        stk.append(v); on_stk[v] = True
        for w in g.get(v, []):
            if w in nodes:
                if w not in idx:
                    sc(w)
                    ll[v] = min(ll[v], ll[w])
                elif on_stk.get(w, False):
                    ll[v] = min(ll[v], idx[w])
        if ll[v] == idx[v]:
            c = []
            while True:
                w = stk.pop(); on_stk[w] = False; c.append(w)
                if w == v: break
            res.append(c)
    for v in sorted(nodes):
        if v not in idx: sc(v)
    return max(len(c) for c in res) if res else 0

original_scc_size = len(main_scc)
# Only test edges within the main SCC
scc_edges = [(s, d) for s, d in edges if s in main_scc_set and d in main_scc_set]

print(f"\nTesting removal of each of {len(scc_edges)} edges within the {original_scc_size}-cluster SCC:")
print()

cuts = []
for rem_edge in scc_edges:
    remaining = [e for e in scc_edges if e != rem_edge]
    new_size = find_scc_size(remaining, main_scc_set)
    if new_size < original_scc_size:
        cuts.append((rem_edge, new_size, original_scc_size - new_size))

cuts.sort(key=lambda x: x[2], reverse=True)
if cuts:
    print(f"Found {len(cuts)} edges whose removal reduces the SCC:")
    for (s, d), new_sz, reduction in cuts:
        print(f"  Remove {s} -> {d}: SCC {original_scc_size} -> {new_sz} (reduces by {reduction})")
else:
    print("No single edge removal breaks the SCC — all edges are redundant.")
    print("Need to try removing edge PAIRS...")
    print()

    # Try removing pairs of edges that share a node (more tractable)
    # Focus on edges involving specific clusters that seem peripheral
    peripheral = []
    for n in main_scc:
        in_deg = sum(1 for s, d in scc_edges if d == n)
        out_deg = sum(1 for s, d in scc_edges if s == n)
        peripheral.append((n, in_deg, out_deg, in_deg + out_deg))
    peripheral.sort(key=lambda x: x[3])

    print("Clusters by total edge degree (low degree = easier to disconnect):")
    for n, indeg, outdeg, total in peripheral:
        print(f"  {n:<30} in={indeg} out={outdeg} total={total}")

    print()
    print("Testing edge-pair removals for low-degree clusters...")
    # For lowest-degree clusters, try removing all in-edges or all out-edges
    for n, indeg, outdeg, total in peripheral[:10]:
        # Try removing all in-edges
        in_edges = [(s, d) for s, d in scc_edges if d == n]
        remaining = [e for e in scc_edges if e not in in_edges]
        new_size = find_scc_size(remaining, main_scc_set)
        if new_size < original_scc_size:
            print(f"  Remove all in-edges to {n} ({len(in_edges)} edges): SCC -> {new_size} (reduces by {original_scc_size - new_size})")
            for e in in_edges:
                print(f"    {e[0]} -> {e[1]}")

        # Try removing all out-edges
        out_edges = [(s, d) for s, d in scc_edges if s == n]
        remaining = [e for e in scc_edges if e not in out_edges]
        new_size = find_scc_size(remaining, main_scc_set)
        if new_size < original_scc_size:
            print(f"  Remove all out-edges from {n} ({len(out_edges)} edges): SCC -> {new_size} (reduces by {original_scc_size - new_size})")
            for e in out_edges:
                print(f"    {e[0]} -> {e[1]}")

# --- Also analyze at coarser "parent cluster" level ---
print()
print("=" * 100)
print("COARSE PARENT-CLUSTER ANALYSIS")
print("=" * 100)

def parent(cluster):
    """Get parent cluster from sub-cluster label."""
    return cluster.split('-')[0] + '-' + cluster.split('-')[1]

parent_edges = set()
for s, d in edges:
    ps, pd = parent(s), parent(d)
    if ps != pd:
        parent_edges.add((ps, pd))

parent_nodes = sorted(set(n for e in parent_edges for n in e))
print(f"Parent cluster graph: {len(parent_nodes)} nodes, {len(parent_edges)} edges")

# Find parent-level SCCs
parent_graph = defaultdict(set)
for s, d in parent_edges:
    parent_graph[s].add(d)

sys.setrecursionlimit(10000)
index_counter = [0]; stack = []; lowlinks = {}; index = {}; on_stack = {}; result = []
for v in sorted(parent_nodes):
    if v not in index:
        strongconnect(v)

print("Parent-level SCCs:")
for comp in sorted(result, key=len, reverse=True):
    print(f"  Size {len(comp)}: {', '.join(comp)}")

parent_main = max(result, key=len)
parent_scc_set = set(parent_main)
parent_scc_edges = [(s,d) for s,d in parent_edges if s in parent_scc_set and d in parent_scc_set]

print(f"\nParent SCC edges ({len(parent_scc_edges)}):")
for s, d in sorted(parent_scc_edges):
    # Count underlying method-level edges
    count = sum(1 for e in scc_edges if parent(e[0]) == s and parent(e[1]) == d)
    print(f"  {s:>5} -> {d:<5}  ({count} method-edges)")

# Test single edge removals at parent level
print(f"\nTesting single-edge removal at parent level ({len(parent_scc_edges)} edges):")
for rem_edge in parent_scc_edges:
    remaining = [e for e in parent_scc_edges if e != rem_edge]
    new_size = find_scc_size(remaining, parent_scc_set)
    if new_size < len(parent_main):
        print(f"  Remove {rem_edge[0]} -> {rem_edge[1]}: SCC {len(parent_main)} -> {new_size}")
