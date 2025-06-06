// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace System.Reflection
{
    internal static class AssemblyNameFormatter
    {
        public static string ComputeDisplayName(string name, Version? version, string? cultureName, byte[]? pkt, AssemblyNameFlags flags = 0, AssemblyContentType contentType = 0, byte[]? pk = null)
        {
            ValueStringBuilder vsb = new(stackalloc char[256]);
            AppendDisplayName(ref vsb, name, version, cultureName, pkt, flags, contentType, pk);
            return vsb.ToString();
        }

        public static void AppendDisplayName(ref ValueStringBuilder vsb, string name, Version? version, string? cultureName, byte[]? pkt, AssemblyNameFlags flags = 0, AssemblyContentType contentType = 0, byte[]? pk = null)
        {
            const int PUBLIC_KEY_TOKEN_LEN = 8;
            Debug.Assert(name.Length != 0);

            vsb.AppendQuoted(name);

            if (version != null)
            {
                ushort major = (ushort)version.Major;
                if (major != ushort.MaxValue)
                {
                    vsb.Append(", Version=");
                    vsb.AppendSpanFormattable(major);

                    ushort minor = (ushort)version.Minor;
                    if (minor != ushort.MaxValue)
                    {
                        vsb.Append('.');
                        vsb.AppendSpanFormattable(minor);

                        ushort build = (ushort)version.Build;
                        if (build != ushort.MaxValue)
                        {
                            vsb.Append('.');
                            vsb.AppendSpanFormattable(build);

                            ushort revision = (ushort)version.Revision;
                            if (revision != ushort.MaxValue)
                            {
                                vsb.Append('.');
                                vsb.AppendSpanFormattable(revision);
                            }
                        }
                    }
                }
            }

            if (cultureName != null)
            {
                if (cultureName.Length == 0)
                    cultureName = "neutral";
                vsb.Append(", Culture=");
                vsb.AppendQuoted(cultureName);
            }

            byte[]? keyOrToken = pkt ?? pk;
            if (keyOrToken != null)
            {
                if (pkt != null)
                {
                    if (pkt.Length > PUBLIC_KEY_TOKEN_LEN)
                        throw new ArgumentException();

                    vsb.Append(", PublicKeyToken=");
                }
                else
                {
                    vsb.Append(", PublicKey=");
                }

                if (keyOrToken.Length == 0)
                {
                    vsb.Append("null");
                }
                else
                {
                    HexConverter.EncodeToUtf16(keyOrToken, vsb.AppendSpan(keyOrToken.Length * 2), HexConverter.Casing.Lower);
                }
            }

            if (0 != (flags & AssemblyNameFlags.Retargetable))
                vsb.Append(", Retargetable=Yes");

            if (contentType == AssemblyContentType.WindowsRuntime)
                vsb.Append(", ContentType=WindowsRuntime");

            // NOTE: By design (desktop compat) AssemblyName.FullName and ToString() do not include ProcessorArchitecture.
        }

        private static void AppendQuoted(this ref ValueStringBuilder vsb, string s)
        {
            bool needsQuoting = false;
            const char quoteChar = '\"';

            // App-compat: You can use double or single quotes to quote a name, and Fusion (or rather the IdentityAuthority) picks one
            // by some algorithm. Rather than guess at it, we use double quotes consistently.
            ReadOnlySpan<char> span = s.AsSpan();
            if (s.Length != span.Trim().Length || span.IndexOfAny('\"', '\'') >= 0)
                needsQuoting = true;

            if (needsQuoting)
                vsb.Append(quoteChar);

            for (int i = 0; i < s.Length; i++)
            {
                switch (s[i])
                {
                    case '\\':
                    case ',':
                    case '=':
                    case '\'':
                    case '"':
                        vsb.Append('\\');
                        break;
                    case '\t':
                        vsb.Append("\\t");
                        continue;
                    case '\r':
                        vsb.Append("\\r");
                        continue;
                    case '\n':
                        vsb.Append("\\n");
                        continue;
                }

                vsb.Append(s[i]);
            }

            if (needsQuoting)
                vsb.Append(quoteChar);
        }

#if !NET
        private static void AppendSpanFormattable(this ref ValueStringBuilder vsb, ushort value)
        {
            vsb.Append(value.ToString());
        }
#endif
    }
}
