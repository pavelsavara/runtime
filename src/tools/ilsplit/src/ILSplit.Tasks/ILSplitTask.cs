// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
#if NET
using Mono.Cecil;
#endif

namespace ILSplit.Tasks;

public sealed class ILSplitTask : Task
{
    [Required]
    public ITaskItem[] InputAssemblies { get; set; } = [];

    public string? HotClassListPath { get; set; }

    [Required]
    public string OutputDirectory { get; set; } = string.Empty;

    public int MinClusterSize { get; set; } = 102400;

    public ITaskItem[]? AssembliesToSplit { get; set; }

    /// <summary>Path to CoreCLR's namespace.h header for VM-pinned type extraction.</summary>
    public string? VmNamespacesHeaderPath { get; set; }

    /// <summary>Path to CoreCLR's corelib.h header for VM-pinned type extraction.</summary>
    public string? VmCorelibHeaderPath { get; set; }

    [Output]
    public ITaskItem[] SplitAssemblies { get; set; } = [];

    [Output]
    public string ManifestPath { get; set; } = string.Empty;

    public override bool Execute()
    {
#if !NET
        Log.LogError("ILSplit requires the .NET Core MSBuild host.");
        return false;
#else
        if (InputAssemblies.Length == 0)
        {
            Log.LogMessage(MessageImportance.Normal, "ILSplit: No input assemblies specified.");
            return true;
        }

        Directory.CreateDirectory(OutputDirectory);

        HashSet<string> splitSet = BuildSplitSet();
        HashSet<string>? hotClasses = ReadProfile();
        Log.LogMessage(MessageImportance.High,
            "ILSplit: VmCorelibHeaderPath='{0}', VmNamespacesHeaderPath='{1}'",
            VmCorelibHeaderPath ?? "(null)", VmNamespacesHeaderPath ?? "(null)");
        HashSet<string>? vmPinnedTypes = ReadVmPinnedTypes();
        Log.LogMessage(MessageImportance.High,
            "ILSplit: vmPinnedTypes count = {0}",
            vmPinnedTypes?.Count.ToString() ?? "null");
        if (Log.HasLoggedErrors)
        {
            return false;
        }

        List<ITaskItem> outputItems = new();
        bool wroteManifest = false;

        foreach (ITaskItem input in InputAssemblies)
        {
            string inputPath = input.ItemSpec;
            string fileName = Path.GetFileName(inputPath);
            string assemblyName = Path.GetFileNameWithoutExtension(fileName);

            if (!ShouldSplit(fileName, assemblyName, splitSet))
            {
                outputItems.Add(input);
                continue;
            }

            Log.LogMessage(MessageImportance.Normal, "ILSplit: Splitting {0}", fileName);

            try
            {
                SplitAssembly(inputPath, hotClasses, vmPinnedTypes, outputItems, input);
                wroteManifest = true;
            }
            catch (Exception ex)
            {
                Log.LogError("ILSplit failed for '{0}': {1}\n{2}", fileName, ex.Message, ex.ToString());
                return false;
            }
        }

        SplitAssemblies = outputItems.ToArray();
        ManifestPath = wroteManifest ? Path.Combine(OutputDirectory, "ilsplit-manifest.json") : string.Empty;

        Log.LogMessage(MessageImportance.Normal,
            "ILSplit complete: {0} output assemblies.", SplitAssemblies.Length);

        return !Log.HasLoggedErrors;
#endif
    }

#if NET
    private HashSet<string> BuildSplitSet()
    {
        HashSet<string> splitSet = new(StringComparer.OrdinalIgnoreCase);

        if (AssembliesToSplit is null || AssembliesToSplit.Length == 0)
        {
            foreach (ITaskItem item in InputAssemblies)
            {
                splitSet.Add(Path.GetFileName(item.ItemSpec));
                splitSet.Add(Path.GetFileNameWithoutExtension(item.ItemSpec));
            }
        }
        else
        {
            foreach (ITaskItem item in AssembliesToSplit)
            {
                splitSet.Add(Path.GetFileName(item.ItemSpec));
                splitSet.Add(Path.GetFileNameWithoutExtension(item.ItemSpec));
            }
        }

        return splitSet;
    }

