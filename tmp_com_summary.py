import json

with open("d:/runtime/method-cost-full-callgraph.json") as f:
    data = json.load(f)

# COM-specific types that should NOT exist in a WASM browser app
com_types = [
    "System.Runtime.InteropServices.COMException",
    "System.Runtime.InteropServices.ExternalException",
    "System.Runtime.InteropServices.SEHException",
    "System.Runtime.InteropServices.Marshalling.ComVariant",
    "System.Runtime.InteropServices.Marshalling.ComVariant/Vector`1",
    "System.Runtime.InteropServices.InvalidOleVariantTypeException",
    "System.Runtime.InteropServices.ComImportAttribute",
    "System.Runtime.InteropServices.DynamicInterfaceCastableHelpers",
    "Internal.Runtime.InteropServices.ComponentActivator",
    "System.Runtime.InteropServices.MarshalDirectiveException",
]

print("=== COM/Interop types in trimmed WASM app ===\n")
total_own = 0
for tp in com_types:
    methods = [m for m in data["methods"] if m.get("type", "") == tp]
    if methods:
        own = sum(m["ownSize"] for m in methods)
        total_own += own
        max_ts = max(m["transitiveSize"] for m in methods)
        in_scc = any(m["transitiveSize"] == 163058 for m in methods)
        callers = []
        method_names = set(m["name"] for m in methods)
        for outer in data["methods"]:
            if outer["type"] == tp:
                continue
            for c in outer.get("topCallees", []):
                if c["name"] in method_names:
                    callers.append(outer["name"])
                    break
        print(f"{tp}")
        print(f"  {len(methods)} methods, {own}B own, maxTransitive={max_ts}, inSCC={in_scc}")
        print(f"  External callers: {len(callers)}")
        if callers:
            for c in callers[:5]:
                print(f"    - {c}")
        print()

print(f"Total own bytes in COM/interop orphans: {total_own}B")

# Check assembly info if available
print("\n=== Assembly info ===")
sample = data["methods"][0]
print(f"Available fields: {list(sample.keys())}")
