#!/usr/bin/env python3
"""Analyze the 942-method SCC from method-cost-full.json and categorize into sub-clusters."""
import json
import os
from collections import defaultdict

with open('method-cost-full.json', 'r') as f:
    data = json.load(f)

methods = data['methods']
scc = [m for m in methods if m['transitiveSize'] == 163058]

print(f"SCC: {len(scc)} methods, {sum(m['ownILSize'] for m in scc)} bytes own IL, {sum(m['ownSize'] for m in scc)} bytes total own")
print()

# Group by type
by_type = defaultdict(list)
for m in scc:
    by_type[m['type']].append(m)

type_sizes = [(t, sum(m['ownILSize'] for m in ms), len(ms)) for t, ms in by_type.items()]
type_sizes.sort(key=lambda x: -x[1])

print(f"Distinct types: {len(type_sizes)}")
print()

# Sub-cluster classification
def classify(type_name):
    """Classify a type into a sub-cluster based on its full name."""
    t = type_name

    # 1F: Runtime Intrinsics & Numerics
    if 'System.Runtime.Intrinsics' in t:
        if 'Scalar' in t:
            return '1F-i-Scalar'
        if 'Vector128' in t or 'Vector256' in t or 'Vector512' in t or 'Vector64' in t:
            return '1F-ii-Vector'
        if 'PackedSimd' in t or 'WasmBase' in t:
            return '1F-ii-Vector'
        return '1F-ii-Vector'
    if 'System.Numerics' in t:
        if 'Vector`1' in t or 'Vector<' in t:
            return '1F-iii-GenericNumerics'
        if 'BitOperations' in t:
            return '1F-iii-GenericNumerics'
        return '1F-iii-GenericNumerics'

    # 1D: Reflection.Emit
    if 'System.Reflection.Emit' in t:
        if 'TypeBuilder' in t or 'EnumBuilder' in t or 'GenericTypeParameter' in t:
            return '1D-i-TypeConstruction'
        if 'ILGenerator' in t or 'DynamicMethod' in t or 'DynamicIL' in t or 'DynamicResolver' in t or 'DynamicScope' in t:
            return '1D-ii-ILGeneration'
        return '1D-iii-EmitSupport'

    # 1C: Reflection (but not Emit)
    if 'System.Reflection' in t:
        if 'Metadata' in t:
            return '1C-iv-Metadata'
        if 'RuntimeType' in t or 'Type' == t.split('.')[-1] or 'TypeHandle' in t or 'RuntimeTypeCache' in t or 'MemberInfoCache' in t:
            return '1C-i-TypeSystem'
        if 'Method' in t or 'Field' in t or 'Property' in t or 'Constructor' in t or 'Invoker' in t or 'CustomAttribute' in t or 'Binder' in t or 'ParameterInfo' in t:
            return '1C-ii-Members'
        if 'Assembly' in t or 'Module' in t or 'AssemblyName' in t:
            return '1C-iii-Assembly'
        return '1C-ii-Members'

    # 1B: Globalization
    if 'System.Globalization' in t:
        if 'CultureInfo' in t or 'CultureData' in t or 'GlobalizationMode' in t:
            return '1B-i-CultureInfra'
        if 'NumberFormat' in t or 'DateTimeFormat' in t or 'TimeSpanFormat' in t or 'TimeSpanParse' in t:
            return '1B-ii-Formatting'
        return '1B-ii-Formatting'

    # 1E: Text & Encoding
    if 'System.Text' in t and 'Json' not in t:
        if 'StringBuilder' in t or 'ValueStringBuilder' in t or 'InterpolatedStringHandler' in t:
            return '1E-i-StringBuilder'
        if 'Encoding' in t or 'Encoder' in t or 'Decoder' in t or 'Fallback' in t:
            return '1E-ii-Encoding'
        if 'Ascii' in t or 'Utf8' in t or 'Utf16' in t or 'Rune' in t or 'Unicode' in t:
            return '1E-iii-Unicode'
        return '1E-iii-Unicode'

    # 1H: Threading & Tasks
    if 'System.Threading' in t:
        if 'Task' in t or 'ValueTask' in t or 'Awaiter' in t or 'AsyncMethodBuilder' in t:
            return '1H-iii-Async'
        if 'Thread' == t.split('.')[-1] or 'ThreadPool' in t or 'Monitor' in t or 'Lock' in t:
            return '1H-i-ThreadPrimitives'
        if 'SemaphoreSlim' in t or 'WaitHandle' in t or 'ManualResetEvent' in t or 'CancellationToken' in t or 'Timer' in t:
            return '1H-ii-Synchronization'
        return '1H-i-ThreadPrimitives'

    # 1G: Collections
    if 'System.Collections' in t:
        if 'Dictionary' in t or 'Hashtable' in t or 'HashSet' in t:
            return '1G-i-Dictionary'
        if 'Comparer' in t or 'EqualityComparer' in t or 'NonRandomized' in t:
            return '1G-ii-Comparer'
        if 'List' in t or 'Queue' in t or 'ReadOnly' in t or 'ValueListBuilder' in t:
            return '1G-iii-Lists'
        return '1G-i-Dictionary'

    # 1J: Buffers & Search
    if 'System.Buffers' in t:
        if 'ArrayPool' in t or 'SharedArrayPool' in t:
            return '1J-i-ArrayPool'
        if 'SearchValues' in t or 'IndexOfAny' in t or 'ProbabilisticMap' in t or 'AsciiChar' in t:
            return '1J-ii-SearchValues'
        return '1J-iii-BinaryPrimitives'

    # 1K: Runtime CompilerServices
    if 'System.Runtime.CompilerServices' in t:
        if 'RuntimeHelpers' in t or 'CastHelpers' in t or 'CastCache' in t or 'MethodTable' in t or 'TypeHandle' in t:
            return '1K-i-RuntimeHelpers'
        if 'AsyncTaskMethodBuilder' in t or 'AsyncValueTaskMethodBuilder' in t or 'Pooling' in t:
            return '1K-ii-AsyncBuilders'
        return '1K-iii-Other'

    # 1L: Interop & Marshalling
    if 'System.Runtime.InteropServices' in t:
        if 'Marshal' in t and 'Marshalling' not in t:
            return '1L-i-Marshal'
        if 'SafeHandle' in t or 'GCHandle' in t or 'NativeMemory' in t or 'NativeLibrary' in t:
            return '1L-ii-SafeHandle'
        if 'Marshalling' in t:
            return '1L-iii-Marshalling'
        return '1L-i-Marshal'
    if 'Microsoft.Win32.SafeHandles' in t:
        return '1L-ii-SafeHandle'

    # 1I: IO & FileSystem
    if 'System.IO' in t:
        if 'Stream' in t or 'BinaryReader' in t or 'MemoryStream' in t or 'UnmanagedMemory' in t:
            return '1I-i-Streams'
        if 'File' in t or 'Directory' in t or 'Path' in t:
            return '1I-ii-FileSystem'
        return '1I-i-Streams'

    # 1M: Diagnostics
    if 'System.Diagnostics' in t:
        if 'StackTrace' in t or 'StackFrame' in t:
            return '1M-i-StackTrace'
        return '1M-ii-EventSource'

    # 1N: Resources & Serialization
    if 'ResourceManager' in t or 'ResourceReader' in t:
        return '1N-i-Resources'
    if 'Serialization' in t:
        return '1N-ii-Serialization'

    # Reflection types in System namespace (before general System.* catch)
    reflection_in_system = {'RuntimeType', 'Type', 'RuntimeTypeHandle', 'RuntimeMethodHandle',
                            'RuntimeFieldHandle', 'DefaultBinder', 'Activator', 'SignatureType',
                            'SignatureConstructedGenericType', 'SignatureArrayType', 'SignaturePointerType',
                            'SignatureByRefType', 'SignatureHasElementType'}
    type_base = t.split('.')[-1].split('/')[0].split('`')[0]
    # Also check the top-level type for nested types like System.RuntimeType/RuntimeTypeCache
    top_type = t.split('/')[0].split('.')[-1].split('`')[0] if '/' in t else type_base
    if t.startswith('System.') and (type_base in reflection_in_system or top_type in reflection_in_system):
        if type_base in ('RuntimeType', 'Type', 'RuntimeTypeHandle', 'SignatureType',
                         'SignatureConstructedGenericType', 'SignatureArrayType', 'SignaturePointerType',
                         'SignatureByRefType', 'SignatureHasElementType'):
            return '1C-i-TypeSystem'
        if type_base == 'DefaultBinder':
            return '1C-ii-Members'
        if type_base == 'Activator':
            return '1C-ii-Members'
        return '1C-i-TypeSystem'

    # 1A: System Primitives
    if t.startswith('System.') and '.' not in t[7:].replace('`1','').replace('`2',''):
        # Direct System.XXX types
        name = t.split('/')[-1].split('`')[0]
        # Numeric types
        numeric_types = {'Int16','Int32','Int64','Int128','UInt16','UInt32','UInt64','UInt128',
                         'Single','Double','Half','Decimal','Byte','SByte','Number','Convert',
                         'Math','MathF','BitConverter','Boolean','Char','IntPtr','UIntPtr','NFloat'}
        if type_base in numeric_types or 'Number' in t:
            return '1A-i-Numeric'
        string_types = {'String','Enum','Array','Object','ValueType','Delegate','MulticastDelegate',
                        'Guid','DateTime','TimeSpan','TimeZoneInfo','DateOnly','TimeOnly','DateTimeOffset',
                        'Range','Index','Attribute','Version','Uri','Console','Environment',
                        'TypedReference','ArgIterator','Tuple','ValueTuple','Span','ReadOnlySpan',
                        'Memory','ReadOnlyMemory','MemoryExtensions'}
        if type_base in string_types:
            return '1A-ii-CoreTypes'
        infra_types = {'ThrowHelper','SR','Buffer','SpanHelpers','HashCode','Marvin','Random',
                       'Exception','SystemException','ArgumentException','ArgumentNullException',
                       'ArgumentOutOfRangeException','InvalidOperationException','NotSupportedException',
                       'FormatException','OverflowException','InvalidCastException','NullReferenceException',
                       'IndexOutOfRangeException','ArrayTypeMismatchException','PlatformNotSupportedException',
                       'NotImplementedException','ObjectDisposedException','TypeLoadException',
                       'MissingMethodException','MissingFieldException','MissingMemberException',
                       'FieldAccessException','MethodAccessException','TypeAccessException',
                       'BadImageFormatException','OutOfMemoryException','StackOverflowException',
                       'AccessViolationException','ApplicationException','AggregateException',
                       'OperationCanceledException','TimeoutException','ArithmeticException',
                       'DivideByZeroException','EntryPointNotFoundException','DllNotFoundException',
                       'MulticastNotSupportedException','RankException','TypeInitializationException',
                       'TypeUnloadedException','UnauthorizedAccessException','ExecutionEngineException',
                       'InsufficientExecutionStackException','InsufficientMemoryException',
                       'AppDomain','GC','WeakReference','Lazy','EventArgs','EventHandler',
                       'Action','Func','Predicate','Comparison','Converter','IFormatProvider',
                       'IFormattable','ISpanFormattable','IUtf8SpanFormattable','IComparable',
                       'IConvertible','IEquatable','FormattableString','ParamsArray'}
        if type_base in infra_types:
            return '1A-iii-Infrastructure'
        # Fallback for System.*
        return '1A-ii-CoreTypes'

    # 1K fallback for RuntimeHelpers etc in non-standard namespace
    if 'RuntimeHelpers' in t or 'CastHelpers' in t:
        return '1K-i-RuntimeHelpers'

    # 1N: SR/resources
    if t == 'System.SR' or t == 'Interop':
        return '1A-iii-Infrastructure'

    # Interop types (native interop stubs)
    if t.startswith('Interop'):
        return '1L-i-Marshal'

    # Loader
    if 'AssemblyLoadContext' in t or 'System.Runtime.Loader' in t:
        return '1C-iii-Assembly'

    return 'UNKNOWN'

