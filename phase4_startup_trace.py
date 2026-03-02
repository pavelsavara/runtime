import json

with open('d:/runtime/method-cost-full-callgraph.json') as f:
    data = json.load(f)

SCC_SIZE = 163058

# Build method index
by_name = {}
for m in data['methods']:
    by_name[m['name']] = m

# Find all StartupHookProvider methods
startup_methods = [m for m in data['methods'] if 'StartupHookProvider' in m.get('type', '')]
print(f"StartupHookProvider methods: {len(startup_methods)}")
for m in startup_methods:
    in_scc = m['transitiveSize'] == SCC_SIZE
    name = m['name']
    print(f"  {name} -- ownSize={m['ownSize']}, transitiveSize={m['transitiveSize']}, inSCC={in_scc}")
    callees = m.get('topCallees', [])
    if callees:
        print(f"    callees ({len(callees)}):")
        for c in callees[:30]:
            c_in_scc = c['transitiveSize'] == SCC_SIZE
            marker = " [SCC]" if c_in_scc else ""
            print(f"      -> {c['name']} (transitiveSize={c['transitiveSize']}){marker}")

# Check which are in the SCC
scc_methods = set(m['name'] for m in data['methods'] if m['transitiveSize'] == SCC_SIZE)
startup_in_scc = [m['name'] for m in startup_methods if m['name'] in scc_methods]
print(f"\nStartupHookProvider methods IN the SCC: {len(startup_in_scc)}")
for n in startup_in_scc:
    print(f"  {n}")

# Trace forward reachability into SCC
print("\n--- Forward reachability from StartupHookProvider into SCC ---")
# Build adjacency from call graph
adj = {}
for m in data['methods']:
    callees = m.get('topCallees', [])
    adj[m['name']] = [c['name'] for c in callees]

# BFS from startup methods
from collections import deque
visited = set()
queue = deque()
for m in startup_methods:
    queue.append(m['name'])
    visited.add(m['name'])

while queue:
    node = queue.popleft()
    for callee_name in adj.get(node, []):
        if callee_name not in visited:
            visited.add(callee_name)
            queue.append(callee_name)

reached_scc = visited & scc_methods
print(f"Total methods reachable from StartupHookProvider: {len(visited)}")
print(f"SCC methods reachable: {len(reached_scc)} / {len(scc_methods)}")

# Find methods exclusively reachable through StartupHookProvider
# i.e., not reachable from any other root
all_methods = set(m['name'] for m in data['methods'])
non_startup_roots = [m['name'] for m in data['methods']
                     if 'StartupHookProvider' not in m.get('type', '')
                     and m.get('topCallees')]

# BFS from non-startup roots
visited_other = set()
queue2 = deque()
for r in non_startup_roots:
    if r not in visited_other:
        queue2.append(r)
        visited_other.add(r)

while queue2:
    node = queue2.popleft()
    for callee_name in adj.get(node, []):
        if callee_name not in visited_other:
            visited_other.add(callee_name)
            queue2.append(callee_name)

exclusive = visited - visited_other
print(f"\nMethods EXCLUSIVELY reachable via StartupHookProvider: {len(exclusive)}")
exclusive_il = sum(by_name[n]['ownSize'] for n in exclusive if n in by_name)
print(f"Exclusive IL size: {exclusive_il} bytes")
for n in sorted(exclusive):
    own = by_name[n]['ownSize'] if n in by_name else '?'
    print(f"  {n} (ownSize={own})")

# Check what Reflection APIs StartupHookProvider uses
print("\n--- Direct Reflection callees from CallStartupHook(StartupHookNameOrPath) ---")
call_hook = None
for m in startup_methods:
    if 'CallStartupHook(StartupHookNameOrPath)' in m['name']:
        call_hook = m
        break

if call_hook:
    callees = call_hook.get('topCallees', [])
    reflection_callees = [c for c in callees if any(kw in c['name'] for kw in
        ['GetType', 'GetMethod', 'Invoke', 'Assembly', 'Reflection', 'MethodInfo',
         'MethodBase', 'BindingFlags', 'RuntimeType', 'Module', 'LoadFrom'])]
    print(f"Reflection-related callees: {len(reflection_callees)}")
    for c in reflection_callees:
        c_in_scc = c['transitiveSize'] == SCC_SIZE
        marker = " [SCC]" if c_in_scc else ""
        print(f"  -> {c['name']} (transitiveSize={c['transitiveSize']}){marker}")
