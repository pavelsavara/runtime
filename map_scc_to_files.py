#!/usr/bin/env python3
"""Map SCC types to their source files and produce the full sub-cluster inventory."""
import json
import os
import glob
import re
from collections import defaultdict

REPO_ROOT = r'd:\runtime2'
SPC_LIB = os.path.join(REPO_ROOT, 'src', 'libraries', 'System.Private.CoreLib', 'src')
SPC_CLR = os.path.join(REPO_ROOT, 'src', 'coreclr', 'System.Private.CoreLib', 'src')

with open(os.path.join(REPO_ROOT, 'method-cost-full.json'), 'r') as f:
    data = json.load(f)

scc = [m for m in data['methods'] if m['transitiveSize'] == 163058]
types_in_scc = sorted(set(m['type'] for m in scc))

print(f"Mapping {len(types_in_scc)} distinct types to source files...")
print()

# Build an index of all .cs files under both source trees
cs_files_lib = glob.glob(os.path.join(SPC_LIB, '**', '*.cs'), recursive=True)
cs_files_clr = glob.glob(os.path.join(SPC_CLR, '**', '*.cs'), recursive=True)
all_cs = cs_files_lib + cs_files_clr

print(f"Found {len(cs_files_lib)} .cs files in libraries SPC")
print(f"Found {len(cs_files_clr)} .cs files in coreclr SPC")
print()

def type_to_search_names(type_name):
    """Convert a type like 'System.RuntimeType/RuntimeTypeCache/MemberInfoCache`1' to search patterns."""
    # Strip generic arity
    clean = re.sub(r'`\d+', '', type_name)
    # Get the innermost type name (after last /)
    parts = clean.split('/')
    innermost = parts[-1]
    # Also get top-level type
    top = parts[0].split('.')[-1]
    return [innermost, top] if innermost != top else [top]

def find_source_files(type_name):
    """Find .cs files likely containing a type definition."""
    search_names = type_to_search_names(type_name)
    results = []

    for name in search_names:
        # First: try exact filename match
        for f in all_cs:
            basename = os.path.basename(f).replace('.cs', '')
            # Handle files like RuntimeType.cs, RuntimeType.CoreCLR.cs, etc
            if basename == name or basename.startswith(name + '.'):
                results.append(f)

    # Deduplicate
    return sorted(set(results))

# Map every type
type_files = {}
unmapped = []
for t in types_in_scc:
    files = find_source_files(t)
    if files:
        type_files[t] = files
    else:
        unmapped.append(t)

print(f"Mapped: {len(type_files)} types")
print(f"Unmapped: {len(unmapped)} types")
print()

if unmapped:
    print("Unmapped types:")
    for t in unmapped:
        search = type_to_search_names(t)
        print(f"  {t}  (searched: {search})")
    print()