# Classify all types
cluster_methods = defaultdict(list)
cluster_types = defaultdict(set)
for t, ms in by_type.items():
    cluster = classify(t)
    cluster_methods[cluster].extend(ms)
    cluster_types[cluster].add(t)

# Print summary
print("=" * 100)
print(f"{'Sub-Cluster':<30} {'Methods':>7} {'Own IL':>8} {'Types':>5}")
print("=" * 100)

clusters_sorted = sorted(cluster_methods.items(), key=lambda x: -sum(m['ownILSize'] for m in x[1]))
for cluster, ms in clusters_sorted:
    il = sum(m['ownILSize'] for m in ms)
    print(f"  {cluster:<28} {len(ms):>7} {il:>7}B {len(cluster_types[cluster]):>5}")
print("=" * 100)
total_m = sum(len(ms) for _, ms in cluster_methods.items())
total_il = sum(sum(m['ownILSize'] for m in ms) for _, ms in cluster_methods.items())
print(f"  {'TOTAL':<28} {total_m:>7} {total_il:>7}B")
print()

# Now print each cluster with its types and methods
for cluster, ms in clusters_sorted:
    il = sum(m['ownILSize'] for m in ms)
    print()
    print(f"### {cluster} ({len(ms)} methods, {il} bytes)")
    print()
    # Group by type within cluster
    ct = defaultdict(list)
    for m in ms:
        ct[m['type']].append(m)
    ct_sorted = sorted(ct.items(), key=lambda x: -sum(m['ownILSize'] for m in x[1]))
    for t, tms in ct_sorted:
        til = sum(m['ownILSize'] for m in tms)
        print(f"  {t} ({len(tms)}m, {til}B)")
        for m in sorted(tms, key=lambda x: -x['ownILSize']):
            print(f"    {m['ownILSize']:>5}B  {m['name']}")
