// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace ILSplit;

/// <summary>
/// Parses CoreCLR's corelib.h and namespace.h headers to extract the set
/// of managed types that the VM binds to by name at startup. These types
/// must remain as TypeDefs in the hot cluster so that the VM can find them
/// without following type forwarders.
/// </summary>
internal static partial class VmPinnedTypeParser
{
    // Matches: #define g_FooNS  g_BarNS ".Suffix"
    // or:      #define g_FooNS  "Literal"
    [GeneratedRegex(@"^\s*#define\s+(g_\w+NS)\s+(.*)")]
    private static partial Regex NamespaceDefRegex();

    // Matches: DEFINE_CLASS(ID, Namespace, ClassName)
    // and:     DEFINE_CLASS_U(ID, Namespace, ClassName, UnmanagedStruct)
    [GeneratedRegex(@"^\s*DEFINE_CLASS(?:_U)?\s*\(\s*\w+\s*,\s*(\w+)\s*,\s*([^\s,)]+)\s*[,)]")]
    private static partial Regex DefineClassRegex();

    // Matches: DEFINE_EXCEPTION(g_NamespaceNS, ClassName, bool, HRESULT...)
    // The namespace argument is the full macro name (e.g. g_SystemNS, g_IONS).
    [GeneratedRegex(@"^\s*DEFINE_EXCEPTION\s*\(\s*(g_\w+)\s*,\s*(\w+)\s*,")]
    private static partial Regex DefineExceptionRegex();

    private static readonly char[] s_whitespaceSeparators = [' ', '\t'];

    /// <summary>
    /// Parses namespace.h to build a mapping from short namespace identifiers
    /// (e.g. "System", "Threading", "StubHelpers") to full namespace strings
    /// (e.g. "System", "System.Threading", "System.StubHelpers").
    /// </summary>
    public static Dictionary<string, string> ParseNamespaces(string namespacesHeaderPath)
    {
        Dictionary<string, string> nsDict = new(StringComparer.Ordinal);

        foreach (string line in File.ReadLines(namespacesHeaderPath))
        {
            Match m = NamespaceDefRegex().Match(line);
            if (!m.Success)
            {
                continue;
            }

            string macroName = m.Groups[1].Value; // e.g. g_SystemNS, g_ThreadingNS
            string rhs = m.Groups[2].Value.Trim();

            // Extract short key: strip "g_" prefix and "NS" suffix
            string shortKey = ExtractShortKey(macroName);
            if (shortKey.Length == 0)
            {
                continue;
            }

            // Parse the right-hand side to resolve the full namespace string.
            // Two forms:
            //   "Literal"                        → value is the literal
            //   g_OtherNS ".Suffix"              → value is nsDict[otherKey] + ".Suffix"
            string? resolved = ResolveNamespaceRhs(rhs, nsDict);
            if (resolved is not null)
            {
                nsDict[shortKey] = resolved;
            }
        }

        return nsDict;
    }

    /// <summary>
    /// Parses corelib.h to extract all DEFINE_CLASS type names, using the
    /// namespace dictionary produced by <see cref="ParseNamespaces"/>.
    /// Returns a set of full type names (e.g. "System.Object", "System.StubHelpers.StubHelpers").
    /// </summary>
    public static HashSet<string> ParsePinnedTypes(string corelibHeaderPath, Dictionary<string, string> nsDict)
    {
        HashSet<string> result = new(StringComparer.Ordinal);

        foreach (string line in File.ReadLines(corelibHeaderPath))
        {
            Match m = DefineClassRegex().Match(line);
            if (!m.Success)
            {
                continue;
            }

            string nsShort = m.Groups[1].Value;  // e.g. "System", "Threading", "StubHelpers"
            string className = m.Groups[2].Value; // e.g. "Object", "Thread", "StubHelpers"

            if (nsShort == "NULL" && className == "NULL")
            {
                continue;
            }

            if (!nsDict.TryGetValue(nsShort, out string? fullNs))
            {
                continue;
            }

            // Nested types in corelib.h use '+' separator → convert to '/' for Cecil
            // but for our purposes we need the IL full name with '/' for nested types.
            // However, for matching against Cecil's TypeDefinition.FullName, nested types
            // use '/' separator. Let's keep '+' since that's what corelib.h uses and
            // Cecil uses '/' — we'll handle this at match time.
            result.Add($"{fullNs}.{className}");
        }

        return result;
    }

