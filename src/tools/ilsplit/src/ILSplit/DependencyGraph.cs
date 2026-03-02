// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace ILSplit;

/// <summary>
/// Builds a directed graph where each node is a TypeDefinition and edges
/// represent "type A needs type B to be loaded".
/// </summary>
internal sealed class DependencyGraph
{
    private readonly Dictionary<TypeDefinition, HashSet<TypeDefinition>> _adjacency = new();
    private readonly List<TypeDefinition> _allTypes = new();

    public IReadOnlyList<TypeDefinition> AllTypes => _allTypes;

    public IReadOnlyDictionary<TypeDefinition, HashSet<TypeDefinition>> Adjacency => _adjacency;

    public static DependencyGraph Build(AssemblyDefinition assembly)
    {
        DependencyGraph graph = new();
        ModuleDefinition module = assembly.MainModule;

        // Collect all types (top-level and nested), excluding <Module>
        foreach (TypeDefinition type in module.Types)
        {
            if (type.Name == "<Module>")
            {
                continue;
            }

            graph.AddType(type);
            CollectNestedTypes(graph, type);
        }

        // Build edges
        foreach (TypeDefinition type in graph._allTypes)
        {
            graph.AnalyzeType(type, module);
        }

        return graph;
    }

    private static void CollectNestedTypes(DependencyGraph graph, TypeDefinition type)
    {
        foreach (TypeDefinition nested in type.NestedTypes)
        {
            graph.AddType(nested);
            CollectNestedTypes(graph, nested);
        }
    }

    private void AddType(TypeDefinition type)
    {
        _allTypes.Add(type);
        _adjacency[type] = new HashSet<TypeDefinition>();
    }

    private void AnalyzeType(TypeDefinition type, ModuleDefinition module)
    {
        // Nested types ↔ declaring type (bidirectional, always co-located)
        if (type.DeclaringType is not null)
        {
            AddEdge(type, type.DeclaringType);
            AddEdge(type.DeclaringType, type);
        }

        // Base type
        AddEdgeIfResolvable(type, type.BaseType, module);

        // Interfaces
        foreach (InterfaceImplementation iface in type.Interfaces)
        {
            AddEdgeIfResolvable(type, iface.InterfaceType, module);
        }

        // Fields
        foreach (FieldDefinition field in type.Fields)
        {
            AddEdgeIfResolvable(type, field.FieldType, module);
        }

        // Methods: signatures + bodies
        foreach (MethodDefinition method in type.Methods)
        {
            AddEdgeIfResolvable(type, method.ReturnType, module);

            foreach (ParameterDefinition param in method.Parameters)
            {
                AddEdgeIfResolvable(type, param.ParameterType, module);
            }

            // Generic constraints
            foreach (GenericParameter gp in method.GenericParameters)
            {
                foreach (GenericParameterConstraint constraint in gp.Constraints)
                {
                    AddEdgeIfResolvable(type, constraint.ConstraintType, module);
                }
            }

            if (method.HasBody)
            {
                AnalyzeMethodBody(type, method.Body, module);
            }
        }

        // Generic type constraints
        foreach (GenericParameter gp in type.GenericParameters)
        {
            foreach (GenericParameterConstraint constraint in gp.Constraints)
            {
                AddEdgeIfResolvable(type, constraint.ConstraintType, module);
            }
        }

        // Custom attributes
        foreach (CustomAttribute attr in type.CustomAttributes)
        {
            AddEdgeIfResolvable(type, attr.AttributeType, module);
        }
    }

    private void AnalyzeMethodBody(TypeDefinition owner, MethodBody body, ModuleDefinition module)
    {
        foreach (Instruction instr in body.Instructions)
        {
            switch (instr.Operand)
            {
                case TypeReference typeRef:
                    AddEdgeIfResolvable(owner, typeRef, module);
                    break;
                case MethodReference methodRef:
                    AddEdgeIfResolvable(owner, methodRef.DeclaringType, module);
                    break;
                case FieldReference fieldRef:
                    AddEdgeIfResolvable(owner, fieldRef.DeclaringType, module);
                    break;
            }
        }
    }

    private void AddEdgeIfResolvable(TypeDefinition from, TypeReference? typeRef, ModuleDefinition module)
    {
        if (typeRef is null)
        {
            return;
        }

        // Traverse generic type arguments
        if (typeRef is GenericInstanceType git)
        {
            foreach (TypeReference arg in git.GenericArguments)
            {
                AddEdgeIfResolvable(from, arg, module);
            }
        }

        TypeDefinition? resolved = ResolveToDefinitionInModule(typeRef, module);
        if (resolved is not null && resolved != from)
        {
            AddEdge(from, resolved);
        }
    }

    private void AddEdge(TypeDefinition from, TypeDefinition to)
    {
        if (_adjacency.TryGetValue(from, out HashSet<TypeDefinition>? edges))
        {
            edges.Add(to);
        }
    }

    private static TypeDefinition? ResolveToDefinitionInModule(TypeReference? typeRef, ModuleDefinition module)
    {
        if (typeRef is null)
        {
            return null;
        }

        // Unwrap generic instances, arrays, byrefs, pointers
        TypeReference elementType = typeRef;
        while (elementType is GenericInstanceType git)
        {
            elementType = git.ElementType;
        }

        while (elementType is TypeSpecification spec)
        {
            elementType = spec.ElementType;
        }

        // GenericParameter and certain TypeSpecification subclasses may yield null
        if (elementType is null or GenericParameter)
        {
            return null;
        }

        // Only consider types defined in the same module
        if (elementType.Scope != module)
        {
            return null;
        }

        try
        {
            TypeDefinition? resolved = elementType.Resolve();
            if (resolved?.Module == module)
            {
                return resolved;
            }
        }
        catch
        {
            // If resolution fails, skip this edge
        }

        return null;
    }
}
