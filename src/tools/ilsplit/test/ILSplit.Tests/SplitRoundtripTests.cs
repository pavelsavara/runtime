// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Xunit;
using FieldAttributes = Mono.Cecil.FieldAttributes;
using MethodAttributes = Mono.Cecil.MethodAttributes;
using TypeAttributes = Mono.Cecil.TypeAttributes;

namespace ILSplit.Tests;

public class SplitRoundtripTests : IDisposable
{
    private readonly string _tempDir;

    public SplitRoundtripTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "ILSplitTests_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_tempDir, recursive: true);
        }
        catch
        {
        }
    }

    [Fact]
    public void Rewrite_ProducesShellHotAndColdChunkFiles()
    {
        string inputPath = CreateTestAssemblyOnDisk("TestLib");
        string outputDir = Path.Combine(_tempDir, "output");
        Directory.CreateDirectory(outputDir);

        using AssemblyDefinition asm = AssemblyDefinition.ReadAssembly(inputPath,
            new ReaderParameters { ReadSymbols = false });

        DependencyGraph graph = DependencyGraph.Build(asm);
        HashSet<string> hotClasses = new() { "NS.HotType" };
        List<Cluster> clusters = ClusterStrategy.Compute(graph, hotClasses, minClusterSize: 0);
        AssemblyRewriter.Rewrite(asm, clusters, outputDir);

        Assert.True(File.Exists(Path.Combine(outputDir, "TestLib.dll")), "Forwarder shell should exist");
        Assert.True(File.Exists(Path.Combine(outputDir, "TestLib.0.dll")), "Hot chunk should exist");
        Assert.True(File.Exists(Path.Combine(outputDir, "TestLib.1.dll")), "Cold chunk should exist");
    }

    [Fact]
    public void Rewrite_ForwarderShellContainsExportedTypesForAllTypes()
    {
        string inputPath = CreateTestAssemblyOnDisk("TestLib2");
        string outputDir = Path.Combine(_tempDir, "output2");
        Directory.CreateDirectory(outputDir);

        using AssemblyDefinition asm = AssemblyDefinition.ReadAssembly(inputPath,
            new ReaderParameters { ReadSymbols = false });

        DependencyGraph graph = DependencyGraph.Build(asm);
        HashSet<string> hotClasses = new() { "NS.HotType" };
        List<Cluster> clusters = ClusterStrategy.Compute(graph, hotClasses, minClusterSize: 0);
        AssemblyRewriter.Rewrite(asm, clusters, outputDir);

        using AssemblyDefinition shell = AssemblyDefinition.ReadAssembly(
            Path.Combine(outputDir, "TestLib2.dll"),
            new ReaderParameters { ReadSymbols = false });

        // Forwarder shell should have no TypeDefs (except <Module>)
        HashSet<string> shellTypeNames = shell.MainModule.Types
            .Where(t => t.Name != "<Module>")
            .Select(t => t.FullName)
            .ToHashSet();
        Assert.Empty(shellTypeNames);

        // Forwarder shell should have ExportedType entries for ALL types (hot and cold)
        HashSet<string> exportedNames = shell.MainModule.ExportedTypes
            .Select(et => string.IsNullOrEmpty(et.Namespace) ? et.Name : $"{et.Namespace}.{et.Name}")
            .ToHashSet();
        Assert.Contains("NS.HotType", exportedNames);
        Assert.Contains("NS.ColdType", exportedNames);

        // Hot types should be forwarded to .0.dll
        ExportedType hotForwarder = shell.MainModule.ExportedTypes
            .First(et => et.Name == "HotType");
        Assert.Equal("TestLib2.0", ((AssemblyNameReference)hotForwarder.Scope).Name);
    }

    [Fact]
    public void Rewrite_HotChunkContainsOnlyHotTypes()
    {
        string inputPath = CreateTestAssemblyOnDisk("TestLib3");
        string outputDir = Path.Combine(_tempDir, "output3");
        Directory.CreateDirectory(outputDir);

        using AssemblyDefinition asm = AssemblyDefinition.ReadAssembly(inputPath,
            new ReaderParameters { ReadSymbols = false });

        DependencyGraph graph = DependencyGraph.Build(asm);
        HashSet<string> hotClasses = new() { "NS.HotType" };
        List<Cluster> clusters = ClusterStrategy.Compute(graph, hotClasses, minClusterSize: 0);
        AssemblyRewriter.Rewrite(asm, clusters, outputDir);

        using AssemblyDefinition hotChunk = AssemblyDefinition.ReadAssembly(
            Path.Combine(outputDir, "TestLib3.0.dll"),
            new ReaderParameters { ReadSymbols = false });

        HashSet<string> hotTypeNames = hotChunk.MainModule.Types
            .Where(t => t.Name != "<Module>")
            .Select(t => t.FullName)
            .ToHashSet();

        Assert.Contains("NS.HotType", hotTypeNames);
        Assert.DoesNotContain("NS.ColdType", hotTypeNames);

        // Hot chunk should have NO ExportedType forwarders
        Assert.Empty(hotChunk.MainModule.ExportedTypes);

        // Hot chunk should have NO AssemblyRef to cold chunks
        Assert.DoesNotContain(hotChunk.MainModule.AssemblyReferences,
            r => r.Name.StartsWith("TestLib3.") && r.Name != "TestLib3.0");
    }

    [Fact]
    public void Rewrite_ColdTypesInSeparateChunk()
    {
        string inputPath = CreateTestAssemblyOnDisk("TestLib4");
        string outputDir = Path.Combine(_tempDir, "output4");
        Directory.CreateDirectory(outputDir);

        using AssemblyDefinition asm = AssemblyDefinition.ReadAssembly(inputPath,
            new ReaderParameters { ReadSymbols = false });

        DependencyGraph graph = DependencyGraph.Build(asm);
        HashSet<string> hotClasses = new() { "NS.HotType" };
        List<Cluster> clusters = ClusterStrategy.Compute(graph, hotClasses, minClusterSize: 0);
        AssemblyRewriter.Rewrite(asm, clusters, outputDir);

        Assert.True(clusters.Count >= 2, "Should have at least hot and cold clusters");

        using AssemblyDefinition coldChunk = AssemblyDefinition.ReadAssembly(
            Path.Combine(outputDir, "TestLib4.1.dll"),
            new ReaderParameters { ReadSymbols = false });

        HashSet<string> coldTypeNames = coldChunk.MainModule.Types
            .Where(t => t.Name != "<Module>")
            .Select(t => t.FullName)
            .ToHashSet();

        Assert.Contains("NS.ColdType", coldTypeNames);
        Assert.DoesNotContain("NS.HotType", coldTypeNames);
    }

    [Fact]
    public void Rewrite_ManifestIsWritten()
    {
        string inputPath = CreateTestAssemblyOnDisk("TestLib5");
        string outputDir = Path.Combine(_tempDir, "output5");
        Directory.CreateDirectory(outputDir);

        using AssemblyDefinition asm = AssemblyDefinition.ReadAssembly(inputPath,
            new ReaderParameters { ReadSymbols = false });

        DependencyGraph graph = DependencyGraph.Build(asm);
        HashSet<string> hotClasses = new() { "NS.HotType" };
        List<Cluster> clusters = ClusterStrategy.Compute(graph, hotClasses, minClusterSize: 0);
        AssemblyRewriter.Rewrite(asm, clusters, outputDir);
        ManifestWriter.Write(asm.Name.Name, clusters, outputDir);

        Assert.True(File.Exists(Path.Combine(outputDir, "ilsplit-manifest.json")));
    }

    private string CreateTestAssemblyOnDisk(string assemblyName)
    {
        AssemblyNameDefinition name = new(assemblyName, new Version(1, 0));
        using AssemblyDefinition asm = AssemblyDefinition.CreateAssembly(name, assemblyName, ModuleKind.Dll);
        ModuleDefinition module = asm.MainModule;

        module.Types.Add(new TypeDefinition("NS", "HotType",
            TypeAttributes.Public | TypeAttributes.Class, module.TypeSystem.Object));
        module.Types.Add(new TypeDefinition("NS", "ColdType",
            TypeAttributes.Public | TypeAttributes.Class, module.TypeSystem.Object));

        string path = Path.Combine(_tempDir, $"{assemblyName}.dll");
        asm.Write(path);

        return path;
    }

    // --- Hybrid shell tests (VM-pinned types as TypeDefs in shell) ---

    [Fact]
    public void Rewrite_HybridShell_PinnedTypesRemainAsTypeDefs()
    {
        string inputPath = CreateHybridShellTestAssemblyOnDisk("HybridShell1");
        string outputDir = Path.Combine(_tempDir, "hybrid1");
        Directory.CreateDirectory(outputDir);

        using (AssemblyDefinition asm = AssemblyDefinition.ReadAssembly(inputPath,
            new ReaderParameters { ReadSymbols = false }))
        {
            DependencyGraph graph = DependencyGraph.Build(asm);
            HashSet<string> hotClasses = new() { "NS.PinnedBase", "NS.HotDerived" };
            List<Cluster> clusters = ClusterStrategy.Compute(graph, hotClasses, minClusterSize: 0);

            // Pin only PinnedBase in the shell (HotDerived stays in the hot chunk)
            HashSet<string> shellPinned = new() { "NS.PinnedBase" };
            AssemblyRewriter.Rewrite(asm, clusters, outputDir, shellPinned);
        }

        // Shell should contain PinnedBase as a TypeDef (not just a forwarder)
        using AssemblyDefinition shell = AssemblyDefinition.ReadAssembly(
            Path.Combine(outputDir, "HybridShell1.dll"),
            new ReaderParameters { ReadSymbols = false });

        HashSet<string> shellTypeNames = shell.MainModule.Types
            .Where(t => t.Name != "<Module>")
            .Select(t => t.FullName)
            .ToHashSet();
        Assert.Contains("NS.PinnedBase", shellTypeNames);

        // PinnedBase should NOT appear as an ExportedType forwarder
        Assert.DoesNotContain(shell.MainModule.ExportedTypes,
            et => et.Name == "PinnedBase" && et.Namespace == "NS");

        // Non-pinned types should still be forwarded
        Assert.Contains(shell.MainModule.ExportedTypes,
            et => et.Name == "HotDerived" && et.Namespace == "NS");
        Assert.Contains(shell.MainModule.ExportedTypes,
            et => et.Name == "ColdStandalone" && et.Namespace == "NS");

        // Hot chunk should NOT contain the shell-pinned type
        using AssemblyDefinition hotChunk = AssemblyDefinition.ReadAssembly(
            Path.Combine(outputDir, "HybridShell1.0.dll"),
            new ReaderParameters { ReadSymbols = false });

        HashSet<string> hotTypeNames = hotChunk.MainModule.Types
            .Where(t => t.Name != "<Module>")
            .Select(t => t.FullName)
            .ToHashSet();
        Assert.DoesNotContain("NS.PinnedBase", hotTypeNames);
        Assert.Contains("NS.HotDerived", hotTypeNames);

        // Hot chunk should have an AssemblyRef to the shell (original assembly name)
        // because HotDerived extends PinnedBase, which is in the shell
        Assert.Contains(hotChunk.MainModule.AssemblyReferences,
            r => r.Name == "HybridShell1");
    }

    [Fact]
    public void Rewrite_HybridShell_ColdChunkReferencesShellPinnedType()
    {
        string inputPath = CreateAssemblyWithCrossRefsOnDisk("HybridShell2");
        string outputDir = Path.Combine(_tempDir, "hybrid2");
        Directory.CreateDirectory(outputDir);

        using (AssemblyDefinition asm = AssemblyDefinition.ReadAssembly(inputPath,
            new ReaderParameters { ReadSymbols = false }))
        {
            DependencyGraph graph = DependencyGraph.Build(asm);
            HashSet<string> hotClasses = new() { "NS.HotBase" };
            List<Cluster> clusters = ClusterStrategy.Compute(graph, hotClasses, minClusterSize: 0);

            HashSet<string> shellPinned = new() { "NS.HotBase" };
            AssemblyRewriter.Rewrite(asm, clusters, outputDir, shellPinned);
        }

        // Cold chunks that reference HotBase should have an AssemblyRef to the shell
        string[] coldChunkFiles = Directory.GetFiles(outputDir, "HybridShell2.*.dll")
            .Where(f => !f.EndsWith("HybridShell2.0.dll")).ToArray();

        bool foundShellRef = false;
        foreach (string coldFile in coldChunkFiles)
        {
            using AssemblyDefinition cold = AssemblyDefinition.ReadAssembly(coldFile,
                new ReaderParameters { ReadSymbols = false });

            // ColdUser has a field of type HotBase — it should reference the shell
            if (cold.MainModule.Types.Any(t => t.FullName == "NS.ColdUser"))
            {
                if (cold.MainModule.AssemblyReferences.Any(r => r.Name == "HybridShell2"))
                {
                    foundShellRef = true;
                }
            }
        }

        Assert.True(foundShellRef,
            "Cold chunk containing ColdUser should have an AssemblyRef to the shell for HotBase");
    }

    // --- Round-trip validation tests (Step 9) ---

    [Fact]
    public void Roundtrip_AllOriginalTypesInChunks_ForwardersConsistent()
    {
        string inputPath = CreateMultiTypeAssemblyOnDisk("RTSplit1");
        string outputDir = Path.Combine(_tempDir, "rt1");
        Directory.CreateDirectory(outputDir);

        List<string> originalTypeNames;
        using (AssemblyDefinition asm = AssemblyDefinition.ReadAssembly(inputPath,
            new ReaderParameters { ReadSymbols = false }))
        {
            originalTypeNames = asm.MainModule.Types
                .Where(t => t.Name != "<Module>")
                .Select(t => t.FullName)
                .ToList();

            DependencyGraph graph = DependencyGraph.Build(asm);
            HashSet<string> hotClasses = new() { "NS.Alpha", "NS.Beta" };
            List<Cluster> clusters = ClusterStrategy.Compute(graph, hotClasses, minClusterSize: 0);
            AssemblyRewriter.Rewrite(asm, clusters, outputDir);
        }

        // Collect types from all chunk assemblies (.0.dll, .1.dll, ...)
        Dictionary<string, string> typeToAssembly = new();

        foreach (string chunkFile in Directory.GetFiles(outputDir, "RTSplit1.*.dll"))
        {
            using AssemblyDefinition chunk = AssemblyDefinition.ReadAssembly(chunkFile,
                new ReaderParameters { ReadSymbols = false });
            foreach (TypeDefinition t in chunk.MainModule.Types.Where(t => t.Name != "<Module>"))
            {
                typeToAssembly[t.FullName] = chunk.Name.Name;
            }
        }

        foreach (string name in originalTypeNames)
        {
            Assert.True(typeToAssembly.ContainsKey(name), $"Type {name} not found in any chunk assembly");
        }

        Assert.Equal(originalTypeNames.Count, typeToAssembly.Count);

        // Verify the forwarder shell has forwarders for ALL types
        using AssemblyDefinition shell = AssemblyDefinition.ReadAssembly(
            Path.Combine(outputDir, "RTSplit1.dll"), new ReaderParameters { ReadSymbols = false });

        // Shell should have no TypeDefs (except <Module>)
        Assert.Equal(0, shell.MainModule.Types.Count(t => t.Name != "<Module>"));

        // Shell should forward ALL original types
        Assert.Equal(originalTypeNames.Count, shell.MainModule.ExportedTypes.Count);

        foreach (ExportedType et in shell.MainModule.ExportedTypes)
        {
            if (et.DeclaringType is not null)
            {
                continue;
            }

            string fullName = string.IsNullOrEmpty(et.Namespace) ? et.Name : $"{et.Namespace}.{et.Name}";
            Assert.True(typeToAssembly.TryGetValue(fullName, out string? expectedAssembly),
                $"ExportedType {fullName} not found in any chunk");

            AssemblyNameReference scopeRef = (AssemblyNameReference)et.Scope;
            Assert.Equal(expectedAssembly, scopeRef.Name);
        }
    }

    [Fact]
    public void Roundtrip_ComplexAssembly_MetadataPreservedInChunks()
    {
        string inputPath = CreateComplexAssemblyOnDisk("RTSplit2");
        string outputDir = Path.Combine(_tempDir, "rt2");
        Directory.CreateDirectory(outputDir);

        using (AssemblyDefinition asm = AssemblyDefinition.ReadAssembly(inputPath,
            new ReaderParameters { ReadSymbols = false }))
        {
            DependencyGraph graph = DependencyGraph.Build(asm);
            HashSet<string> hotClasses = new() { "NS.BaseClass" };
            List<Cluster> clusters = ClusterStrategy.Compute(graph, hotClasses, minClusterSize: 0);
            AssemblyRewriter.Rewrite(asm, clusters, outputDir);
        }

        Dictionary<string, TypeDefinition> allTypes = new();
        Dictionary<string, string> typeModule = new();
        List<AssemblyDefinition> loaded = new();

        // Load all chunk assemblies (.0.dll, .1.dll, ...)
        foreach (string outputFile in Directory.GetFiles(outputDir, "RTSplit2.*.dll"))
        {
            AssemblyDefinition chunk = AssemblyDefinition.ReadAssembly(outputFile,
                new ReaderParameters { ReadSymbols = false });
            loaded.Add(chunk);
            foreach (TypeDefinition t in GetAllCecilTypes(chunk.MainModule))
            {
                if (t.Name != "<Module>")
                {
                    allTypes[t.FullName] = t;
                    typeModule[t.FullName] = chunk.Name.Name;
                }
            }
        }

        try
        {
            Assert.True(allTypes.ContainsKey("NS.BaseClass"), "BaseClass missing from chunks");
            MethodDefinition? getValueMethod = allTypes["NS.BaseClass"].Methods
                .FirstOrDefault(m => m.Name == "GetValue");
            Assert.NotNull(getValueMethod);
            Assert.True(getValueMethod.HasBody);
            Assert.True(getValueMethod.Body.Instructions.Count > 0);

            Assert.True(allTypes.ContainsKey("NS.DerivedClass"), "DerivedClass missing from chunks");
            TypeDefinition derived = allTypes["NS.DerivedClass"];
            Assert.NotNull(derived.BaseType);
            Assert.Contains("BaseClass", derived.BaseType.Name);

            Assert.True(allTypes.ContainsKey("NS.IService"), "IService missing from chunks");
            Assert.True(allTypes["NS.IService"].IsInterface);

            Assert.True(allTypes.ContainsKey("NS.ServiceImpl"), "ServiceImpl missing from chunks");
            TypeDefinition serviceImpl = allTypes["NS.ServiceImpl"];
            Assert.True(serviceImpl.Interfaces.Count > 0);
            Assert.Contains("IService", serviceImpl.Interfaces[0].InterfaceType.Name);

            Assert.True(allTypes.ContainsKey("NS.GenericHolder`1"), "GenericHolder missing from chunks");
            TypeDefinition generic = allTypes["NS.GenericHolder`1"];
            Assert.Equal(1, generic.GenericParameters.Count);
            Assert.Equal("T", generic.GenericParameters[0].Name);

            Assert.True(allTypes.ContainsKey("NS.Outer"), "Outer missing from chunks");
            Assert.True(allTypes.ContainsKey("NS.Outer/Inner"), "Outer.Inner missing from chunks");
            Assert.Equal(typeModule["NS.Outer"], typeModule["NS.Outer/Inner"]);
        }
        finally
        {
            foreach (AssemblyDefinition a in loaded)
            {
                a.Dispose();
            }
        }
    }

    [Fact]
    public void Roundtrip_CrossChunkAssemblyRefsPresent()
    {
        string inputPath = CreateAssemblyWithCrossRefsOnDisk("RTSplit3");
        string outputDir = Path.Combine(_tempDir, "rt3");
        Directory.CreateDirectory(outputDir);

        using (AssemblyDefinition asm = AssemblyDefinition.ReadAssembly(inputPath,
            new ReaderParameters { ReadSymbols = false }))
        {
            DependencyGraph graph = DependencyGraph.Build(asm);
            HashSet<string> hotClasses = new() { "NS.HotBase" };
            List<Cluster> clusters = ClusterStrategy.Compute(graph, hotClasses, minClusterSize: 0);
            Assert.True(clusters.Count >= 2, "Expected at least hot + cold clusters");
            AssemblyRewriter.Rewrite(asm, clusters, outputDir);
        }

        // Forwarder shell keeps original name, hot chunk is .0
        string shellPath = Path.Combine(outputDir, "RTSplit3.dll");
        string hotChunkPath = Path.Combine(outputDir, "RTSplit3.0.dll");
        Assert.True(File.Exists(shellPath), "Forwarder shell should exist");
        Assert.True(File.Exists(hotChunkPath), "Hot chunk (.0) should exist");

        string[] coldChunkFiles = Directory.GetFiles(outputDir, "RTSplit3.*.dll")
            .Where(f => !f.EndsWith("RTSplit3.0.dll")).ToArray();
        Assert.True(coldChunkFiles.Length > 0, "Expected at least one cold chunk");

        // Cold chunks should reference the hot chunk (.0) for hot types
        bool foundCrossRef = false;
        foreach (string coldFile in coldChunkFiles)
        {
            using AssemblyDefinition cold = AssemblyDefinition.ReadAssembly(coldFile,
                new ReaderParameters { ReadSymbols = false });
            if (cold.MainModule.AssemblyReferences.Any(r => r.Name == "RTSplit3.0"))
            {
                foundCrossRef = true;
                break;
            }
        }

        Assert.True(foundCrossRef, "No cold chunk has an AssemblyRef to the hot chunk (.0)");

        // Hot chunk should have NO AssemblyRef to cold chunks
        using AssemblyDefinition hotChunk = AssemblyDefinition.ReadAssembly(hotChunkPath,
            new ReaderParameters { ReadSymbols = false });
        foreach (AssemblyNameReference asmRef in hotChunk.MainModule.AssemblyReferences)
        {
            Assert.False(asmRef.Name.StartsWith("RTSplit3.") && asmRef.Name != "RTSplit3.0",
                $"Hot chunk should not reference cold chunk: {asmRef.Name}");
        }
    }

    [Fact]
    public void Roundtrip_TypeForwardersResolveViaAssemblyLoadContext()
    {
        string inputPath = CreateInstantiableAssemblyOnDisk("RTSplit4");
        string outputDir = Path.Combine(_tempDir, "rt4");
        Directory.CreateDirectory(outputDir);

        using (AssemblyDefinition asm = AssemblyDefinition.ReadAssembly(inputPath,
            new ReaderParameters { ReadSymbols = false }))
        {
            DependencyGraph graph = DependencyGraph.Build(asm);
            HashSet<string> hotClasses = new() { "NS.HotService" };
            List<Cluster> clusters = ClusterStrategy.Compute(graph, hotClasses, minClusterSize: 0);
            AssemblyRewriter.Rewrite(asm, clusters, outputDir);
        }

        SplitAssemblyLoadContext alc = new(outputDir);
        try
        {
            Assembly shell = alc.LoadFromAssemblyPath(Path.Combine(outputDir, "RTSplit4.dll"));

            Type? hotType = shell.GetType("NS.HotService");
            Type? coldType = shell.GetType("NS.ColdUtil");

            Assert.NotNull(hotType);
            Assert.NotNull(coldType);

            Assert.NotEqual(hotType.Assembly.GetName().Name, coldType.Assembly.GetName().Name);
            // Hot type lives in the hot chunk (RTSplit4.0), cold in a cold chunk (RTSplit4.1, etc.)
            Assert.Equal("RTSplit4.0", hotType.Assembly.GetName().Name);
            Assert.StartsWith("RTSplit4.", coldType.Assembly.GetName().Name!);
            Assert.NotEqual("RTSplit4.0", coldType.Assembly.GetName().Name);

            object hotInstance = Activator.CreateInstance(hotType)!;
            Assert.NotNull(hotInstance);

            object coldInstance = Activator.CreateInstance(coldType)!;
            Assert.NotNull(coldInstance);
        }
        finally
        {
            alc.Unload();
        }
    }

    // --- Helper methods for round-trip tests ---

    private string CreateHybridShellTestAssemblyOnDisk(string assemblyName)
    {
        AssemblyNameDefinition name = new(assemblyName, new Version(1, 0));
        using AssemblyDefinition asm = AssemblyDefinition.CreateAssembly(name, assemblyName, ModuleKind.Dll);
        ModuleDefinition module = asm.MainModule;

        // PinnedBase — will be pinned in the shell
        TypeDefinition pinnedBase = new("NS", "PinnedBase",
            TypeAttributes.Public | TypeAttributes.Class, module.TypeSystem.Object);
        module.Types.Add(pinnedBase);

        // HotDerived — hot type that extends PinnedBase (stays in hot chunk)
        TypeDefinition hotDerived = new("NS", "HotDerived",
            TypeAttributes.Public | TypeAttributes.Class, pinnedBase);
        module.Types.Add(hotDerived);

        // ColdStandalone — cold type, not pinned
        TypeDefinition coldStandalone = new("NS", "ColdStandalone",
            TypeAttributes.Public | TypeAttributes.Class, module.TypeSystem.Object);
        module.Types.Add(coldStandalone);

        string path = Path.Combine(_tempDir, $"{assemblyName}.dll");
        asm.Write(path);

        return path;
    }

    private string CreateMultiTypeAssemblyOnDisk(string assemblyName)
    {
        AssemblyNameDefinition name = new(assemblyName, new Version(1, 0));
        using AssemblyDefinition asm = AssemblyDefinition.CreateAssembly(name, assemblyName, ModuleKind.Dll);
        ModuleDefinition module = asm.MainModule;

        module.Types.Add(new TypeDefinition("NS", "Alpha",
            TypeAttributes.Public | TypeAttributes.Class, module.TypeSystem.Object));
        module.Types.Add(new TypeDefinition("NS", "Beta",
            TypeAttributes.Public | TypeAttributes.Class, module.TypeSystem.Object));
        module.Types.Add(new TypeDefinition("NS", "Gamma",
            TypeAttributes.Public | TypeAttributes.Class, module.TypeSystem.Object));
        module.Types.Add(new TypeDefinition("NS", "Delta",
            TypeAttributes.Public | TypeAttributes.Class, module.TypeSystem.Object));

        string path = Path.Combine(_tempDir, $"{assemblyName}.dll");
        asm.Write(path);

        return path;
    }

    private string CreateComplexAssemblyOnDisk(string assemblyName)
    {
        AssemblyNameDefinition name = new(assemblyName, new Version(1, 0));
        using AssemblyDefinition asm = AssemblyDefinition.CreateAssembly(name, assemblyName, ModuleKind.Dll);
        ModuleDefinition module = asm.MainModule;

        TypeDefinition baseClass = new("NS", "BaseClass",
            TypeAttributes.Public | TypeAttributes.Class, module.TypeSystem.Object);
        MethodDefinition getValueMethod = new("GetValue",
            MethodAttributes.Public | MethodAttributes.Virtual,
            module.TypeSystem.Int32);
        ILProcessor il = getValueMethod.Body.GetILProcessor();
        il.Emit(OpCodes.Ldc_I4, 42);
        il.Emit(OpCodes.Ret);
        baseClass.Methods.Add(getValueMethod);
        module.Types.Add(baseClass);

        TypeDefinition derivedClass = new("NS", "DerivedClass",
            TypeAttributes.Public | TypeAttributes.Class, baseClass);
        module.Types.Add(derivedClass);

        TypeDefinition iService = new("NS", "IService",
            TypeAttributes.Public | TypeAttributes.Interface | TypeAttributes.Abstract,
            null);
        MethodDefinition executeMethod = new("Execute",
            MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Abstract |
            MethodAttributes.HideBySig | MethodAttributes.NewSlot,
            module.TypeSystem.Void);
        iService.Methods.Add(executeMethod);
        module.Types.Add(iService);

        TypeDefinition serviceImpl = new("NS", "ServiceImpl",
            TypeAttributes.Public | TypeAttributes.Class, module.TypeSystem.Object);
        serviceImpl.Interfaces.Add(new InterfaceImplementation(iService));
        MethodDefinition executeImpl = new("Execute",
            MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig,
            module.TypeSystem.Void);
        ILProcessor ilImpl = executeImpl.Body.GetILProcessor();
        ilImpl.Emit(OpCodes.Ret);
        serviceImpl.Methods.Add(executeImpl);
        module.Types.Add(serviceImpl);

        TypeDefinition genericHolder = new("NS", "GenericHolder`1",
            TypeAttributes.Public | TypeAttributes.Class, module.TypeSystem.Object);
        genericHolder.GenericParameters.Add(new GenericParameter("T", genericHolder));
        module.Types.Add(genericHolder);

        TypeDefinition outer = new("NS", "Outer",
            TypeAttributes.Public | TypeAttributes.Class, module.TypeSystem.Object);
        TypeDefinition inner = new("", "Inner",
            TypeAttributes.NestedPublic | TypeAttributes.Class, module.TypeSystem.Object);
        outer.NestedTypes.Add(inner);
        module.Types.Add(outer);

        string path = Path.Combine(_tempDir, $"{assemblyName}.dll");
        asm.Write(path);

        return path;
    }

    private string CreateAssemblyWithCrossRefsOnDisk(string assemblyName)
    {
        AssemblyNameDefinition name = new(assemblyName, new Version(1, 0));
        using AssemblyDefinition asm = AssemblyDefinition.CreateAssembly(name, assemblyName, ModuleKind.Dll);
        ModuleDefinition module = asm.MainModule;

        TypeDefinition hotBase = new("NS", "HotBase",
            TypeAttributes.Public | TypeAttributes.Class, module.TypeSystem.Object);
        module.Types.Add(hotBase);

        TypeDefinition coldUser = new("NS", "ColdUser",
            TypeAttributes.Public | TypeAttributes.Class, module.TypeSystem.Object);
        coldUser.Fields.Add(new FieldDefinition("_base", FieldAttributes.Private, hotBase));
        module.Types.Add(coldUser);

        TypeDefinition coldStandalone = new("NS", "ColdStandalone",
            TypeAttributes.Public | TypeAttributes.Class, module.TypeSystem.Object);
        module.Types.Add(coldStandalone);

        string path = Path.Combine(_tempDir, $"{assemblyName}.dll");
        asm.Write(path);

        return path;
    }

    private string CreateInstantiableAssemblyOnDisk(string assemblyName)
    {
        AssemblyNameDefinition name = new(assemblyName, new Version(1, 0));
        using AssemblyDefinition asm = AssemblyDefinition.CreateAssembly(name, assemblyName, ModuleKind.Dll);
        ModuleDefinition module = asm.MainModule;

        MethodReference objectCtor = new(".ctor", module.TypeSystem.Void, module.TypeSystem.Object)
        {
            HasThis = true,
        };

        TypeDefinition hotService = new("NS", "HotService",
            TypeAttributes.Public | TypeAttributes.Class, module.TypeSystem.Object);
        AddDefaultConstructor(hotService, module, objectCtor);
        module.Types.Add(hotService);

        TypeDefinition coldUtil = new("NS", "ColdUtil",
            TypeAttributes.Public | TypeAttributes.Class, module.TypeSystem.Object);
        AddDefaultConstructor(coldUtil, module, objectCtor);
        module.Types.Add(coldUtil);

        string path = Path.Combine(_tempDir, $"{assemblyName}.dll");
        asm.Write(path);

        return path;
    }

    private static void AddDefaultConstructor(TypeDefinition type, ModuleDefinition module, MethodReference baseCtor)
    {
        MethodDefinition ctor = new(".ctor",
            MethodAttributes.Public | MethodAttributes.HideBySig |
            MethodAttributes.SpecialName | MethodAttributes.RTSpecialName,
            module.TypeSystem.Void);
        ILProcessor il = ctor.Body.GetILProcessor();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Call, baseCtor);
        il.Emit(OpCodes.Ret);
        type.Methods.Add(ctor);
    }

    private static IEnumerable<TypeDefinition> GetAllCecilTypes(ModuleDefinition module)
    {
        foreach (TypeDefinition type in module.Types)
        {
            yield return type;
            foreach (TypeDefinition nested in GetAllNestedCecilTypes(type))
            {
                yield return nested;
            }
        }
    }

    private static IEnumerable<TypeDefinition> GetAllNestedCecilTypes(TypeDefinition type)
    {
        foreach (TypeDefinition nested in type.NestedTypes)
        {
            yield return nested;
            foreach (TypeDefinition deep in GetAllNestedCecilTypes(nested))
            {
                yield return deep;
            }
        }
    }

    private sealed class SplitAssemblyLoadContext : AssemblyLoadContext
    {
        private readonly string _outputDir;

        public SplitAssemblyLoadContext(string outputDir) : base(isCollectible: true)
        {
            _outputDir = outputDir;
        }

        protected override Assembly? Load(AssemblyName assemblyName)
        {
            string path = Path.Combine(_outputDir, $"{assemblyName.Name}.dll");
            if (File.Exists(path))
            {
                return LoadFromAssemblyPath(path);
            }

            return null;
        }
    }
}
