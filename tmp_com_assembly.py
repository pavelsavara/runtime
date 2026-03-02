import json

with open("d:/runtime/method-cost-full-callgraph.json") as f:
    data = json.load(f)

# Check which assemblies these COM types come from
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

print("=== Assembly provenance ===")
for tp in com_types:
    methods = [m for m in data["methods"] if m.get("type", "") == tp]
    assemblies = set(m.get("assembly", "?") for m in methods)
    print(f"  {tp}: {assemblies}")

# Also check for any BuiltInComInterop feature switch types
print("\n=== BuiltInComInterop feature switch ===")
for m in data["methods"]:
    if "BuiltInCom" in m["name"] or "EnableComInterop" in m["name"]:
        print(f"  {m['name']} own={m['ownSize']} tSize={m['transitiveSize']}")

# Check what ComVariant.Dispose does that's heavy (1037B own!)
print("\n=== ComVariant.Dispose callees (deep) ===")
by_name = {m["name"]: m for m in data["methods"]}
dispose = by_name.get("System.Runtime.InteropServices.Marshalling.ComVariant::Dispose()")
if dispose:
    print(f"  own={dispose['ownSize']}B, tSize={dispose['transitiveSize']}B")
    # Show how many methods it transitively reaches
    print(f"  transitive method count: {dispose['transitiveMethodCount']}")

# COMException.ToString callees — why is it 217KB transitive?
print("\n=== COMException.ToString callees ===")
tostr = by_name.get("System.Runtime.InteropServices.COMException::ToString()")
if tostr:
    print(f"  own={tostr['ownSize']}B, tSize={tostr['transitiveSize']}B")
    for c in tostr.get("topCallees", []):
        print(f"  -> {c['name']} tSize={c['transitiveSize']}")