    /// <summary>
    /// Parses rexcep.h to extract all DEFINE_EXCEPTION type names, using the
    /// namespace dictionary produced by <see cref="ParseNamespaces"/>.
    /// The DEFINE_EXCEPTION macro uses the full namespace macro name (e.g. g_SystemNS)
    /// rather than the short identifier used by DEFINE_CLASS.
    /// </summary>
    public static HashSet<string> ParseExceptionTypes(string rexcepHeaderPath, Dictionary<string, string> nsDict)
    {
        HashSet<string> result = new(StringComparer.Ordinal);

        foreach (string line in File.ReadLines(rexcepHeaderPath))
        {
            Match m = DefineExceptionRegex().Match(line);
            if (!m.Success)
            {
                continue;
            }

            string nsMacro = m.Groups[1].Value;    // e.g. "g_SystemNS", "g_IONS"
            string className = m.Groups[2].Value;  // e.g. "StackOverflowException"

            // Convert macro name to short key: g_SystemNS → System, g_IONS → IO
            string shortKey = ExtractShortKey(nsMacro);
            if (shortKey.Length == 0 || !nsDict.TryGetValue(shortKey, out string? fullNs))
            {
                continue;
            }

            result.Add($"{fullNs}.{className}");
        }

        return result;
    }

    /// <summary>
    /// Convenience method: parses all headers and returns the full set of
    /// VM-pinned type names (DEFINE_CLASS from corelib.h + DEFINE_EXCEPTION from rexcep.h).
    /// </summary>
    public static HashSet<string> Parse(string namespacesHeaderPath, string corelibHeaderPath, string? rexcepHeaderPath = null)
    {
        Dictionary<string, string> nsDict = ParseNamespaces(namespacesHeaderPath);
        HashSet<string> result = ParsePinnedTypes(corelibHeaderPath, nsDict);

        if (rexcepHeaderPath is not null)
        {
            result.UnionWith(ParseExceptionTypes(rexcepHeaderPath, nsDict));
        }

        return result;
    }

    private static string ExtractShortKey(string macroName)
    {
        // g_SystemNS → System, g_ThreadingNS → Threading, g_InternalCompilerHelpersNS → InternalCompilerHelpers
        const string prefix = "g_";
        const string suffix = "NS";

        if (!macroName.StartsWith(prefix) || !macroName.EndsWith(suffix))
        {
            return string.Empty;
        }

        return macroName.Substring(prefix.Length, macroName.Length - prefix.Length - suffix.Length);
    }

    private static string? ResolveNamespaceRhs(string rhs, Dictionary<string, string> nsDict)
    {
        // Case 1: Simple string literal — "System" or "Internal.Runtime.CompilerHelpers"
        if (rhs.StartsWith('"'))
        {
            int end = rhs.IndexOf('"', 1);
            return end > 1 ? rhs.Substring(1, end - 1) : null;
        }

        // Case 2: Composite — g_SystemNS ".Runtime"
        // Split into the macro reference and the string suffix
        int spaceIdx = rhs.IndexOfAny(s_whitespaceSeparators);
        if (spaceIdx < 0)
        {
            return null;
        }

        string refMacro = rhs.Substring(0, spaceIdx).Trim();
        string rest = rhs.Substring(spaceIdx).Trim();

        string refKey = ExtractShortKey(refMacro);
        if (refKey.Length == 0 || !nsDict.TryGetValue(refKey, out string? prefixValue))
        {
            return null;
        }

        // rest should be like ".Runtime" (with quotes)
        int q1 = rest.IndexOf('"');
        int q2 = rest.LastIndexOf('"');
        if (q1 < 0 || q2 <= q1)
        {
            return prefixValue;
        }

        string suffix = rest.Substring(q1 + 1, q2 - q1 - 1);
        return prefixValue + suffix;
    }
}
