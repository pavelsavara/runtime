// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Threading.Tasks;

namespace ILSplit;

internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        Option<FileInfo[]> inputOption = new("--input", "-i")
        {
            Description = "Input assembly paths to split",
            Required = true,
        };

        Option<DirectoryInfo> outputOption = new("--output", "-o")
        {
            Description = "Output directory for split assemblies",
            Required = true,
        };

        Option<FileInfo?> profileOption = new("--profile", "-p")
        {
            Description = "Path to text file with hot class names (one per line)",
        };

        Option<int> minClusterSizeOption = new("--min-cluster-size")
        {
            Description = "Minimum cluster size in bytes (below this, merge with neighbors)",
            DefaultValueFactory = _ => 102400,
        };

        Option<FileInfo?> vmNamespacesHeaderOption = new("--vm-namespaces-header")
        {
            Description = "Path to CoreCLR's namespace.h header for VM-pinned type extraction",
        };

        Option<FileInfo?> vmCorelibHeaderOption = new("--vm-corelib-header")
        {
            Description = "Path to CoreCLR's corelib.h header for VM-pinned type extraction",
        };

        Option<FileInfo?> vmRexcepHeaderOption = new("--vm-rexcep-header")
        {
            Description = "Path to CoreCLR's rexcep.h header for VM-pinned exception type extraction",
        };

        RootCommand rootCommand = new("ILSplit \u2014 splits trimmed .NET assemblies into smaller DLLs for on-demand loading")
        {
            inputOption,
            outputOption,
            profileOption,
            minClusterSizeOption,
            vmNamespacesHeaderOption,
            vmCorelibHeaderOption,
            vmRexcepHeaderOption,
        };

        rootCommand.SetAction(async (parseResult, _) =>
        {
            FileInfo[] inputs = parseResult.GetValue(inputOption)!;
            DirectoryInfo output = parseResult.GetValue(outputOption)!;
            FileInfo? profile = parseResult.GetValue(profileOption);
            int minClusterSize = parseResult.GetValue(minClusterSizeOption);
            FileInfo? vmNamespacesHeader = parseResult.GetValue(vmNamespacesHeaderOption);
            FileInfo? vmCorelibHeader = parseResult.GetValue(vmCorelibHeaderOption);
            FileInfo? vmRexcepHeader = parseResult.GetValue(vmRexcepHeaderOption);

            HashSet<string>? vmPinnedTypes = null;
            if (vmNamespacesHeader is not null && vmCorelibHeader is not null)
            {
                vmPinnedTypes = VmPinnedTypeParser.Parse(
                    vmNamespacesHeader.FullName, vmCorelibHeader.FullName,
                    vmRexcepHeader?.FullName);
            }

            return await SplitEngine.RunAsync(inputs, output, profile, minClusterSize, vmPinnedTypes).ConfigureAwait(false);
        });

        ParseResult parsed = rootCommand.Parse(args);

        return await parsed.InvokeAsync().ConfigureAwait(false);
    }
}
