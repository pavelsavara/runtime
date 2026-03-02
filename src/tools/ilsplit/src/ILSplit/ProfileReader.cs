// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;

namespace ILSplit;

internal static class ProfileReader
{
    public static HashSet<string> Read(string path)
    {
        HashSet<string> hotClasses = new(StringComparer.Ordinal);

        foreach (string line in File.ReadLines(path))
        {
            string trimmed = line.Trim();
            if (trimmed.Length == 0 || trimmed.StartsWith('#'))
            {
                continue;
            }

            hotClasses.Add(trimmed);
        }

        return hotClasses;
    }
}
