// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;

namespace ILSplit;

internal sealed class Cluster
{
    public int Index { get; }
    public bool IsEager { get; }
    public List<TypeDefinition> Types { get; } = new();

    public Cluster(int index, bool isEager)
    {
        Index = index;
        IsEager = isEager;
    }

    public int EstimatedSize => Types.Sum(EstimateTypeSize);

    private static int EstimateTypeSize(TypeDefinition type)
    {
        int size = 40; // Base metadata overhead per type
        size += type.Fields.Count * 16;
        foreach (MethodDefinition method in type.Methods)
        {
            size += 32; // Method metadata
            if (method.HasBody)
            {
                size += method.Body.CodeSize;
            }
        }

        return size;
    }
}

internal static class ClusterStrategy
{
    public static List<Cluster> Compute(DependencyGraph graph, HashSet<string>? hotClasses, int minClusterSize, HashSet<string>? vmPinnedTypes = null)
    {
        // Without a profile, fall back to namespace-based clustering
        if (hotClasses is null || hotClasses.Count == 0)
        {
            return ComputeNamespaceFallback(graph, minClusterSize, vmPinnedTypes);
        }

        // Step 1: Identify hot types (from profile + VM-pinned)
        HashSet<TypeDefinition> hotSet = new();
        foreach (TypeDefinition type in graph.AllTypes)
        {
            if (hotClasses.Contains(type.FullName))
            {
                hotSet.Add(type);
            }
            else if (vmPinnedTypes is not null && vmPinnedTypes.Contains(type.FullName))
            {
                hotSet.Add(type);
            }
        }

        // Step 2: Compute transitive closure of hot type dependencies
        HashSet<TypeDefinition> hotClosure = ComputeTransitiveClosure(graph, hotSet);

        // Step 3: Build hot cluster (cluster 0)
        Cluster hotCluster = new(0, isEager: true);
        foreach (TypeDefinition type in graph.AllTypes)
        {
            if (hotClosure.Contains(type))
            {
                hotCluster.Types.Add(type);
            }
        }

        // Step 4: Run Tarjan's SCC on remaining (cold) types
        HashSet<TypeDefinition> coldTypes = new(graph.AllTypes.Where(t => !hotClosure.Contains(t)));
        List<List<TypeDefinition>> sccs = TarjanScc(graph, coldTypes);

        // Step 5: Merge small SCCs into clusters above minimum size
        List<Cluster> clusters = new() { hotCluster };
        Cluster? currentCluster = null;

        foreach (List<TypeDefinition> scc in sccs)
        {
            if (currentCluster is null || currentCluster.EstimatedSize >= minClusterSize)
            {
                currentCluster = new Cluster(clusters.Count, isEager: false);
                clusters.Add(currentCluster);
            }

            currentCluster.Types.AddRange(scc);
        }

        // If there are no cold types, return just the hot cluster
        if (clusters.Count == 1 || (clusters.Count == 2 && clusters[1].Types.Count == 0))
        {
            return new List<Cluster> { hotCluster };
        }

        return clusters;
    }

    private static HashSet<TypeDefinition> ComputeTransitiveClosure(DependencyGraph graph, HashSet<TypeDefinition> roots)
    {
        HashSet<TypeDefinition> visited = new();
        Queue<TypeDefinition> queue = new(roots);

        while (queue.Count > 0)
        {
            TypeDefinition current = queue.Dequeue();
            if (!visited.Add(current))
            {
                continue;
            }

            if (graph.Adjacency.TryGetValue(current, out HashSet<TypeDefinition>? neighbors))
            {
                foreach (TypeDefinition neighbor in neighbors)
                {
                    if (!visited.Contains(neighbor))
                    {
                        queue.Enqueue(neighbor);
                    }
                }
            }
        }

        return visited;
    }

    private static List<List<TypeDefinition>> TarjanScc(DependencyGraph graph, HashSet<TypeDefinition> types)
    {
        int index = 0;
        Stack<TypeDefinition> stack = new();
        Dictionary<TypeDefinition, int> indices = new();
        Dictionary<TypeDefinition, int> lowLinks = new();
        HashSet<TypeDefinition> onStack = new();
        List<List<TypeDefinition>> result = new();

        foreach (TypeDefinition type in types)
        {
            if (!indices.ContainsKey(type))
            {
                StrongConnect(type);
            }
        }

        void StrongConnect(TypeDefinition v)
        {
            indices[v] = index;
            lowLinks[v] = index;
            index++;
            stack.Push(v);
            onStack.Add(v);

            if (graph.Adjacency.TryGetValue(v, out HashSet<TypeDefinition>? neighbors))
            {
                foreach (TypeDefinition w in neighbors)
                {
                    if (!types.Contains(w))
                    {
                        continue;
                    }

                    if (!indices.TryGetValue(w, out int wIndex))
                    {
                        StrongConnect(w);
                        lowLinks[v] = System.Math.Min(lowLinks[v], lowLinks[w]);
                    }
                    else if (onStack.Contains(w))
                    {
                        lowLinks[v] = System.Math.Min(lowLinks[v], wIndex);
                    }
                }
            }

            if (lowLinks[v] == indices[v])
            {
                List<TypeDefinition> scc = new();
                TypeDefinition w;
                do
                {
                    w = stack.Pop();
                    onStack.Remove(w);
                    scc.Add(w);
                } while (w != v);

                result.Add(scc);
            }
        }

        return result;
    }

