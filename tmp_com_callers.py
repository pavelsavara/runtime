import json

with open("d:/runtime/method-cost-full-callgraph.json") as f:
    data = json.load(f)

by_name = {m["name"]: m for m in data["methods"]}
SCC_SIZE = 163058

# Who calls ComVariant methods?
print("=== ComVariant callers ===")
cv_methods = {"System.Runtime.InteropServices.Marshalling.ComVariant::Dispose()",
              "System.Runtime.InteropServices.Marshalling.ComVariant::GetRawDataRef()",
              "System.Runtime.InteropServices.Marshalling.ComVariant::get_VarType()"}
for m in data["methods"]:
    for c in m.get("topCallees", []):
        if c["name"] in cv_methods:
            in_scc = "SCC" if m["transitiveSize"] == SCC_SIZE else ""
            print(f"  {m['name']} -> {c['name']}  (caller tSize={m['transitiveSize']}) {in_scc}")

# Who calls COMException methods?
print("\n=== COMException callers ===")
ce_methods = set()
for m in data["methods"]:
    if "COMException" in m.get("type", ""):
        ce_methods.add(m["name"])
for m in data["methods"]:
    for c in m.get("topCallees", []):
        if c["name"] in ce_methods:
            in_scc = "SCC" if m["transitiveSize"] == SCC_SIZE else ""
            print(f"  {m['name']} -> {c['name']}  (caller tSize={m['transitiveSize']}) {in_scc}")

# Who calls InvalidOleVariantTypeException?
print("\n=== InvalidOleVariantTypeException callers ===")
iov_methods = set()
for m in data["methods"]:
    if "InvalidOleVariantType" in m.get("type", ""):
        iov_methods.add(m["name"])
for m in data["methods"]:
    for c in m.get("topCallees", []):
        if c["name"] in iov_methods:
            print(f"  {m['name']} -> {c['name']}  (caller tSize={m['transitiveSize']})")

# ExternalException callers (parent of COMException)
print("\n=== ExternalException callers ===")
ee_methods = set()
for m in data["methods"]:
    if "ExternalException" in m.get("type", ""):
        ee_methods.add(m["name"])
for m in data["methods"]:
    for c in m.get("topCallees", []):
        if c["name"] in ee_methods:
            print(f"  {m['name']} -> {c['name']}  (caller tSize={m['transitiveSize']})")

# What does ComVariant.Dispose() call?
print("\n=== ComVariant.Dispose() callees ===")
cv_dispose = by_name.get("System.Runtime.InteropServices.Marshalling.ComVariant::Dispose()")
if cv_dispose:
    for c in cv_dispose.get("topCallees", []):
        print(f"  -> {c['name']} tSize={c['transitiveSize']}")

# What does ComVariant.GetRawDataRef() call?
print("\n=== ComVariant.GetRawDataRef() callees ===")
cv_raw = by_name.get("System.Runtime.InteropServices.Marshalling.ComVariant::GetRawDataRef()")
if cv_raw:
    for c in cv_raw.get("topCallees", []):
        print(f"  -> {c['name']} tSize={c['transitiveSize']}")
