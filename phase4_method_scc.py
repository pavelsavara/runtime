#!/usr/bin/env python3
"""Phase 4: Method-Level SCC Analysis.

Loads the full call graph from method-cost-full-callgraph.json,
runs Tarjan's algorithm on the 942-method super-SCC,
and identifies articulation edges whose removal would break the SCC.
"""
import json
import sys
from collections import defaultdict

# ──────────────────────────────────────────────────────────────────────
# 1. Load data
# ──────────────────────────────────────────────────────────────────────
with open('method-cost-full-callgraph.json', 'r') as f:
    data = json.load(f)

methods = data['methods']
SCC_SIZE = 163058
scc_methods = [m for m in methods if m['transitiveSize'] == SCC_SIZE]
scc_names = {m['name'] for m in scc_methods}
method_lookup = {m['name']: m for m in methods}

print(f"Loaded {len(methods)} total methods, {len(scc_methods)} in super-SCC")
print(f"Super-SCC total own IL: {sum(m['ownILSize'] for m in scc_methods)} bytes")
print()

# ──────────────────────────────────────────────────────────────────────
# 2. Build adjacency list (only intra-SCC edges)
# ──────────────────────────────────────────────────────────────────────
adj = defaultdict(set)      # method -> set of called methods (within SCC)
rev_adj = defaultdict(set)  # reverse graph
all_edges = set()

for m in scc_methods:
    caller = m['name']
    for c in m.get('topCallees', []):
        callee = c['name']
        if callee in scc_names and callee != caller:
            adj[caller].add(callee)
            rev_adj[callee].add(caller)
            all_edges.add((caller, callee))

# Ensure every SCC node is represented
for m in scc_methods:
    if m['name'] not in adj:
        adj[m['name']]  # create empty entry

print(f"Intra-SCC edges: {len(all_edges)}")
print(f"Nodes with outgoing edges: {sum(1 for v in adj.values() if v)}")
print(f"Nodes with incoming edges: {sum(1 for v in rev_adj.values() if v)}")
print()

# ──────────────────────────────────────────────────────────────────────
# 3. Tarjan's SCC on the intra-SCC subgraph
# ──────────────────────────────────────────────────────────────────────
def tarjan_scc(graph, nodes):
    """Run Tarjan's SCC algorithm. Returns list of SCCs (each a set of node names)."""
    index_counter = [0]
    stack = []
    on_stack = set()
    index = {}
    lowlink = {}
    result = []

    def strongconnect(v):
        index[v] = index_counter[0]
        lowlink[v] = index_counter[0]
        index_counter[0] += 1
        stack.append(v)
        on_stack.add(v)

        for w in graph.get(v, set()):
            if w not in index:
                strongconnect(w)
                lowlink[v] = min(lowlink[v], lowlink[w])
            elif w in on_stack:
                lowlink[v] = min(lowlink[v], index[w])

        if lowlink[v] == index[v]:
            component = set()
            while True:
                w = stack.pop()
                on_stack.discard(w)
                component.add(w)
                if w == v:
                    break
            result.append(component)

    for v in nodes:
        if v not in index:
            strongconnect(v)

    return result

# Use iterative Tarjan to avoid recursion limit
sys.setrecursionlimit(10000)

all_nodes = list(scc_names)
sccs = tarjan_scc(adj, all_nodes)

# Sort by size descending
sccs.sort(key=len, reverse=True)

print("=== Tarjan SCC Results (method-level, intra-SCC subgraph) ===")
print(f"Total components: {len(sccs)}")
multi_sccs = [s for s in sccs if len(s) > 1]
singleton_sccs = [s for s in sccs if len(s) == 1]
print(f"Multi-method SCCs: {len(multi_sccs)}")
print(f"Singleton components: {len(singleton_sccs)}")
print()

