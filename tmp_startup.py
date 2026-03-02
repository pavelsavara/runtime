import json

with open('d:/runtime/method-cost-full-callgraph.json') as f:
    data = json.load(f)

for m in data['methods']:
    if 'StartupHook' in m['name']:
        name = m['name']
        own = m['ownSize']
        trans = m['transitiveSize']
        print(f"{name}  ownSize={own}  transitiveSize={trans}")
        callees = m.get('topCallees', [])
        for c in callees:
            cn = c['name']
            ct = c['transitiveSize']
            print(f"  -> {cn}  tSize={ct}")
