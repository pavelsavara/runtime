#!/usr/bin/env python3
"""Trace the StackTrace.IsSupported opportunity - what does keeping it true cost?"""
import json
from collections import defaultdict, deque

d = json.load(open('method-cost-full-callgraph.json'))
methods = d['methods']
SCC_SIZE = 163058
scc = [m for m in methods if m['transitiveSize'] == SCC_SIZE]
scc_names = {m['name'] for m in scc}
method_lookup = {m['name']: m for m in methods}

# Build full adjacency (all methods, not just SCC)
adj = defaultdict(set)
rev_adj = defaultdict(set)
for m in methods:
    caller = m['name']
    for c in m.get('topCallees', []):
        callee = c['name']
        if callee != caller:
            adj[caller].add(callee)
            rev_adj[callee].add(caller)

print("=== StackTrace/StackFrame methods in the dataset ===")
stack_methods = [m for m in methods if 'StackTrace' in m['type'] or 'StackFrame' in m['type']]
for m in sorted(stack_methods, key=lambda x: -x['ownILSize']):
    in_scc = "IN SCC" if m['name'] in scc_names else "not in SCC"
    print(f"  [{m['ownILSize']:5d}B] [{in_scc}] trans={m['transitiveSize']:6d}  {m['name']}")

print(f"\nTotal StackTrace/StackFrame methods: {len(stack_methods)}")
print(f"In SCC: {sum(1 for m in stack_methods if m['name'] in scc_names)}")
print(f"Total own IL: {sum(m['ownILSize'] for m in stack_methods)}B")

# Find all methods that mention StackTrace in their callees
print("\n=== Methods that call StackTrace/StackFrame methods ===")
stack_names = {m['name'] for m in stack_methods}
callers_of_stack = set()
for m in methods:
    for c in m.get('topCallees', []):
        if c['name'] in stack_names:
            callers_of_stack.add(m['name'])

for name in sorted(callers_of_stack):
    in_scc = "IN SCC" if name in scc_names else ""
    targets = [c for c in adj[name] if c in stack_names]
    print(f"  {in_scc:6s} {name}")
    for t in targets:
        print(f"           -> {t}")

# Find the StackTrace.IsSupported feature switch usage
print("\n=== StackTrace.IsSupported related methods ===")
for m in methods:
    if 'StackTrace' in m['name'] and ('IsSupported' in m['name'] or 'Supported' in m['name']):
        print(f"  [{m['ownILSize']:5d}B] {m['name']} trans={m['transitiveSize']}")

# What about Exception -> StackTrace chain?
print("\n=== Exception methods and their StackTrace connections ===")
exception_methods = [m for m in methods if 'Exception' in m['type'] and m['type'].endswith('Exception')]
for m in sorted(exception_methods, key=lambda x: -x['transitiveSize'])[:20]:
    in_scc = "IN SCC" if m['name'] in scc_names else ""
    st_callees = [c['name'] for c in m.get('topCallees', []) if 'StackTrace' in c['name'] or 'StackFrame' in c['name']]
    print(f"  [{m['ownILSize']:5d}B] {in_scc:6s} trans={m['transitiveSize']:6d}  {m['name']}")
    for c in st_callees:
        print(f"            -> {c}")

# Trace: what would be trimmed if StackTrace.IsSupported = false?
# Find methods gated by StackTrace.IsSupported
print("\n=== Forward reachability from StackTrace methods ===")
stack_reachable = set()
queue = deque(stack_names)
stack_reachable.update(stack_names)
while queue:
    current = queue.popleft()
    for neighbor in adj.get(current, set()):
        if neighbor not in stack_reachable:
            stack_reachable.add(neighbor)
            queue.append(neighbor)
print(f"Forward-reachable from StackTrace/StackFrame: {len(stack_reachable)} methods")
print(f"  In SCC: {len(stack_reachable & scc_names)}")
il = sum(method_lookup[n]['ownILSize'] for n in stack_reachable if n in method_lookup)
print(f"  Total own IL: {il}B")

# What ONLY StackTrace pulls in (not reachable from other roots)
print("\n=== Methods reachable ONLY through StackTrace (exclusive dependencies) ===")
# Get all non-StackTrace entry points
all_entries = set()
for m in methods:
    if m['name'] not in stack_names:
        all_entries.add(m['name'])

# BFS from all non-StackTrace methods
non_stack_reachable = set()
queue = deque(all_entries - stack_names)
non_stack_reachable.update(queue)
while queue:
    current = queue.popleft()
    for neighbor in adj.get(current, set()):
        if neighbor not in non_stack_reachable:
            non_stack_reachable.add(neighbor)
            queue.append(neighbor)

stack_exclusive = stack_reachable - non_stack_reachable
print(f"Exclusively reachable through StackTrace: {len(stack_exclusive)} methods")
il = sum(method_lookup[n]['ownILSize'] for n in stack_exclusive if n in method_lookup)
print(f"Total own IL: {il}B")
for n in sorted(stack_exclusive):
    m = method_lookup.get(n)
    if m:
        print(f"  [{m['ownILSize']:5d}B] {m['type']} :: {n.split('::')[1] if '::' in n else n}")

# What about the Reflection coupling? StackTrace -> Reflection
print("\n=== StackTrace -> Reflection coupling ===")
reflection_in_stack_reach = [n for n in stack_reachable if 'Reflection' in n.split('::')[0] or 'RuntimeType' in n.split('::')[0]]
print(f"Reflection methods reachable from StackTrace: {len(reflection_in_stack_reach)}")
# How many of these are SCC members?
refl_scc = [n for n in reflection_in_stack_reach if n in scc_names]
print(f"  Of which in SCC: {len(refl_scc)}")

# Direct callees of StackTrace/StackFrame methods that are in Reflection
print("\n=== Direct Reflection callees of StackTrace/StackFrame ===")
for sn in sorted(stack_names):
    refl_callees = [c for c in adj.get(sn, set()) if 'Reflection' in c.split('::')[0] or 'RuntimeType' in c.split('::')[0]]
    if refl_callees:
        m = method_lookup.get(sn)
        il = m['ownILSize'] if m else 0
        print(f"  [{il:4d}B] {sn}")
        for c in sorted(refl_callees):
            cm = method_lookup.get(c)
            cil = cm['ownILSize'] if cm else 0
            print(f"    -> [{cil:4d}B] {c}")

# Check: what does Exception.ToString and related do?
print("\n=== Exception methods and toString chains ===")
for m in methods:
    if m['type'] == 'System.Exception' or (m['type'].endswith('Exception') and 'ToString' in m['name']):
        callees_interesting = [c['name'] for c in m.get('topCallees', [])
                               if 'StackTrace' in c['name'] or 'Reflection' in c['name'] or 'RuntimeType' in c['name']]
        if callees_interesting or 'ToString' in m['name'] or 'StackTrace' in m['name']:
            print(f"  [{m['ownILSize']:5d}B] trans={m['transitiveSize']:6d} {m['name']}")
            for c in m.get('topCallees', []):
                print(f"    -> [{c.get('transitiveSize', 0):6d}] {c['name']}")

# StackTrace.IsSupported feature switch - what guards it
print("\n=== Diagnostics.StackTrace feature switch guard pattern ===")
for m in methods:
    if 'StackTrace' in m['name'] and 'get_IsSupported' in m['name']:
        print(f"  {m['name']} IL={m['ownILSize']}B trans={m['transitiveSize']}")
    if 'IsSupported' in m['name'] and 'Diagnostics' in m['name']:
        print(f"  {m['name']} IL={m['ownILSize']}B trans={m['transitiveSize']}")