for i, scc in enumerate(multi_sccs[:20]):
    il_size = sum(method_lookup[n]['ownILSize'] for n in scc if n in method_lookup)
    print(f"SCC #{i+1}: {len(scc)} methods, {il_size} bytes own IL")
    # Show representative types
    types = defaultdict(int)
    for n in scc:
        if n in method_lookup:
            types[method_lookup[n]['type']] += 1
    top_types = sorted(types.items(), key=lambda x: -x[1])[:10]
    for t, cnt in top_types:
        print(f"  [{cnt}] {t}")
    if len(scc) <= 20:
        for n in sorted(scc):
            print(f"    - {n}")
    print()

# ──────────────────────────────────────────────────────────────────────
# 4. Articulation edge analysis
# ──────────────────────────────────────────────────────────────────────
# For the largest SCC, find edges whose removal reduces the SCC size.
# Since full articulation analysis is expensive, we use heuristic:
# Find edges between different "type clusters" that are bridges.

if multi_sccs:
    largest_scc = multi_sccs[0]
    print(f"\n=== Largest SCC: {len(largest_scc)} methods ===")

    # Build sub-adjacency for largest SCC only
    sub_adj = defaultdict(set)
    sub_edges = set()
    for n in largest_scc:
        for callee in adj.get(n, set()):
            if callee in largest_scc:
                sub_adj[n].add(callee)
                sub_edges.add((n, callee))

    print(f"Edges in largest SCC: {len(sub_edges)}")

    # Try removing each edge and re-running Tarjan to find articulation edges
    # This is O(E * (V+E)) which for ~2000 edges and ~900 nodes is feasible
    print("\nSearching for articulation edges (edge removal that splits the SCC)...")

    articulation_edges = []
    for i, (u, v) in enumerate(sorted(sub_edges)):
        if i % 200 == 0:
            print(f"  Progress: {i}/{len(sub_edges)} edges tested...", file=sys.stderr)
        # Build graph without this edge
        test_adj = defaultdict(set)
        for n in largest_scc:
            for callee in sub_adj.get(n, set()):
                if (n, callee) != (u, v):
                    test_adj[n].add(callee)

        test_sccs = tarjan_scc(test_adj, list(largest_scc))
        test_largest = max(len(s) for s in test_sccs)
        if test_largest < len(largest_scc):
            reduction = len(largest_scc) - test_largest
            new_scc_count = sum(1 for s in test_sccs if len(s) > 1)
            articulation_edges.append((u, v, test_largest, reduction, new_scc_count, len(test_sccs)))

    articulation_edges.sort(key=lambda x: -x[3])  # sort by reduction

    print(f"\nFound {len(articulation_edges)} articulation edges")
    print()

    if articulation_edges:
        print("=== Top Articulation Edges (by SCC reduction) ===")
        for u, v, new_largest, reduction, new_multi, new_total in articulation_edges[:50]:
            caller_type = method_lookup[u]['type'] if u in method_lookup else '?'
            callee_type = method_lookup[v]['type'] if v in method_lookup else '?'
            print(f"  -{reduction} methods: [{caller_type}] {u}")
            print(f"    -> [{callee_type}] {v}")
            print(f"    New largest SCC: {new_largest}, multi-SCCs: {new_multi}, total components: {new_total}")
            print()

# ──────────────────────────────────────────────────────────────────────
# 5. Cross-type edge analysis (which type-to-type edges are bridges?)
# ──────────────────────────────────────────────────────────────────────
print("\n=== Cross-Type Edge Summary ===")
type_edges = defaultdict(int)
for u, v in all_edges:
    ut = method_lookup[u]['type'] if u in method_lookup else '?'
    vt = method_lookup[v]['type'] if v in method_lookup else '?'
    if ut != vt:
        type_edges[(ut, vt)] += 1

print(f"Distinct cross-type edges: {len(type_edges)}")
top_type_edges = sorted(type_edges.items(), key=lambda x: -x[1])[:30]
for (ut, vt), cnt in top_type_edges:
    print(f"  [{cnt}] {ut} -> {vt}")

