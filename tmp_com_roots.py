import json

with open("d:/runtime/method-cost-full-callgraph.json") as f:
    data = json.load(f)

by_name = {m["name"]: m for m in data["methods"]}

# Broader search: any method whose callees mention ComVariant
print("=== Any method calling into ComVariant type ===")
for m in data["methods"]:
    for c in m.get("topCallees", []):
        if "ComVariant" in c["name"] and "ComVariant" not in m.get("type", ""):
            print(f"  {m['name']} -> {c['name']}")

# Any method calling COMException
print("\n=== Any method calling into COMException type ===")
for m in data["methods"]:
    for c in m.get("topCallees", []):
        if "COMException" in c["name"] and "COMException" not in m.get("type", ""):
            print(f"  {m['name']} -> {c['name']}")

# Any method calling ExternalException
print("\n=== Any method calling into ExternalException type ===")
for m in data["methods"]:
    for c in m.get("topCallees", []):
        if "ExternalException" in c["name"] and "ExternalException" not in m.get("type", ""):
            print(f"  {m['name']} -> {c['name']}")

# Check DynamicInterfaceCastableHelpers - it has high transitive size
print("\n=== DynamicInterfaceCastableHelpers callees ===")
for m in data["methods"]:
    if "DynamicInterfaceCastableHelpers" in m.get("type", ""):
        print(f"\n{m['name']} own={m['ownSize']} tSize={m['transitiveSize']}")
        for c in m.get("topCallees", []):
            print(f"  -> {c['name']} tSize={c['transitiveSize']}")

# Check who calls DynamicInterfaceCastableHelpers
print("\n=== DynamicInterfaceCastableHelpers callers ===")
dic_names = set(m["name"] for m in data["methods"] if "DynamicInterfaceCastableHelpers" in m.get("type", ""))
for m in data["methods"]:
    for c in m.get("topCallees", []):
        if c["name"] in dic_names:
            print(f"  {m['name']} -> {c['name']}")

# Check ComponentActivator callers
print("\n=== ComponentActivator callers ===")
ca_names = set(m["name"] for m in data["methods"] if "ComponentActivator" in m.get("type", ""))
for m in data["methods"]:
    for c in m.get("topCallees", []):
        if c["name"] in ca_names:
            print(f"  {m['name']} -> {c['name']}")