# Now output the mapping grouped by sub-cluster
# Reuse the classify function
def classify(type_name):
    t = type_name
    if 'System.Runtime.Intrinsics' in t:
        if 'Scalar' in t: return '1F-i-Scalar'
        return '1F-ii-Vector'
    if 'System.Numerics' in t:
        return '1F-iii-GenericNumerics'
    if 'System.Reflection.Emit' in t:
        if 'TypeBuilder' in t or 'EnumBuilder' in t or 'GenericTypeParameter' in t: return '1D-i-TypeConstruction'
        if 'ILGenerator' in t or 'DynamicMethod' in t or 'DynamicIL' in t or 'DynamicResolver' in t or 'DynamicScope' in t: return '1D-ii-ILGeneration'
        return '1D-iii-EmitSupport'
    if 'System.Reflection' in t:
        if 'Metadata' in t: return '1C-iv-Metadata'
        if 'RuntimeType' in t or 'MemberInfoCache' in t: return '1C-i-TypeSystem'
        if 'Method' in t or 'Field' in t or 'Property' in t or 'Constructor' in t or 'Invoker' in t or 'CustomAttribute' in t or 'Binder' in t or 'ParameterInfo' in t: return '1C-ii-Members'
        if 'Assembly' in t or 'Module' in t or 'AssemblyName' in t: return '1C-iii-Assembly'
        return '1C-ii-Members'
    if 'System.Globalization' in t:
        if 'CultureInfo' in t or 'CultureData' in t or 'GlobalizationMode' in t: return '1B-i-CultureInfra'
        return '1B-ii-Formatting'
    if 'System.Text' in t and 'Json' not in t:
        if 'StringBuilder' in t or 'ValueStringBuilder' in t or 'InterpolatedStringHandler' in t: return '1E-i-StringBuilder'
        if 'Encoding' in t or 'Encoder' in t or 'Decoder' in t or 'Fallback' in t: return '1E-ii-Encoding'
        return '1E-iii-Unicode'
    if 'System.Threading' in t:
        if 'Task' in t or 'ValueTask' in t or 'Awaiter' in t or 'AsyncMethodBuilder' in t: return '1H-iii-Async'
        if any(x in t.split('.')[-1].split('/')[0] for x in ['Thread', 'ThreadPool', 'Monitor', 'Lock']): return '1H-i-ThreadPrimitives'
        return '1H-ii-Synchronization'
    if 'System.Collections' in t:
        if 'Dictionary' in t or 'Hashtable' in t or 'HashSet' in t: return '1G-i-Dictionary'
        if 'Comparer' in t or 'EqualityComparer' in t or 'NonRandomized' in t: return '1G-ii-Comparer'
        return '1G-iii-Lists'
    if 'System.Buffers' in t:
        if 'ArrayPool' in t or 'SharedArrayPool' in t: return '1J-i-ArrayPool'
        if 'SearchValues' in t or 'IndexOfAny' in t or 'ProbabilisticMap' in t or 'AsciiChar' in t: return '1J-ii-SearchValues'
        return '1J-iii-BinaryPrimitives'
    if 'System.Runtime.CompilerServices' in t:
        if 'RuntimeHelpers' in t or 'CastHelpers' in t or 'CastCache' in t: return '1K-i-RuntimeHelpers'
        if 'AsyncTaskMethodBuilder' in t or 'AsyncValueTaskMethodBuilder' in t or 'Pooling' in t: return '1K-ii-AsyncBuilders'
        return '1K-iii-Other'
    if 'System.Runtime.InteropServices' in t:
        if 'Marshalling' in t: return '1L-iii-Marshalling'
        if 'Marshal' in t: return '1L-i-Marshal'
        if 'SafeHandle' in t or 'GCHandle' in t or 'NativeMemory' in t or 'NativeLibrary' in t: return '1L-ii-SafeHandle'
        return '1L-i-Marshal'
    if 'Microsoft.Win32.SafeHandles' in t: return '1L-ii-SafeHandle'
    if 'System.IO' in t:
        if 'Stream' in t or 'BinaryReader' in t: return '1I-i-Streams'
        if 'File' in t or 'Directory' in t or 'Path' in t: return '1I-ii-FileSystem'
        return '1I-i-Streams'
    if 'System.Diagnostics' in t:
        if 'StackTrace' in t or 'StackFrame' in t: return '1M-i-StackTrace'
        return '1M-ii-EventSource'
    if 'ResourceManager' in t or 'ResourceReader' in t: return '1N-i-Resources'
    if 'Serialization' in t: return '1N-ii-Serialization'

    # Reflection types in System namespace
    reflection_types = {'RuntimeType', 'Type', 'RuntimeTypeHandle', 'RuntimeMethodHandle',
                        'RuntimeFieldHandle', 'DefaultBinder', 'Activator'}
    type_base = t.split('.')[-1].split('/')[0].split('`')[0]
    top_type = t.split('/')[0].split('.')[-1].split('`')[0] if '/' in t else type_base
    if t.startswith('System.') and (type_base in reflection_types or top_type in reflection_types):
        if type_base in ('RuntimeType', 'Type', 'RuntimeTypeHandle') or top_type == 'RuntimeType':
            return '1C-i-TypeSystem'
        return '1C-ii-Members'

    if t.startswith('System.'):
        numeric = {'Int16','Int32','Int64','Int128','UInt16','UInt32','UInt64','UInt128',
                   'Single','Double','Half','Decimal','Byte','SByte','Number','Convert',
                   'Math','MathF','BitConverter','Boolean','Char','IntPtr','UIntPtr','NFloat'}
        if type_base in numeric or 'Number' in t: return '1A-i-Numeric'
        core = {'String','Enum','Array','Object','ValueType','Delegate','MulticastDelegate',
                'Guid','DateTime','TimeSpan','TimeZoneInfo','DateOnly','TimeOnly','DateTimeOffset',
                'Range','Index','Attribute','Version','Uri','Console','Environment',
                'Span','ReadOnlySpan','Memory','ReadOnlyMemory','MemoryExtensions'}
        if type_base in core: return '1A-ii-CoreTypes'
        infra = {'ThrowHelper','SR','Buffer','SpanHelpers','HashCode','Marvin','Random',
                 'Exception','SystemException','ArgumentException','ArgumentNullException',
                 'ArgumentOutOfRangeException','InvalidOperationException','NotSupportedException',
                 'FormatException','OverflowException','AppDomain','GC','WeakReference','Lazy',
                 'EventArgs','EventHandler','Action','Func','FormattableString','ParamsArray'}
        if type_base in infra: return '1A-iii-Infrastructure'
        # Check if it's an exception type
        if 'Exception' in type_base: return '1A-iii-Infrastructure'
        return '1A-ii-CoreTypes'

    if t.startswith('Interop'): return '1L-i-Marshal'
    if 'AssemblyLoadContext' in t: return '1C-iii-Assembly'
    return 'UNKNOWN'