# ──────────────────────────────────────────────────────────────────────
# 6. Method in-degree / out-degree analysis within SCC
# ──────────────────────────────────────────────────────────────────────
print("\n=== High-Degree Methods (potential hubs) ===")
in_degree = defaultdict(int)
out_degree = defaultdict(int)
for u, v in all_edges:
    out_degree[u] += 1
    in_degree[v] += 1

print("\nTop methods by in-degree (most called within SCC):")
for name, deg in sorted(in_degree.items(), key=lambda x: -x[1])[:20]:
    t = method_lookup[name]['type'] if name in method_lookup else '?'
    il = method_lookup[name]['ownILSize'] if name in method_lookup else 0
    print(f"  in={deg:3d} out={out_degree.get(name,0):3d} IL={il:5d}  {name}")

print("\nTop methods by out-degree (call most SCC methods):")
for name, deg in sorted(out_degree.items(), key=lambda x: -x[1])[:20]:
    t = method_lookup[name]['type'] if name in method_lookup else '?'
    il = method_lookup[name]['ownILSize'] if name in method_lookup else 0
    print(f"  in={in_degree.get(name,0):3d} out={deg:3d} IL={il:5d}  {name}")

# ──────────────────────────────────────────────────────────────────────
# 7. Classify each SCC method for coupling theory validation
# ──────────────────────────────────────────────────────────────────────
def get_namespace(name):
    """Extract approximate namespace from method name."""
    parts = name.split('::')[0].split('.')
    if len(parts) >= 2:
        return '.'.join(parts[:-1])
    return parts[0]

def get_domain(name):
    """Classify method into high-level domain."""
    n = name.split('::')[0]
    if 'Reflection.Emit' in n: return 'Emit'
    if 'Reflection' in n or 'RuntimeType' in n or 'DefaultBinder' in n or 'Activator' in n or 'SignatureType' in n: return 'Reflection'
    if 'Globalization' in n or 'CultureInfo' in n or 'CultureData' in n: return 'Globalization'
    if 'NumberFormat' in n or 'DateTimeFormat' in n or 'TimeSpanFormat' in n: return 'Formatting'
    if 'Intrinsics' in n or 'Scalar`1' in n: return 'Intrinsics'
    if 'Vector' in n and 'Numerics' in n: return 'Numerics'
    if 'Threading' in n or 'Thread' in n.split('.')[-1] or 'Task' in n.split('.')[-1]: return 'Threading'
    if 'Collections' in n: return 'Collections'
    if 'IO' in n or 'Stream' in n.split('.')[-1] or 'File' in n.split('.')[-1]: return 'IO'
    if 'Text' in n and 'Json' not in n: return 'Text'
    if 'Buffers' in n or 'SearchValues' in n: return 'Buffers'
    if 'Diagnostics' in n or 'StackTrace' in n: return 'Diagnostics'
    if 'Exception' in n or 'ThrowHelper' in n: return 'Exceptions'
    if 'Enum' in n.split('.')[-1].split('`')[0]: return 'Enum'
    if 'Convert' in n.split('.')[-1]: return 'Convert'
    if 'Marshal' in n: return 'Interop'
    if 'SafeHandle' in n or 'SafeFileHandle' in n: return 'SafeHandle'
    if 'AssemblyLoadContext' in n or 'Assembly' in n.split('.')[-1]: return 'Assembly'
    if 'Encoding' in n or 'Encoder' in n or 'Decoder' in n: return 'Encoding'
    if 'Serialization' in n: return 'Serialization'
    if 'Resource' in n: return 'Resources'
    if 'TimeZone' in n: return 'TimeZone'
    if 'String' == n.split('.')[-1] or 'StringBuilder' in n: return 'String'
    if 'Array' == n.split('.')[-1]: return 'Array'
    if 'Char' == n.split('.')[-1]: return 'Char'
    return 'Other'

print("\n\n=== Domain-Level Cross-Cutting Analysis ===")
domain_edges = defaultdict(int)
domain_methods = defaultdict(set)
for m in scc_methods:
    domain_methods[get_domain(m['name'])].add(m['name'])

