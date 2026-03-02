import json

with open("d:/runtime/method-cost-full-callgraph.json") as f:
    data = json.load(f)

SCC_SIZE = 163058

# Targeted COM/interop types
targets = ["COMException", "ComImportAttribute", "ComVariant", "ComponentActivator"]
for t in targets:
    matches = [m for m in data["methods"] if t in m.get("type", "")]
    if matches:
        types = {}
        for m in matches:
            types.setdefault(m["type"], []).append(m)
        for tp, methods in sorted(types.items()):
            total_own = sum(m["ownSize"] for m in methods)
            max_ts = max(m["transitiveSize"] for m in methods)
            in_scc = any(m["transitiveSize"] == SCC_SIZE for m in methods)
            print(f"{tp}: {len(methods)} methods, {total_own}B own, maxTS={max_ts}, inSCC={in_scc}")
            for m in methods:
                print(f"  {m['name']} own={m['ownSize']} tSize={m['transitiveSize']}")

# Marshal
print("\n=== Marshal ===")
marshal = [m for m in data["methods"] if m.get("type", "") == "System.Runtime.InteropServices.Marshal"]
if marshal:
    total_own = sum(m["ownSize"] for m in marshal)
    max_ts = max(m["transitiveSize"] for m in marshal)
    in_scc = any(m["transitiveSize"] == SCC_SIZE for m in marshal)
    print(f"Marshal: {len(marshal)} methods, {total_own}B own, maxTS={max_ts}, inSCC={in_scc}")
    for m in sorted(marshal, key=lambda x: -x["transitiveSize"])[:20]:
        print(f"  {m['name']} own={m['ownSize']} tSize={m['transitiveSize']}")
else:
    print("No Marshal methods found")

# Search for methods with "COM" or "com" in the method name (not type)
print("\n=== Methods with COM/Interop in name ===")
com_name = [m for m in data["methods"] if any(kw in m["name"] for kw in 
    ["GetObjectForIUnknown", "GetIUnknownForObject", "ReleaseComObject", 
     "GetComObject", "CreateWrapperOfType", "IsComObject",
     "GetTypedObjectForIUnknown", "ChangeWrapperHandleStrength"])]
for m in com_name:
    print(f"  {m['name']} own={m['ownSize']} tSize={m['transitiveSize']}")

# Search for ObjCRuntime / Swift
print("\n=== ObjC/Swift ===")
objc = [m for m in data["methods"] if "ObjC" in m.get("type", "") or "Swift" in m.get("type", "")]
for m in objc:
    print(f"  {m['name']} own={m['ownSize']} tSize={m['transitiveSize']}")

# Search for DisableRuntimeMarshalling
print("\n=== DisableRuntimeMarshalling ===")
drm = [m for m in data["methods"] if "DisableRuntimeMarshalling" in m.get("type", "")]
for m in drm:
    print(f"  {m['name']} own={m['ownSize']} tSize={m['transitiveSize']}")

# Search for Variant / VARIANT
print("\n=== Variant ===")
var = [m for m in data["methods"] if "Variant" in m.get("type", "")]
for m in var:
    print(f"  {m['name']} own={m['ownSize']} tSize={m['transitiveSize']}")

# DllImport / PInvoke related
print("\n=== DllImport/PInvoke ===")
dllimport = [m for m in data["methods"] if m.get("type", "") in [
    "System.Runtime.InteropServices.DllImportAttribute",
    "System.Runtime.InteropServices.DllImportSearchPath"
]]
for m in dllimport:
    print(f"  {m['name']} own={m['ownSize']} tSize={m['transitiveSize']}")

# Search for anything with "interop" in type name (case insensitive)
print("\n=== Types under System.Runtime.InteropServices (non-CompilerServices) ===")
interop = [m for m in data["methods"] if m.get("type", "").startswith("System.Runtime.InteropServices")]
interop_types = {}
for m in interop:
    interop_types.setdefault(m["type"], []).append(m)
for tp in sorted(interop_types.keys()):
    methods = interop_types[tp]
    total_own = sum(m["ownSize"] for m in methods)
    max_ts = max(m["transitiveSize"] for m in methods)
    in_scc = any(m["transitiveSize"] == SCC_SIZE for m in methods)
    print(f"  {tp}: {len(methods)} methods, {total_own}B own, maxTS={max_ts}, inSCC={in_scc}")