# Group by cluster
by_cluster = defaultdict(lambda: defaultdict(dict))
for m in scc:
    cluster = classify(m['type'])
    if m['type'] not in by_cluster[cluster]:
        by_cluster[cluster][m['type']] = {'methods': [], 'files': type_files.get(m['type'], [])}
    by_cluster[cluster][m['type']]['methods'].append(m)

def rel_path(p):
    return os.path.relpath(p, REPO_ROOT).replace('\\', '/')

# Sort clusters
cluster_order = sorted(by_cluster.keys(), key=lambda c: -sum(
    sum(m['ownILSize'] for m in info['methods'])
    for info in by_cluster[c].values()
))

# Print final inventory
print("=" * 120)
print(f"{'Sub-Cluster':<30} {'Methods':>7} {'Own IL':>8} {'Types':>5}  Key Source Files")
print("=" * 120)

for cluster in cluster_order:
    types = by_cluster[cluster]
    methods_count = sum(len(info['methods']) for info in types.values())
    il_size = sum(sum(m['ownILSize'] for m in info['methods']) for info in types.values())
    type_count = len(types)
    # Collect unique source files
    all_files = set()
    for info in types.values():
        all_files.update(info['files'])
    file_preview = ', '.join(os.path.basename(f) for f in sorted(all_files)[:4])
    if len(all_files) > 4:
        file_preview += f' (+{len(all_files)-4})'
    print(f"  {cluster:<28} {methods_count:>7} {il_size:>7}B {type_count:>5}  {file_preview}")

total_m = sum(len(info['methods']) for types in by_cluster.values() for info in types.values())
total_il = sum(sum(m['ownILSize'] for m in info['methods']) for types in by_cluster.values() for info in types.values())
print("=" * 120)
print(f"  {'TOTAL':<28} {total_m:>7} {total_il:>7}B")
print()

# Detailed output per cluster
for cluster in cluster_order:
    types = by_cluster[cluster]
    methods_count = sum(len(info['methods']) for info in types.values())
    il_size = sum(sum(m['ownILSize'] for m in info['methods']) for info in types.values())
    all_files = set()
    for info in types.values():
        all_files.update(info['files'])

    print()
    print(f"### {cluster} ({methods_count} methods, {il_size:,} bytes, {len(types)} types)")
    if all_files:
        print(f"Source files:")
        for f in sorted(all_files):
            print(f"  {rel_path(f)}")
    print()

    # Sort types by IL size
    type_list = sorted(types.items(), key=lambda x: -sum(m['ownILSize'] for m in x[1]['methods']))
    for t, info in type_list:
        til = sum(m['ownILSize'] for m in info['methods'])
        files_str = ', '.join(os.path.basename(f) for f in info['files'])
        print(f"  {t} ({len(info['methods'])}m, {til}B) [{files_str}]")
        for m in sorted(info['methods'], key=lambda x: -x['ownILSize'])[:10]:  # Top 10 methods
            print(f"    {m['ownILSize']:>5}B  {m['name']}")
        if len(info['methods']) > 10:
            remaining = len(info['methods']) - 10
            remaining_il = sum(m['ownILSize'] for m in sorted(info['methods'], key=lambda x: -x['ownILSize'])[10:])
            print(f"    ... +{remaining} more ({remaining_il}B)")