for u, v in all_edges:
    du = get_domain(u)
    dv = get_domain(v)
    if du != dv:
        domain_edges[(du, dv)] += 1

print("\nDomain sizes:")
for d, ms in sorted(domain_methods.items(), key=lambda x: -len(x[1])):
    il = sum(method_lookup[n]['ownILSize'] for n in ms if n in method_lookup)
    print(f"  {d:20s}: {len(ms):3d} methods, {il:6d} bytes IL")

print("\nTop cross-domain edges:")
for (du, dv), cnt in sorted(domain_edges.items(), key=lambda x: -x[1])[:40]:
    print(f"  [{cnt:3d}] {du:20s} -> {dv}")

# ──────────────────────────────────────────────────────────────────────
# 8. Coupling theory validation
# ──────────────────────────────────────────────────────────────────────
print("\n\n=== Coupling Theory Validation ===")

def find_paths(graph, start_pred, end_pred, max_depth=5):
    """BFS to find shortest paths from methods matching start_pred to methods matching end_pred."""
    starts = [n for n in scc_names if start_pred(n)]
    ends = set(n for n in scc_names if end_pred(n))
    if not starts or not ends:
        return []

    results = []
    for start in starts[:3]:  # limit to 3 starts
        visited = {start}
        queue = [(start, [start])]
        found = False
        while queue and not found:
            current, path = queue.pop(0)
            if len(path) > max_depth:
                break
            for neighbor in graph.get(current, set()):
                if neighbor in ends:
                    results.append(path + [neighbor])
                    found = True
                    break
                if neighbor not in visited:
                    visited.add(neighbor)
                    queue.append((neighbor, path + [neighbor]))
    return results

theories = [
    ("T1: Exception.ToString -> StackTrace -> Reflection",
     lambda n: 'Exception' in n and 'ToString' in n,
     lambda n: 'Reflection' in n.split('::')[0] and 'Emit' not in n),
    ("T2: RuntimeType -> Reflection.Emit",
     lambda n: 'RuntimeType' in n.split('::')[0],
     lambda n: 'Reflection.Emit' in n),
    ("T3: CultureInfo <-> Number formatting",
     lambda n: 'CultureInfo' in n or 'CultureData' in n,
     lambda n: 'NumberFormat' in n or 'Number.' in n),
    ("T9: Enum.ToString -> RuntimeType -> Reflection",
     lambda n: 'Enum' in n.split('::')[0] and 'ToString' in n,
     lambda n: 'RuntimeType' in n.split('::')[0]),
    ("T10: DefaultBinder -> RuntimeType",
     lambda n: 'DefaultBinder' in n,
     lambda n: 'RuntimeType' in n.split('::')[0]),
    ("T13: Scalar`1 <-> numeric types",
     lambda n: 'Scalar`1' in n,
     lambda n: 'BitConverter' in n or ('Int' in n.split('.')[-1] and 'Intrinsics' not in n)),
    ("T18: DynamicMethod -> RuntimeILGenerator -> SignatureHelper -> RuntimeType",
     lambda n: 'DynamicMethod' in n,
     lambda n: 'RuntimeType' in n.split('::')[0]),
    ("T19: ThrowHelper -> exception -> StackTrace",
     lambda n: 'ThrowHelper' in n,
     lambda n: 'StackTrace' in n or 'StackFrame' in n),
    ("T20: CalendricalCalculationsHelper -> DateTimeFormatInfo",
     lambda n: 'CalendricalCalculations' in n,
     lambda n: 'DateTimeFormat' in n),
    ("T25: SerializationInfo -> RuntimeType",
     lambda n: 'SerializationInfo' in n,
     lambda n: 'RuntimeType' in n.split('::')[0]),
    ("T26: FieldAccessor -> Reflection.Emit (InvokerEmitUtil)",
     lambda n: 'FieldAccessor' in n or 'InvokerEmitUtil' in n,
     lambda n: 'Reflection.Emit' in n),
    ("T29: TimeZoneInfo -> IO + Globalization",
     lambda n: 'TimeZoneInfo' in n,
     lambda n: 'IO.' in n or 'Globalization' in n),
]