    private HashSet<string>? ReadProfile()
    {
        if (string.IsNullOrEmpty(HotClassListPath))
        {
            return null;
        }

        if (!File.Exists(HotClassListPath))
        {
            Log.LogError("ILSplit profile file not found: '{0}'", HotClassListPath);
            return null;
        }

        Log.LogMessage(MessageImportance.Normal, "ILSplit: Reading profile from {0}", HotClassListPath);

        return ProfileReader.Read(HotClassListPath);
    }

    private void SplitAssembly(
        string inputPath,
        HashSet<string>? hotClasses,
        HashSet<string>? vmPinnedTypes,
        List<ITaskItem> outputItems,
        ITaskItem originalItem)
    {
        using AssemblyDefinition assembly = AssemblyDefinition.ReadAssembly(
            inputPath,
            new ReaderParameters { ReadSymbols = false, ReadWrite = false });

        string originalName = assembly.Name.Name;

        DependencyGraph graph = DependencyGraph.Build(assembly);
        List<Cluster> clusters = ClusterStrategy.Compute(graph, hotClasses, MinClusterSize, vmPinnedTypes);

        // If all types ended up in a single cluster, skip the rewrite entirely.
        // A Cecil read→write round-trip can alter metadata layout, MVID, etc.,
        // which breaks runtimes that depend on exact binary identity (e.g., CoreCLR
        // bootstrap matching well-known types in System.Private.CoreLib).
        if (clusters.Count <= 1)
        {
            Log.LogMessage(MessageImportance.Normal,
                "ILSplit: {0} — single cluster, skipping split ({1} types)",
                originalName, graph.AllTypes.Count);
            outputItems.Add(originalItem);
            return;
        }

        AssemblyRewriter.Rewrite(assembly, clusters, OutputDirectory, vmPinnedTypes);
        ManifestWriter.Write(originalName, clusters, OutputDirectory);

        // Forwarder shell keeps the original name — it's a thin shim with
        // ExportedType entries for all types (hot and cold)
        string shellPath = Path.Combine(OutputDirectory, $"{originalName}.dll");
        TaskItem shellItem = new(shellPath);
        originalItem.CopyMetadataTo(shellItem);
        outputItems.Add(shellItem);

        // All chunk assemblies (.0.dll = hot, .1.dll, .2.dll, ... = cold)
        foreach (Cluster cluster in clusters)
        {
            string chunkPath = Path.Combine(OutputDirectory, $"{originalName}.{cluster.Index}.dll");
            TaskItem chunkItem = new(chunkPath);
            originalItem.CopyMetadataTo(chunkItem);
            chunkItem.SetMetadata("OriginalItemSpec", chunkPath);
            chunkItem.SetMetadata("RelativePath", $"{originalName}.{cluster.Index}.dll");
            chunkItem.SetMetadata("ILSplitChunk", "true");
            chunkItem.SetMetadata("ILSplitClusterIndex", cluster.Index.ToString());
            chunkItem.SetMetadata("ILSplitEager", cluster.IsEager.ToString());
            outputItems.Add(chunkItem);
        }

        Log.LogMessage(MessageImportance.Normal,
            "ILSplit: {0} → {1} clusters ({2} types total)",
            originalName, clusters.Count, graph.AllTypes.Count);
    }

    private static bool ShouldSplit(string fileName, string assemblyName, HashSet<string> splitSet)
    {
        return splitSet.Contains(fileName) || splitSet.Contains(assemblyName);
    }

    private HashSet<string>? ReadVmPinnedTypes()
    {
        if (string.IsNullOrEmpty(VmCorelibHeaderPath) || string.IsNullOrEmpty(VmNamespacesHeaderPath))
        {
            return null;
        }

        if (!File.Exists(VmCorelibHeaderPath))
        {
            Log.LogMessage(MessageImportance.Normal, "ILSplit: VM corelib header not found: {0}", VmCorelibHeaderPath);
            return null;
        }

        if (!File.Exists(VmNamespacesHeaderPath))
        {
            Log.LogMessage(MessageImportance.Normal, "ILSplit: VM namespace header not found: {0}", VmNamespacesHeaderPath);
            return null;
        }

        HashSet<string> pinnedTypes = VmPinnedTypeParser.Parse(VmNamespacesHeaderPath, VmCorelibHeaderPath);
        Log.LogMessage(MessageImportance.Normal, "ILSplit: Parsed {0} VM-pinned types from corelib.h", pinnedTypes.Count);

        return pinnedTypes;
    }
#endif
}
