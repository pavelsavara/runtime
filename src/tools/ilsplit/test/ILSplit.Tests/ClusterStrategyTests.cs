// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Cecil;
using Xunit;

namespace ILSplit.Tests;

public class ClusterStrategyTests
{
    [Fact]
    public void Compute_WithHotClasses_CreatesHotCluster()
    {
        using AssemblyDefinition asm = CreateTestAssembly();
        DependencyGraph graph = DependencyGraph.Build(asm);

        HashSet<string> hotClasses = new() { "TestNamespace.HotClass" };
        List<Cluster> clusters = ClusterStrategy.Compute(graph, hotClasses, minClusterSize: 0);

        Assert.True(clusters.Count >= 1);
        Assert.True(clusters[0].IsEager);
    }

    [Fact]
    public void Compute_WithoutProfile_FallsBackToSingleCluster()
    {
        using AssemblyDefinition asm = CreateTestAssembly();
        DependencyGraph graph = DependencyGraph.Build(asm);

        List<Cluster> clusters = ClusterStrategy.Compute(graph, hotClasses: null, minClusterSize: 0);

        // Without a profile and no hot classes, all types end up in cold clusters (empty hot + cold)
        Assert.True(clusters.Count >= 1);
    }

    [Fact]
    public void Compute_WithNullProfile_UsesNamespaceFallback()
    {
        using AssemblyDefinition asm = CreateMultiNamespaceAssembly();
        DependencyGraph graph = DependencyGraph.Build(asm);

        List<Cluster> clusters = ClusterStrategy.Compute(graph, hotClasses: null, minClusterSize: 0);

        Assert.True(clusters.Count >= 1);
        Assert.True(clusters[0].IsEager);
        int totalTypes = clusters.Sum(c => c.Types.Count);
        Assert.Equal(3, totalTypes);
    }

    [Fact]
    public void Compute_NamespaceFallback_MergesSmallNamespaces()
    {
        using AssemblyDefinition asm = CreateMultiNamespaceAssembly();
        DependencyGraph graph = DependencyGraph.Build(asm);

        // With a high minClusterSize, all namespaces should merge into one cluster
        List<Cluster> clusters = ClusterStrategy.Compute(graph, hotClasses: null, minClusterSize: 1_000_000);

        Assert.Single(clusters);
        Assert.Equal(3, clusters[0].Types.Count);
    }

    private static AssemblyDefinition CreateTestAssembly()
    {
        AssemblyNameDefinition name = new("TestAssembly", new System.Version(1, 0));
        AssemblyDefinition asm = AssemblyDefinition.CreateAssembly(name, "TestAssembly", ModuleKind.Dll);
        ModuleDefinition module = asm.MainModule;

        TypeDefinition hotType = new("TestNamespace", "HotClass",
            TypeAttributes.Public | TypeAttributes.Class,
            module.TypeSystem.Object);
        module.Types.Add(hotType);

        TypeDefinition coldType = new("TestNamespace", "ColdClass",
            TypeAttributes.Public | TypeAttributes.Class,
            module.TypeSystem.Object);
        module.Types.Add(coldType);

        return asm;
    }

    private static AssemblyDefinition CreateMultiNamespaceAssembly()
    {
        AssemblyNameDefinition name = new("TestAssembly", new System.Version(1, 0));
        AssemblyDefinition asm = AssemblyDefinition.CreateAssembly(name, "TestAssembly", ModuleKind.Dll);
        ModuleDefinition module = asm.MainModule;

        module.Types.Add(new TypeDefinition("NS1", "ClassA",
            TypeAttributes.Public | TypeAttributes.Class, module.TypeSystem.Object));
        module.Types.Add(new TypeDefinition("NS1", "ClassB",
            TypeAttributes.Public | TypeAttributes.Class, module.TypeSystem.Object));
        module.Types.Add(new TypeDefinition("NS2", "ClassC",
            TypeAttributes.Public | TypeAttributes.Class, module.TypeSystem.Object));

        return asm;
    }

    [Fact]
    public void VmPinnedTypeParser_ParsesNamespaceHeader()
    {
        string nsHeader = Path.GetTempFileName();
        try
        {
            File.WriteAllText(nsHeader, """
                #define g_SystemNS          "System"
                #define g_ThreadingNS       g_SystemNS ".Threading"
                #define g_StubHelpersNS     g_SystemNS ".StubHelpers"
                """);

            Dictionary<string, string> nsDict = VmPinnedTypeParser.ParseNamespaces(nsHeader);

            Assert.Equal("System", nsDict["System"]);
            Assert.Equal("System.Threading", nsDict["Threading"]);
            Assert.Equal("System.StubHelpers", nsDict["StubHelpers"]);
        }
        finally
        {
            File.Delete(nsHeader);
        }
    }

    [Fact]
    public void VmPinnedTypeParser_ParsesDefineClassEntries()
    {
        string nsHeader = Path.GetTempFileName();
        string clHeader = Path.GetTempFileName();
        try
        {
            File.WriteAllText(nsHeader, """
                #define g_SystemNS          "System"
                #define g_ThreadingNS       g_SystemNS ".Threading"
                """);

            File.WriteAllText(clHeader, """
                DEFINE_CLASS(OBJECT,  System,    Object)
                DEFINE_CLASS(STRING,  System,    String)
                DEFINE_CLASS(THREAD,  Threading, Thread)
                DEFINE_METHOD(THREAD, START,     Start,  IM_RetVoid)
                """);

            HashSet<string> types = VmPinnedTypeParser.Parse(nsHeader, clHeader);

            Assert.Contains("System.Object", types);
            Assert.Contains("System.String", types);
            Assert.Contains("System.Threading.Thread", types);
            Assert.Equal(3, types.Count);
        }
        finally
        {
            File.Delete(nsHeader);
            File.Delete(clHeader);
        }
    }

    [Fact]
    public void Compute_WithVmPinnedTypes_PinsTypesInHotCluster()
    {
        AssemblyNameDefinition name = new("TestLib", new System.Version(1, 0));
        using AssemblyDefinition asm = AssemblyDefinition.CreateAssembly(name, "TestLib", ModuleKind.Dll);
        ModuleDefinition module = asm.MainModule;

        module.Types.Add(new TypeDefinition("MyApp", "Pinned",
            TypeAttributes.Public | TypeAttributes.Class, module.TypeSystem.Object));
        module.Types.Add(new TypeDefinition("MyApp", "Cold",
            TypeAttributes.Public | TypeAttributes.Class, module.TypeSystem.Object));

        DependencyGraph graph = DependencyGraph.Build(asm);
        HashSet<string> pinned = new() { "MyApp.Pinned" };

        List<Cluster> clusters = ClusterStrategy.Compute(graph, hotClasses: null, minClusterSize: 0, vmPinnedTypes: pinned);

        Assert.True(clusters[0].IsEager);
        Assert.Contains(clusters[0].Types, t => t.FullName == "MyApp.Pinned");
    }
}