for title, start_pred, end_pred in theories:
    paths = find_paths(adj, start_pred, end_pred, max_depth=6)
    starts = [n for n in scc_names if start_pred(n)]
    ends = [n for n in scc_names if end_pred(n)]
    if paths:
        print(f"\n{title}: CONFIRMED ({len(paths)} paths found)")
        for path in paths[:2]:
            print(f"  Path ({len(path)-1} hops):")
            for step in path:
                il = method_lookup[step]['ownILSize'] if step in method_lookup else 0
                print(f"    [{il:5d}B] {step}")
    else:
        print(f"\n{title}: NOT FOUND in SCC (starts={len(starts)}, ends={len(ends)})")

# ──────────────────────────────────────────────────────────────────────
# 9. Greedy multi-edge cut analysis
# ──────────────────────────────────────────────────────────────────────
if multi_sccs and len(multi_sccs[0]) > 50:
    largest = multi_sccs[0]
    print(f"\n\n=== Greedy Multi-Edge Cut Analysis (largest SCC = {len(largest)}) ===")
    print("Finding minimal set of edges to maximally fragment the SCC...")

    current_adj = defaultdict(set)
    for n in largest:
        for callee in adj.get(n, set()):
            if callee in largest:
                current_adj[n].add(callee)

    cuts = []
    for iteration in range(30):
        current_edges = set()
        for n in largest:
            for callee in current_adj.get(n, set()):
                current_edges.add((n, callee))

        current_sccs = tarjan_scc(current_adj, list(largest))
        current_largest_scc = max(current_sccs, key=len)

        if len(current_largest_scc) <= 1:
            print(f"  SCC fully fragmented after {iteration} cuts!")
            break

        # Find best edge to cut from current largest SCC
        sub_edges = [(u, v) for u, v in current_edges if u in current_largest_scc and v in current_largest_scc]
        best_edge = None
        best_reduction = 0

        for u, v in sub_edges:
            test_adj = defaultdict(set)
            for n in current_largest_scc:
                for callee in current_adj.get(n, set()):
                    if callee in current_largest_scc and (n, callee) != (u, v):
                        test_adj[n].add(callee)
            test_sccs = tarjan_scc(test_adj, list(current_largest_scc))
            test_largest = max(len(s) for s in test_sccs)
            reduction = len(current_largest_scc) - test_largest
            if reduction > best_reduction:
                best_reduction = reduction
                best_edge = (u, v)

        if best_edge is None or best_reduction == 0:
            print(f"  No further progress at iteration {iteration}, SCC size = {len(current_largest_scc)}")
            break

        u, v = best_edge
        current_adj[u].discard(v)
        cuts.append((u, v, best_reduction, len(current_largest_scc)))

        ut = method_lookup[u]['type'] if u in method_lookup else '?'
        vt = method_lookup[v]['type'] if v in method_lookup else '?'
        new_size = len(current_largest_scc) - best_reduction
        print(f"  Cut #{iteration+1}: -{best_reduction} (SCC {len(current_largest_scc)} -> {new_size})")
        print(f"    [{ut}] {u}")
        print(f"    -> [{vt}] {v}")

    # Final state
    final_sccs = tarjan_scc(current_adj, list(largest))
    final_multi = [s for s in final_sccs if len(s) > 1]
    print(f"\nAfter {len(cuts)} cuts:")
    print(f"  Components: {len(final_sccs)}, multi-method SCCs: {len(final_multi)}")
    for i, s in enumerate(sorted(final_multi, key=len, reverse=True)[:10]):
        il = sum(method_lookup[n]['ownILSize'] for n in s if n in method_lookup)
        types = set(method_lookup[n]['type'] for n in s if n in method_lookup)
        print(f"  SCC-{i+1}: {len(s)} methods, {il}B IL, {len(types)} types")
        for t in sorted(types)[:5]:
            print(f"    - {t}")

print("\n\nDone.")
