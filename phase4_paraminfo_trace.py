import json
from collections import deque

with open('d:/runtime/method-cost-full-callgraph.json') as f:
    data = json.load(f)

SCC_SIZE = 163058

# Build indices
by_name = {}
for m in data['methods']:
    by_name[m['name']] = m

# Build adjacency
adj = {}
for m in data['methods']:
    adj[m['name']] = [c['name'] for c in m.get('topCallees', [])]

# Build reverse adjacency
radj = {}
for m in data['methods']:
    for c in m.get('topCallees', []):
        radj.setdefault(c['name'], []).append(m['name'])

scc_methods = set(m['name'] for m in data['methods'] if m['transitiveSize'] == SCC_SIZE)

# 1. RuntimeParameterInfo::IsDefined details
target = 'System.Reflection.RuntimeParameterInfo::IsDefined(Type, Boolean)'
print(f"=== {target} ===")
if target in by_name:
    m = by_name[target]
    print(f"  ownSize={m['ownSize']}, transitiveSize={m['transitiveSize']}, inSCC={m['transitiveSize']==SCC_SIZE}")
    print(f"  Callees:")
    for c in m.get('topCallees', []):
        marker = " [SCC]" if c['transitiveSize'] == SCC_SIZE else ""
        print(f"    -> {c['name']} (tSize={c['transitiveSize']}){marker}")

print(f"\n  Callers (who calls IsDefined):")
for caller in radj.get(target, []):
    in_scc = by_name[caller]['transitiveSize'] == SCC_SIZE if caller in by_name else False
    marker = " [SCC]" if in_scc else ""
    print(f"    <- {caller}{marker}")

# 2. CustomAttribute::IsDefined(RuntimeParameterInfo, RuntimeType)
target2 = 'System.Reflection.CustomAttribute::IsDefined(RuntimeParameterInfo, RuntimeType)'
print(f"\n=== {target2} ===")
if target2 in by_name:
    m = by_name[target2]
    print(f"  ownSize={m['ownSize']}, transitiveSize={m['transitiveSize']}")
    print(f"  Callees:")
    for c in m.get('topCallees', []):
        marker = " [SCC]" if c['transitiveSize'] == SCC_SIZE else ""
        print(f"    -> {c['name']} (tSize={c['transitiveSize']}){marker}")
    print(f"  Callers:")
    for caller in radj.get(target2, []):
        print(f"    <- {caller}")

# 3. PseudoCustomAttribute::IsDefined(RuntimeParameterInfo, RuntimeType)
target3 = 'System.Reflection.PseudoCustomAttribute::IsDefined(RuntimeParameterInfo, RuntimeType)'
print(f"\n=== {target3} ===")
if target3 in by_name:
    m = by_name[target3]
    print(f"  ownSize={m['ownSize']}, transitiveSize={m['transitiveSize']}")
    print(f"  Callees:")
    for c in m.get('topCallees', []):
        marker = " [SCC]" if c['transitiveSize'] == SCC_SIZE else ""
        print(f"    -> {c['name']} (tSize={c['transitiveSize']}){marker}")

# 4. Trace the full cycle path from FilterApplyMethodBase -> IsDefined -> ... -> back
print("\n=== Cycle trace: FilterApplyMethodBase -> IsDefined -> ... -> back ===")
# BFS from IsDefined looking for paths back to FilterApplyMethodBase
start = target
goal_prefix = 'FilterApplyMethodBase'

# Use BFS with parent tracking
visited = {start: None}
queue = deque([start])
found_cycle = None

while queue:
    node = queue.popleft()
    for callee in adj.get(node, []):
        if callee not in visited:
            visited[callee] = node
            queue.append(callee)
            if goal_prefix in callee:
                found_cycle = callee
                break
    if found_cycle:
        break

if found_cycle:
    path = []
    node = found_cycle
    while node is not None:
        path.append(node)
        node = visited[node]
    path.reverse()
    print(f"Path ({len(path)} hops):")
    for i, p in enumerate(path):
        in_scc = p in scc_methods
        marker = " [SCC]" if in_scc else ""
        own = by_name[p]['ownSize'] if p in by_name else '?'
        print(f"  {i}: {p} (ownSize={own}){marker}")

# 5. All callers of IsDefined across the codebase (any ParameterInfo.IsDefined)
print("\n=== All callers of any IsDefined on ParameterInfo ===")
for method_name, callers in radj.items():
    if 'ParameterInfo' in method_name and 'IsDefined' in method_name:
        print(f"\n{method_name}:")
        for caller in callers:
            own = by_name[caller]['ownSize'] if caller in by_name else '?'
            in_scc = caller in scc_methods
            marker = " [SCC]" if in_scc else ""
            print(f"  <- {caller} (ownSize={own}){marker}")

# 6. DefaultBinder IsDefined calls
print("\n=== IsDefined calls from DefaultBinder ===")
for m in data['methods']:
    if 'DefaultBinder' in m.get('type', '') or 'DefaultBinder' in m['name']:
        callees = m.get('topCallees', [])
        is_defined_callees = [c for c in callees if 'IsDefined' in c['name']]
        if is_defined_callees:
            print(f"{m['name']} (ownSize={m['ownSize']}, tSize={m['transitiveSize']}):")
            for c in is_defined_callees:
                print(f"  -> {c['name']} (tSize={c['transitiveSize']})")

# 7. Who calls ParameterInfo.IsDefined (the virtual)
print("\n=== Callers of ParameterInfo.IsDefined (virtual dispatch) ===")
for method_name, callers in radj.items():
    if method_name == 'System.Reflection.ParameterInfo::IsDefined(Type, Boolean)':
        for caller in callers:
            own = by_name[caller]['ownSize'] if caller in by_name else '?'
            in_scc = caller in scc_methods
            marker = " [SCC]" if in_scc else ""
            print(f"  <- {caller} (ownSize={own}){marker}")

# 8. What methods become exclusively unreachable if IsDefined edge is cut?
# Simulate removing the call from FilterApplyMethodBase to IsDefined
print("\n=== Impact analysis: cutting FilterApplyMethodBase -> IsDefined ===")

# Find all FilterApplyMethodBase methods
filter_methods = [m['name'] for m in data['methods'] if 'FilterApplyMethodBase' in m['name']]
print(f"FilterApplyMethodBase methods: {filter_methods}")

# Build modified adjacency, removing IsDefined calls from FilterApplyMethodBase
adj_modified = {}
for name, callees in adj.items():
    if 'FilterApplyMethodBase' in name:
        adj_modified[name] = [c for c in callees if 'IsDefined' not in c]
    else:
        adj_modified[name] = callees

# BFS from all methods
all_methods = set(by_name.keys())
reachable_original = set()
queue = deque(all_methods)
reachable_original = set(all_methods)  # all are roots in method-cost

# Better: find methods NOT reachable from non-IsDefined paths
# Count how many callers each method has, excluding FilterApplyMethodBase->IsDefined
is_defined_methods = set()
for mn in by_name:
    if 'IsDefined' in mn and 'ParameterInfo' in mn:
        is_defined_methods.add(mn)

print(f"\nIsDefined methods: {len(is_defined_methods)}")
for m in sorted(is_defined_methods):
    callers = radj.get(m, [])
    non_filter_callers = [c for c in callers if 'FilterApplyMethodBase' not in c]
    print(f"  {m}")
    print(f"    All callers: {len(callers)}")
    print(f"    Non-FilterApplyMethodBase callers: {len(non_filter_callers)}")
    for c in non_filter_callers:
        print(f"      <- {c}")
