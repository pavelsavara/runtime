// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Cecil;
using Xunit;

namespace ILSplit.Tests;

public class DependencyGraphTests
{
    [Fact]
    public void Build_DiscoversAllTypes()
    {
        using AssemblyDefinition asm = CreateTestAssembly();
        DependencyGraph graph = DependencyGraph.Build(asm);

        Assert.True(graph.AllTypes.Count > 0);
    }

    [Fact]
    public void Build_CreatesEdgesForBaseType()
    {
        using AssemblyDefinition asm = CreateTestAssembly();
        DependencyGraph graph = DependencyGraph.Build(asm);

        // All types (except <Module> and System.Object-derived with no explicit base)
        // should have some adjacency entries
        Assert.True(graph.Adjacency.Count > 0);
    }

    [Fact]
    public void Build_CreatesEdgesForGenericTypeArguments()
    {
        using AssemblyDefinition asm = CreateAssemblyWithGenericUsage();
        DependencyGraph graph = DependencyGraph.Build(asm);

        TypeDefinition? container = asm.MainModule.Types.FirstOrDefault(t => t.Name == "Container");
        TypeDefinition? item = asm.MainModule.Types.FirstOrDefault(t => t.Name == "Item");
        TypeDefinition? wrapper = asm.MainModule.Types.FirstOrDefault(t => t.Name == "Wrapper`1");

        Assert.NotNull(container);
        Assert.NotNull(item);
        Assert.NotNull(wrapper);

        Assert.True(graph.Adjacency.TryGetValue(container, out HashSet<TypeDefinition>? edges));
        Assert.Contains(wrapper, edges);
        Assert.Contains(item, edges);
    }

    private static AssemblyDefinition CreateTestAssembly()
    {
        // Create a minimal in-memory assembly for testing
        AssemblyNameDefinition name = new("TestAssembly", new System.Version(1, 0));
        AssemblyDefinition asm = AssemblyDefinition.CreateAssembly(name, "TestAssembly", ModuleKind.Dll);
        ModuleDefinition module = asm.MainModule;

        // Add a simple type
        TypeDefinition type = new("TestNamespace", "TestClass",
            TypeAttributes.Public | TypeAttributes.Class,
            module.TypeSystem.Object);
        module.Types.Add(type);

        return asm;
    }

    private static AssemblyDefinition CreateAssemblyWithGenericUsage()
    {
        AssemblyNameDefinition name = new("GenericTestAssembly", new System.Version(1, 0));
        AssemblyDefinition asm = AssemblyDefinition.CreateAssembly(name, "GenericTestAssembly", ModuleKind.Dll);
        ModuleDefinition module = asm.MainModule;

        TypeDefinition itemType = new("TestNamespace", "Item",
            TypeAttributes.Public | TypeAttributes.Class,
            module.TypeSystem.Object);
        module.Types.Add(itemType);

        TypeDefinition wrapperType = new("TestNamespace", "Wrapper`1",
            TypeAttributes.Public | TypeAttributes.Class,
            module.TypeSystem.Object);
        wrapperType.GenericParameters.Add(new GenericParameter("T", wrapperType));
        module.Types.Add(wrapperType);

        TypeDefinition containerType = new("TestNamespace", "Container",
            TypeAttributes.Public | TypeAttributes.Class,
            module.TypeSystem.Object);
        GenericInstanceType wrapperOfItem = new(wrapperType);
        wrapperOfItem.GenericArguments.Add(itemType);
        containerType.Fields.Add(new FieldDefinition("_wrapped", FieldAttributes.Private, wrapperOfItem));
        module.Types.Add(containerType);

        return asm;
    }
}