    /// <summary>
    /// Names of types that sit at the root of the .NET type hierarchy.
    /// These must always reside in the hot (eager) cluster so that cold chunks
    /// can reference them via AssemblyRef without hitting type forwarders.
    /// </summary>
    private static readonly HashSet<string> s_fundamentalTypeNames = new(StringComparer.Ordinal)
    {
        "System.Object",
        "System.String",
        "System.ValueType",
        "System.Enum",
        "System.Delegate",
        "System.MulticastDelegate",
        "System.Array",
        "System.Attribute",
        "System.Type",
        "System.Exception",
    };

    private static List<Cluster> ComputeNamespaceFallback(DependencyGraph graph, int minClusterSize, HashSet<string>? vmPinnedTypes)
    {
        HashSet<TypeDefinition> allTypes = new(graph.AllTypes.Where(t => t.Name != "<Module>"));
        if (allTypes.Count == 0)
        {
            return new List<Cluster> { new Cluster(0, isEager: true) };
        }

        // Identify types that must be in the hot cluster:
        // 1. VM-pinned types from corelib.h (CoreCLR) — the VM resolves these
        //    by name in CoreLib and they must be TypeDefs, not forwarders.
        // 2. Fallback: fundamental type names (System.Object, etc.)
        HashSet<string> pinnedNames = vmPinnedTypes is not null && vmPinnedTypes.Count > 0
            ? vmPinnedTypes
            : s_fundamentalTypeNames;

        HashSet<TypeDefinition> seeds = new();
        foreach (TypeDefinition type in allTypes)
        {
            if (pinnedNames.Contains(type.FullName))
            {
                seeds.Add(type);
            }
        }

        Cluster hotCluster = new(0, isEager: true);

        if (seeds.Count > 0)
        {
            // Compute transitive closure from seeds: pulls in all types that
            // fundamental types depend on (method body refs, field types, etc.)
            HashSet<TypeDefinition> hotClosure = ComputeTransitiveClosure(graph, seeds);

            foreach (TypeDefinition type in graph.AllTypes)
            {
                if (hotClosure.Contains(type))
                {
                    hotCluster.Types.Add(type);
                }
            }
        }

        // For non-CoreLib assemblies (no fundamental seeds) or if the closure
        // is empty, fall back to namespace-based grouping for the eager cluster.
        if (hotCluster.Types.Count == 0)
        {
            Dictionary<string, List<TypeDefinition>> namespaceGroups = new();
            foreach (TypeDefinition type in allTypes)
            {
                string ns = type.Namespace ?? string.Empty;
                if (!namespaceGroups.TryGetValue(ns, out List<TypeDefinition>? list))
                {
                    list = new List<TypeDefinition>();
                    namespaceGroups[ns] = list;
                }

                list.Add(type);
            }

            foreach (List<TypeDefinition> group in namespaceGroups.Values.OrderBy(g => g[0].Namespace))
            {
                hotCluster.Types.AddRange(group);
                if (hotCluster.EstimatedSize >= minClusterSize)
                {
                    break;
                }
            }
        }

        // Run Tarjan's SCC on remaining (cold) types and merge into clusters
        HashSet<TypeDefinition> hotSet = new(hotCluster.Types);
        HashSet<TypeDefinition> coldTypes = new(allTypes.Where(t => !hotSet.Contains(t)));

        if (coldTypes.Count == 0)
        {
            return new List<Cluster> { hotCluster };
        }

        List<List<TypeDefinition>> sccs = TarjanScc(graph, coldTypes);

        List<Cluster> clusters = new() { hotCluster };
        Cluster? currentCluster = null;

        foreach (List<TypeDefinition> scc in sccs)
        {
            if (currentCluster is null || currentCluster.EstimatedSize >= minClusterSize)
            {
                currentCluster = new Cluster(clusters.Count, isEager: false);
                clusters.Add(currentCluster);
            }

            currentCluster.Types.AddRange(scc);
        }

        if (clusters.Count == 1 || (clusters.Count == 2 && clusters[1].Types.Count == 0))
        {
            return new List<Cluster> { hotCluster };
        }

        return clusters;
    }
}
