#!/usr/bin/env python3
"""Trace the CustomAttribute::IsDefined articulation edge."""
import json
from collections import defaultdict

d = json.load(open('method-cost-full-callgraph.json'))
methods = d['methods']
SCC_SIZE = 163058
scc = [m for m in methods if m['transitiveSize'] == SCC_SIZE]
scc_names = {m['name'] for m in scc}
method_lookup = {m['name']: m for m in methods}

adj = defaultdict(set)
rev_adj = defaultdict(set)
for m in scc:
    caller = m['name']
    for c in m.get('topCallees', []):
        callee = c['name']
        if callee in scc_names and callee != caller:
            adj[caller].add(callee)
            rev_adj[callee].add(caller)

u = 'System.Reflection.RuntimeParameterInfo::IsDefined(Type, Boolean)'
v = 'System.Reflection.CustomAttribute::IsDefined(RuntimeParameterInfo, RuntimeType)'

def show_method(name, label=""):
    m = method_lookup.get(name)
    if not m:
        print(f"  {label}{name} -- NOT FOUND")
        return
    print(f"  {label}{name}")
    print(f"    IL: {m['ownILSize']}B, Type: {m['type']}")
    print(f"    Callees in SCC ({len(adj.get(name, set()))}):")
    for c in sorted(adj.get(name, set())):
        cil = method_lookup[c]['ownILSize'] if c in method_lookup else '?'
        print(f"      -> [{cil}B] {c}")
    print(f"    Callers in SCC ({len(rev_adj.get(name, set()))}):")
    for c in sorted(rev_adj.get(name, set())):
        print(f"      <- {c}")

print("=== The Articulation Edge ===")
show_method(u, "FROM: ")
print()
show_method(v, "TO: ")

# Now simulate the cut: remove this edge and run Tarjan
print("\n=== Simulating cut ===")

# Build sub-graph for the SCC after cuts 1+2 (Lock/Monitor already cut)
# Actually, let's simulate it in context of cut #3 (after cuts 1 and 2)
# But first let's just see what happens with this single edge cut

import sys
sys.setrecursionlimit(10000)

def tarjan_scc(graph, nodes):
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

# Simulate cuts 1+2 first, then show what cut 3 does
cut_edges = [
    ('System.Threading.Monitor::Enter(Object)', 'System.Threading.Lock::Enter()'),
    ('System.Threading.Lock::Exit()', 'System.Threading.Lock::ExitImpl()'),
]

test_adj = defaultdict(set)
for n in scc_names:
    for callee in adj.get(n, set()):
        if (n, callee) not in set(cut_edges):
            test_adj[n].add(callee)

sccs_after_12 = tarjan_scc(test_adj, list(scc_names))
sccs_after_12.sort(key=len, reverse=True)
print(f"After cuts 1+2 (Lock/Monitor): largest SCC = {len(sccs_after_12[0])}")

# Now remove cut 3
test_adj2 = defaultdict(set)
for n in scc_names:
    for callee in test_adj.get(n, set()):
        if (n, callee) != (u, v):
            test_adj2[n].add(callee)

sccs_after_123 = tarjan_scc(test_adj2, list(scc_names))
sccs_after_123.sort(key=len, reverse=True)
print(f"After cuts 1+2+3 (+ CustomAttribute): largest SCC = {len(sccs_after_123[0])}")

# Show what got separated
big_before = sccs_after_12[0]
big_after = sccs_after_123[0]
separated = big_before - big_after

# Find the component these separated methods ended up in
separated_components = [s for s in sccs_after_123 if s & separated]
print(f"\nSeparated methods: {len(separated)}")
print(f"They formed {len(separated_components)} new components:")
for i, comp in enumerate(sorted(separated_components, key=len, reverse=True)[:5]):
    types = set(method_lookup[n]['type'] for n in comp if n in method_lookup)
    il = sum(method_lookup[n]['ownILSize'] for n in comp if n in method_lookup)
    print(f"  Component {i+1}: {len(comp)} methods, {il}B IL, types: {sorted(types)[:5]}")
    if len(comp) <= 15:
        for n in sorted(comp):
            nil = method_lookup[n]['ownILSize'] if n in method_lookup else 0
            print(f"    [{nil:4d}B] {n}")

# What's the chain? Trace back from CustomAttribute::IsDefined to understand why it's a bridge
print("\n=== What does CustomAttribute::IsDefined pull in? ===")
# BFS from v (CustomAttribute::IsDefined) to find its reachable set within the pre-cut SCC
visited = set()
queue = [v]
visited.add(v)
while queue:
    current = queue.pop(0)
    for neighbor in test_adj.get(current, set()):
        if neighbor not in visited and neighbor in big_before:
            visited.add(neighbor)
            queue.append(neighbor)

print(f"Forward-reachable from CustomAttribute::IsDefined: {len(visited)} methods")

# And what reaches RuntimeParameterInfo::IsDefined?
visited_back = set()
# Build reverse of test_adj
test_rev = defaultdict(set)
for n in scc_names:
    for callee in test_adj.get(n, set()):
        test_rev[callee].add(n)

queue = [u]
visited_back.add(u)
while queue:
    current = queue.pop(0)
    for neighbor in test_rev.get(current, set()):
        if neighbor not in visited_back and neighbor in big_before:
            visited_back.add(neighbor)
            queue.append(neighbor)

print(f"Backward-reachable to RuntimeParameterInfo::IsDefined: {len(visited_back)} methods")
print(f"Overlap (cycle participants through this edge): {len(visited & visited_back)} methods")

# Show the path from CustomAttribute::IsDefined back to RuntimeParameterInfo::IsDefined
# (i.e., the return path that creates the cycle)
print("\n=== Return path: CustomAttribute::IsDefined -> ... -> RuntimeParameterInfo::IsDefined ===")
# BFS from v to u in test_adj (without the cut edge)
from collections import deque
bfs_queue = deque([(v, [v])])
bfs_visited = {v}
found_path = None
while bfs_queue:
    current, path = bfs_queue.popleft()
    if len(path) > 15:
        continue
    for neighbor in test_adj.get(current, set()):
        if neighbor == u:
            found_path = path + [neighbor]
            break
        if neighbor not in bfs_visited and neighbor in big_before:
            bfs_visited.add(neighbor)
            bfs_queue.append((neighbor, path + [neighbor]))
    if found_path:
        break

if found_path:
    print(f"Path length: {len(found_path) - 1} hops")
    for step in found_path:
        il = method_lookup[step]['ownILSize'] if step in method_lookup else 0
        t = method_lookup[step]['type'] if step in method_lookup else '?'
        print(f"  [{il:4d}B] [{t}] {step}")
else:
    print("No return path found (cut already breaks the cycle)")

# Also check: who calls RuntimeParameterInfo::IsDefined?
print("\n=== Who calls RuntimeParameterInfo::IsDefined in the SCC? ===")
for caller in sorted(rev_adj.get(u, set())):
    t = method_lookup[caller]['type'] if caller in method_lookup else '?'
    print(f"  [{t}] {caller}")

# And what calls CustomAttribute::IsDefined more broadly?
print("\n=== All callers of CustomAttribute::IsDefined variants in SCC ===")
for name in sorted(scc_names):
    if 'CustomAttribute' in name and 'IsDefined' in name:
        callers = rev_adj.get(name, set())
        print(f"{name} ({len(callers)} callers):")
        for c in sorted(callers):
            print(f"  <- {c}")
