import json
from collections import deque

with open('d:/runtime/method-cost-full-callgraph.json') as f:
    data = json.load(f)

SCC = 163058

by_name = {}
for m in data['methods']:
    by_name[m['name']] = m

adj = {}
for m in data['methods']:
    adj[m['name']] = [c['name'] for c in m.get('topCallees', [])]

radj = {}
for m in data['methods']:
    for c in m.get('topCallees', []):
        radj.setdefault(c['name'], []).append(m['name'])

scc = set(m['name'] for m in data['methods'] if m['transitiveSize'] == SCC)

target = 'System.Reflection.Emit.RuntimeTypeBuilder::get_UnderlyingSystemType()'

# Trace: what does get_UnderlyingSystemType call that's in the SCC?
print("=== RuntimeTypeBuilder::get_UnderlyingSystemType callees ===")
for c in by_name[target].get('topCallees', []):
    marker = " [SCC]" if c['transitiveSize'] == SCC else ""
    print(f"  -> {c['name']} (tSize={c['transitiveSize']}){marker}")

# Trace forward from get_UnderlyingSystemType through SCC only
print("\n=== Forward path from get_UnderlyingSystemType into SCC (BFS, SCC only) ===")
visited = {target: None}
queue = deque([target])
while queue:
    node = queue.popleft()
    for callee in adj.get(node, []):
        if callee not in visited and callee in scc:
            visited[callee] = node
            queue.append(callee)

# Find path back to get_UnderlyingSystemType
back_path = None
for caller_of_ust in radj.get(target, []):
    if caller_of_ust in visited and caller_of_ust in scc:
        # Trace path from caller back to target
        path = []
        node = caller_of_ust
        while node is not None:
            path.append(node)
            node = visited[node]
        path.reverse()
        if back_path is None or len(path) < len(back_path):
            back_path = path + [target]

if back_path:
    print(f"Shortest cycle ({len(back_path)} hops):")
    for i, p in enumerate(back_path):
        own = by_name[p]['ownSize'] if p in by_name else '?'
        print(f"  {i}: {p} (ownSize={own})")

# How does Type.IsEnum reach back?
print("\n=== Type::get_IsEnum() callees ===")
is_enum = 'System.Type::get_IsEnum()'
if is_enum in by_name:
    for c in by_name[is_enum].get('topCallees', []):
        marker = " [SCC]" if c['transitiveSize'] == SCC else ""
        print(f"  -> {c['name']} (tSize={c['transitiveSize']}){marker}")

# Trace the specific paths: Type.IsEnum -> ... -> back to get_UnderlyingSystemType
print("\n=== IsEnum call chain ===")
is_enum_impl = 'System.Type::get_IsEnumDefined()'
# Check IsEnum's callees
for callee_name in adj.get(is_enum, []):
    if callee_name in scc:
        print(f"  IsEnum -> {callee_name}")
        for c2 in adj.get(callee_name, []):
            if c2 in scc:
                print(f"    -> {c2}")

# What calls Type.op_Inequality and Type.op_Equality?
print("\n=== Type::op_Inequality callees ===")
op_neq = 'System.Type::op_Inequality(Type, Type)'
if op_neq in by_name:
    for c in by_name[op_neq].get('topCallees', []):
        marker = " [SCC]" if c['transitiveSize'] == SCC else ""
        print(f"  -> {c['name']} (tSize={c['transitiveSize']}){marker}")

print("\n=== Type::op_Equality callees ===")
op_eq = 'System.Type::op_Equality(Type, Type)'
if op_eq in by_name:
    for c in by_name[op_eq].get('topCallees', []):
        marker = " [SCC]" if c['transitiveSize'] == SCC else ""
        print(f"  -> {c['name']} (tSize={c['transitiveSize']}){marker}")

# Pattern: .UnderlyingSystemType is not RuntimeType
# Count how many callers of get_UnderlyingSystemType are in SCC vs not
scc_callers = [c for c in radj.get(target, []) if c in scc]
non_scc_callers = [c for c in radj.get(target, []) if c not in scc]
print(f"\n=== Caller summary ===")
print(f"Total callers: {len(radj.get(target, []))}")
print(f"SCC callers: {len(scc_callers)}")
print(f"Non-SCC callers: {len(non_scc_callers)}")

print("\nSCC callers:")
for c in sorted(scc_callers):
    print(f"  {c}")

print("\nNon-SCC callers:")
for c in sorted(non_scc_callers):
    own = by_name[c]['ownSize'] if c in by_name else '?'
    tsize = by_name[c]['transitiveSize'] if c in by_name else '?'
    print(f"  {c} (tSize={tsize})")

# Count all places that call any UnderlyingSystemType
print("\n=== All UnderlyingSystemType virtual dispatch callers ===")
ust_base = 'System.Type::get_UnderlyingSystemType()'
all_ust_callers = set()
for ust_method in by_name:
    if 'UnderlyingSystemType' in ust_method:
        for caller in radj.get(ust_method, []):
            all_ust_callers.add(caller)
print(f"Total methods calling any UnderlyingSystemType: {len(all_ust_callers)}")
