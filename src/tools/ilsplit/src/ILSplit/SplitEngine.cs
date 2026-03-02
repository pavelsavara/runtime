// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.PortableExecutable;
using System.Threading.Tasks;
using Mono.Cecil;

namespace ILSplit;

internal static class SplitEngine
{
    public static Task<int> RunAsync(FileInfo[] inputs, DirectoryInfo output, FileInfo? profile, int minClusterSize, HashSet<string>? vmPinnedTypes = null)
    {
        if (!output.Exists)
        {
            output.Create();
        }

        HashSet<string>? hotClasses = null;
        if (profile is not null)
        {
            hotClasses = ProfileReader.Read(profile.FullName);
        }

        foreach (FileInfo input in inputs)
        {
            Console.WriteLine($"Processing: {input.FullName}");

            AssemblyDefinition assembly;
            try
            {
                assembly = AssemblyDefinition.ReadAssembly(
                    input.FullName,
                    new ReaderParameters { ReadSymbols = false, ReadWrite = false });
            }
            catch (BadImageFormatException)
            {
                Console.WriteLine($"  Skipping (not a managed assembly): {input.Name}");
                continue;
            }

            using (assembly)
            {
                // Detect R2R (ReadyToRun) assemblies by checking for a native
                // code directory in the PE header. Cecil cannot write these.
                if (HasNativeCode(input.FullName))
                {
                    Console.WriteLine($"  Skipping (mixed-mode/R2R assembly): {input.Name}");
                    continue;
                }

                DependencyGraph graph = DependencyGraph.Build(assembly);

                // When VM-pinned types are specified, expand the set to include
                // the transitive closure of all types they depend on. This ensures
                // the CoreLib shell is self-contained — loading a pinned type during
                // bootstrap won't require resolving type forwarders to chunk assemblies.
                HashSet<string>? effectivePinnedTypes = vmPinnedTypes;
                if (vmPinnedTypes is not null && vmPinnedTypes.Count > 0)
                {
                    effectivePinnedTypes = ExpandPinnedWithDependencies(graph, vmPinnedTypes);
                    Console.WriteLine($"  VM-pinned types: {vmPinnedTypes.Count} → {effectivePinnedTypes.Count} (with dependencies)");
                }

                List<Cluster> clusters = ClusterStrategy.Compute(graph, hotClasses, minClusterSize, effectivePinnedTypes);
                AssemblyRewriter.Rewrite(assembly, clusters, output.FullName, effectivePinnedTypes);
                ManifestWriter.Write(assembly.Name.Name, clusters, output.FullName);
            }
        }

        Console.WriteLine("ILSplit complete.");

        return Task.FromResult(0);
    }

    /// <summary>
    /// Expands the set of VM-pinned type names to include the transitive closure
    /// of all types reachable via the dependency graph. This ensures the CoreLib
    /// shell is self-contained during bootstrap — pinned types and everything
    /// they depend on stay as TypeDefs in the shell.
    /// </summary>
    private static HashSet<string> ExpandPinnedWithDependencies(DependencyGraph graph, HashSet<string> pinnedTypeNames)
    {
        // Build a name → TypeDefinition lookup for the graph
        Dictionary<string, TypeDefinition> nameToType = new(StringComparer.Ordinal);
        foreach (TypeDefinition type in graph.AllTypes)
        {
            nameToType[type.FullName] = type;
        }

        // BFS/DFS from all pinned types through the dependency graph
        HashSet<string> expanded = new(StringComparer.Ordinal);
        Queue<TypeDefinition> queue = new();

        foreach (string name in pinnedTypeNames)
        {
            if (nameToType.TryGetValue(name, out TypeDefinition? typeDef) && expanded.Add(name))
            {
                queue.Enqueue(typeDef);
            }
        }

        while (queue.Count > 0)
        {
            TypeDefinition current = queue.Dequeue();

            if (graph.Adjacency.TryGetValue(current, out HashSet<TypeDefinition>? deps))
            {
                foreach (TypeDefinition dep in deps)
                {
                    if (expanded.Add(dep.FullName))
                    {
                        queue.Enqueue(dep);
                    }
                }
            }
        }

        return expanded;
    }

    /// <summary>
    /// Returns true if the PE file contains native code sections (e.g. R2R / mixed-mode)
    /// that Mono.Cecil cannot rewrite.
    /// </summary>
    private static bool HasNativeCode(string path)
    {
        try
        {
            using FileStream fs = File.OpenRead(path);
            using PEReader pe = new(fs);

            // A managed-only assembly has no native header. Mixed-mode / R2R
            // assemblies have a non-zero native header size.
            return pe.PEHeaders.CorHeader is not null
                && pe.PEHeaders.CorHeader.ManagedNativeHeaderDirectory.Size > 0;
        }
        catch
        {
            return false;
        }
    }
}
