#!/usr/bin/env python3
"""Phase 2: Build cross-cluster dependency graph from method-cost call graph data."""
import json
import re
from collections import defaultdict

with open('method-cost-full.json', 'r') as f:
    data = json.load(f)

methods = data['methods']
scc = [m for m in methods if m['transitiveSize'] == 163058]
scc_names = set(m['name'] for m in scc)
all_by_name = {m['name']: m for m in methods}

print(f"SCC: {len(scc)} methods")

# Check topCallees stats
callee_counts = [len(m.get('topCallees', [])) for m in scc]
with_callees = sum(1 for c in callee_counts if c > 0)
print(f"Methods with topCallees: {with_callees}/{len(scc)}")
print(f"Callee count range: {min(callee_counts)}-{max(callee_counts)}, avg={sum(callee_counts)/len(callee_counts):.1f}")

# Show a few examples
print("\nSample callees:")
for m in scc[:3]:
    print(f"\n  {m['name'][:80]}")
    for c in m.get('topCallees', [])[:8]:
        tag = "SCC" if c['name'] in scc_names else "ext"
        print(f"    [{tag}] {c['name'][:70]} ({c['transitiveSize']}B)")
