// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace ILSplit;

internal static class AssemblyRewriter
{
    public static void Rewrite(AssemblyDefinition original, List<Cluster> clusters, string outputDir, HashSet<string>? shellPinnedTypes = null)
    {
        string originalName = original.Name.Name;
        string inputPath = original.MainModule.FileName;

        Dictionary<string, int> typeNameToCluster = new();
        foreach (Cluster cluster in clusters)
        {
            foreach (TypeDefinition type in cluster.Types)
            {
                typeNameToCluster[type.FullName] = cluster.Index;
            }
        }

        // Expand shell-pinned types to include nested types (nested types must
        // stay with their declaring type and cannot be split to a different assembly).
        HashSet<string>? expandedPinnedTypes = null;
        if (shellPinnedTypes is not null && shellPinnedTypes.Count > 0)
        {
            expandedPinnedTypes = ExpandWithNestedTypes(original.MainModule, shellPinnedTypes);
        }

        // Write the forwarder shell (OriginalName.dll). When shellPinnedTypes is
        // provided, the shell is a hybrid: VM-pinned types stay as TypeDefs so
        // the VM can find them without following type forwarders, while all other
        // types are forwarded to their respective chunk assemblies.
        WriteForwarderShell(inputPath, originalName, original.Name.Version,
            clusters, typeNameToCluster, outputDir, expandedPinnedTypes);

        // Write all chunk assemblies (hot = .0.dll, cold = .1.dll, .2.dll, ...)
        // using the same code path. The hot chunk is the DAG root — it has no
        // AssemblyRef to cold chunks because the transitive closure guarantees
        // all its type dependencies are self-contained.
        foreach (Cluster cluster in clusters)
        {
            WriteChunkAssembly(inputPath, originalName, original.Name.Version,
                cluster, clusters, typeNameToCluster, outputDir, expandedPinnedTypes);
        }
    }

    private static HashSet<string> ExpandWithNestedTypes(ModuleDefinition module, HashSet<string> pinnedTypes)
    {
        HashSet<string> expanded = new(pinnedTypes, StringComparer.Ordinal);

        // GetAllTypes yields parents before children, so a single pass captures
        // transitively nested types.
        foreach (TypeDefinition type in GetAllTypes(module))
        {
            if (type.DeclaringType is not null && expanded.Contains(type.DeclaringType.FullName))
            {
                expanded.Add(type.FullName);
            }
        }

        return expanded;
    }

    private static void WriteChunkAssembly(
        string inputPath,
        string originalName,
        System.Version version,
        Cluster targetCluster,
        List<Cluster> allClusters,
        Dictionary<string, int> typeNameToCluster,
        string outputDir,
        HashSet<string>? shellPinnedTypes)
    {
        // Use a resolver that can resolve chunk assembly references by loading the
        // original (unsplit) assembly. This is needed because Cecil resolves types
        // during Write (e.g., to determine enum underlying types for constants).
        ChunkAssemblyResolver resolver = new(inputPath, originalName, allClusters.Count);

        using AssemblyDefinition asm = AssemblyDefinition.ReadAssembly(
            inputPath, new ReaderParameters
            {
                ReadSymbols = false,
                // Use Deferred mode so custom attribute arguments aren't eagerly resolved.
                // Resolved attributes force Cecil to re-serialize from parsed arguments,
                // which fails when the argument type is an enum moved to another chunk.
                ReadingMode = ReadingMode.Deferred,
                AssemblyResolver = resolver,
            });
        ModuleDefinition module = asm.MainModule;

        string chunkName = $"{originalName}.{targetCluster.Index}";
        asm.Name = new AssemblyNameDefinition(chunkName, version);
        module.Name = $"{chunkName}.dll";

        HashSet<string> keepTypes = new();
        foreach (TypeDefinition type in targetCluster.Types)
        {
            keepTypes.Add(type.FullName);
        }

        // Nested types must stay with their parent — expand keepTypes to include
        // all transitively nested types of any kept type.
        foreach (TypeDefinition type in GetAllTypes(module))
        {
            if (type.DeclaringType is not null && keepTypes.Contains(type.DeclaringType.FullName))
            {
                keepTypes.Add(type.FullName);
            }
        }

        // Exclude shell-pinned types — they stay as TypeDefs in the forwarder
        // shell so the VM can resolve them without following type forwarders.
        if (shellPinnedTypes is not null)
        {
            keepTypes.ExceptWith(shellPinnedTypes);
        }

        // Prepare potential AssemblyNameReferences for other chunks. We add them
        // all upfront so BuildRemapTable can reference them, then strip unused ones
        // after type removal. This ensures the hot chunk (whose transitive closure is
        // self-contained) ends up with zero AssemblyRef entries to cold chunks.
        Dictionary<int, AssemblyNameReference> chunkRefs = new();
        foreach (Cluster other in allClusters)
        {
            if (other.Index == targetCluster.Index)
            {
                continue;
            }

            // All chunks use the .N naming scheme
            string otherName = $"{originalName}.{other.Index}";
            AssemblyNameReference asmRef = new(otherName, version);
            module.AssemblyReferences.Add(asmRef);
            chunkRefs[other.Index] = asmRef;
        }

        // Add AssemblyRef to the forwarder shell for references to shell-pinned types.
        // Shell-pinned types live in the shell (original assembly name), so chunk
        // code that references them needs a TypeRef with the shell as scope.
        AssemblyNameReference? shellScope = null;
        if (shellPinnedTypes is not null && shellPinnedTypes.Count > 0)
        {
            shellScope = new AssemblyNameReference(originalName, version);
            module.AssemblyReferences.Add(shellScope);
            chunkRefs[-1] = shellScope;
        }

        Dictionary<TypeDefinition, TypeReference> remap =
            BuildRemapTable(module, keepTypes, typeNameToCluster, chunkRefs, shellPinnedTypes, shellScope);

        RemapAllReferences(module, remap, keepTypes);

        // Remove top-level types not kept in this chunk
        List<TypeDefinition> toRemove = module.Types
            .Where(t => t.Name != "<Module>" && !keepTypes.Contains(t.FullName))
            .ToList();

        foreach (TypeDefinition type in toRemove)
        {
            module.Types.Remove(type);
        }

        // Remove nested types that are in other chunks (their parent is kept but they aren't)
        foreach (TypeDefinition type in module.Types)
        {
            if (type.Name != "<Module>")
            {
                RemoveUnkeptNestedTypes(type, keepTypes);
            }
        }

        // Resources stay in the forwarder shell, not in chunks
        module.Resources.Clear();

        // Strip AssemblyRef entries that are no longer referenced by any remaining
        // TypeRef, MemberRef, or ExportedType. This ensures the hot chunk (whose
        // transitive closure is self-contained) has zero AssemblyRef to cold chunks.
        StripUnusedAssemblyReferences(module, chunkRefs);

        // Strip strong name — rewriting invalidates the signature.
        StripStrongName(asm);

        string chunkPath = Path.Combine(outputDir, $"{chunkName}.dll");
        asm.Write(chunkPath);
    }

    private static void RemoveUnkeptNestedTypes(TypeDefinition parent, HashSet<string> keepTypes)
    {
        List<TypeDefinition>? toRemove = null;
        foreach (TypeDefinition nested in parent.NestedTypes)
        {
            if (!keepTypes.Contains(nested.FullName))
            {
                toRemove ??= new();
                toRemove.Add(nested);
            }
            else
            {
                RemoveUnkeptNestedTypes(nested, keepTypes);
            }
        }

        if (toRemove is not null)
        {
            foreach (TypeDefinition nested in toRemove)
            {
                parent.NestedTypes.Remove(nested);
            }
        }
    }

    private static Dictionary<TypeDefinition, TypeReference> BuildRemapTable(
        ModuleDefinition module,
        HashSet<string> keepTypes,
        Dictionary<string, int> typeNameToCluster,
        Dictionary<int, AssemblyNameReference> chunkRefs,
        HashSet<string>? shellPinnedTypes = null,
        AssemblyNameReference? shellScope = null)
    {
        Dictionary<TypeDefinition, TypeReference> remap = new();

        foreach (TypeDefinition type in GetAllTypes(module))
        {
            if (type.Name == "<Module>" || keepTypes.Contains(type.FullName))
            {
                continue;
            }

            // Shell-pinned types remap to the forwarder shell (original assembly)
            // rather than to a chunk assembly.
            if (shellPinnedTypes is not null && shellScope is not null
                && shellPinnedTypes.Contains(type.FullName))
            {
                if (type.DeclaringType is not null
                    && remap.TryGetValue(type.DeclaringType, out TypeReference? parentRef))
                {
                    remap[type] = new TypeReference(type.Namespace, type.Name, module, null)
                    {
                        DeclaringType = parentRef,
                    };
                }
                else
                {
                    remap[type] = new TypeReference(type.Namespace, type.Name, module, shellScope);
                }

                continue;
            }

            if (!typeNameToCluster.TryGetValue(type.FullName, out int ownerCluster)
                || !chunkRefs.TryGetValue(ownerCluster, out AssemblyNameReference? scope))
            {
                continue;
            }

            if (type.DeclaringType is not null
                && remap.TryGetValue(type.DeclaringType, out TypeReference? declaringRef))
            {
                remap[type] = new TypeReference(type.Namespace, type.Name, module, null)
                {
                    DeclaringType = declaringRef,
                };
            }
            else
            {
                remap[type] = new TypeReference(type.Namespace, type.Name, module, scope);
            }
        }

        return remap;
    }

    /// <summary>
    /// Writes the forwarder shell assembly with the original assembly name.
    /// When <paramref name="shellPinnedTypes"/> is provided (e.g. for CoreLib),
    /// the shell is a hybrid: VM-pinned types stay as TypeDefs so the VM can
    /// find them directly, while all other types are forwarded to chunk assemblies.
    /// When <paramref name="shellPinnedTypes"/> is null, the shell is a pure
    /// forwarder with ExportedType entries for all types.
    /// Resources are preserved in the shell in both cases.
    /// </summary>
    private static void WriteForwarderShell(
        string inputPath,
        string originalName,
        System.Version version,
        List<Cluster> allClusters,
        Dictionary<string, int> typeNameToCluster,
        string outputDir,
        HashSet<string>? shellPinnedTypes)
    {
        bool isHybrid = shellPinnedTypes is not null && shellPinnedTypes.Count > 0;

        using AssemblyDefinition asm = AssemblyDefinition.ReadAssembly(
            inputPath, new ReaderParameters
            {
                ReadSymbols = false,
                ReadingMode = ReadingMode.Deferred,
                AssemblyResolver = isHybrid
                    ? new ChunkAssemblyResolver(inputPath, originalName, allClusters.Count)
                    : null,
            });
        ModuleDefinition module = asm.MainModule;

        // Add AssemblyRef to every chunk assembly (needed as scope for forwarders
        // and, in hybrid mode, for TypeRefs from pinned type code).
        Dictionary<int, AssemblyNameReference> chunkRefs = new();
        foreach (Cluster cluster in allClusters)
        {
            string chunkName = $"{originalName}.{cluster.Index}";
            AssemblyNameReference asmRef = new(chunkName, version);
            module.AssemblyReferences.Add(asmRef);
            chunkRefs[cluster.Index] = asmRef;
        }

        // Build the set of types to keep as TypeDefs in the shell.
        // In hybrid mode these are the VM-pinned types; otherwise empty (pure forwarder).
        HashSet<string> keepTypes = new();
        if (isHybrid)
        {
            foreach (TypeDefinition type in GetAllTypes(module))
            {
                if (shellPinnedTypes!.Contains(type.FullName))
                {
                    keepTypes.Add(type.FullName);
                }
            }

            // Remap references from pinned types to types living in chunk assemblies
            Dictionary<TypeDefinition, TypeReference> remap =
                BuildRemapTable(module, keepTypes, typeNameToCluster, chunkRefs);
            RemapAllReferences(module, remap, keepTypes);
        }

        // Add ExportedType forwarders for types NOT kept in the shell
        Dictionary<string, ExportedType> exportedByFullName = new();
        foreach (TypeDefinition type in GetAllTypes(module))
        {
            if (type.Name == "<Module>" || keepTypes.Contains(type.FullName))
            {
                continue;
            }

            if (!typeNameToCluster.TryGetValue(type.FullName, out int clusterIndex)
                || !chunkRefs.TryGetValue(clusterIndex, out AssemblyNameReference? asmRef))
            {
                continue;
            }

            ExportedType exported;
            if (type.DeclaringType is not null
                && exportedByFullName.TryGetValue(type.DeclaringType.FullName, out ExportedType? parentExported))
            {
                exported = new ExportedType(type.Namespace, type.Name, module, asmRef)
                {
                    Attributes = TypeAttributes.Forwarder,
                    DeclaringType = parentExported,
                };
            }
            else
            {
                exported = new ExportedType(type.Namespace, type.Name, module, asmRef)
                {
                    Attributes = TypeAttributes.Forwarder,
                };
            }

            module.ExportedTypes.Add(exported);
            exportedByFullName[type.FullName] = exported;
        }

        // Remove types NOT kept in the shell
        List<TypeDefinition> toRemove = module.Types
            .Where(t => t.Name != "<Module>" && !keepTypes.Contains(t.FullName))
            .ToList();

        foreach (TypeDefinition type in toRemove)
        {
            module.Types.Remove(type);
        }

        if (isHybrid)
        {
            // Remove non-kept nested types within kept types
            foreach (TypeDefinition type in module.Types)
            {
                if (type.Name != "<Module>")
                {
                    RemoveUnkeptNestedTypes(type, keepTypes);
                }
            }

            // Strip unused chunk AssemblyRef entries
            StripUnusedAssemblyReferences(module, chunkRefs);
        }
        else
        {
            // Pure forwarder: clear custom attributes (they belong in the chunks)
            module.CustomAttributes.Clear();
            module.Assembly.CustomAttributes.Clear();
        }

        // Resources stay in the forwarder shell

        // Strip strong name — rewriting invalidates the signature.
        // Preserve identity so the assembly name + public key token remain unchanged.
        StripStrongName(asm, preserveIdentity: true);

        string shellPath = Path.Combine(outputDir, $"{originalName}.dll");
        asm.Write(shellPath);
    }

    private static void StripStrongName(AssemblyDefinition asm, bool preserveIdentity = false)
    {
        if (!preserveIdentity)
        {
            // Chunk assemblies are new — strip the public key entirely.
            asm.Name.HasPublicKey = false;
            asm.Name.PublicKey = Array.Empty<byte>();
        }

        // Always clear the StrongNameSigned module flag so the runtime does not
        // attempt to verify a signature that rewriting invalidated.
        asm.MainModule.Attributes &= ~ModuleAttributes.StrongNameSigned;
    }

    private static void RemapAllReferences(
        ModuleDefinition module,
        Dictionary<TypeDefinition, TypeReference> remap,
        HashSet<string> keepTypes)
    {
        if (remap.Count == 0)
        {
            return;
        }

        foreach (TypeDefinition type in GetAllTypes(module))
        {
            // Skip types being moved to other chunks (they'll be removed entirely)
            if (remap.ContainsKey(type))
            {
                continue;
            }

            // Remap references in kept types and <Module>
            if (type.Name == "<Module>" || keepTypes.Contains(type.FullName))
            {
                RemapType(type, remap);
            }
        }

        // Remap module-level and assembly-level custom attributes
        RemapCustomAttributes(module, remap);
        RemapCustomAttributes(module.Assembly, remap);
    }

    /// <summary>
    /// Removes chunk AssemblyRef entries that are not referenced by any remaining
    /// type metadata in the module. After type removal, the hot chunk typically has
    /// no references to cold chunks, so their AssemblyRef entries should be stripped.
    /// We walk all remaining types' fields, methods, interfaces, etc. because
    /// module.GetTypeReferences() doesn't include dynamically-created TypeReference
    /// objects from the remap phase.
    /// </summary>
    private static void StripUnusedAssemblyReferences(
        ModuleDefinition module,
        Dictionary<int, AssemblyNameReference> chunkRefs)
    {
        HashSet<AssemblyNameReference> usedRefs = new(ReferenceEqualityComparer.Instance);

        // Walk all remaining types and their members to find referenced scopes
        foreach (TypeDefinition type in GetAllTypes(module))
        {
            CollectScopeRef(type.BaseType, usedRefs);

            foreach (InterfaceImplementation iface in type.Interfaces)
            {
                CollectScopeRef(iface.InterfaceType, usedRefs);
            }

            foreach (FieldDefinition field in type.Fields)
            {
                CollectScopeRef(field.FieldType, usedRefs);
            }

            foreach (MethodDefinition method in type.Methods)
            {
                CollectScopeRef(method.ReturnType, usedRefs);

                foreach (ParameterDefinition param in method.Parameters)
                {
                    CollectScopeRef(param.ParameterType, usedRefs);
                }

                if (method.HasBody)
                {
                    foreach (Instruction instr in method.Body.Instructions)
                    {
                        switch (instr.Operand)
                        {
                            case TypeReference tr:
                                CollectScopeRef(tr, usedRefs);
                                break;
                            case MethodReference mr:
                                CollectScopeRef(mr.DeclaringType, usedRefs);
                                CollectScopeRef(mr.ReturnType, usedRefs);
                                foreach (ParameterDefinition p in mr.Parameters)
                                {
                                    CollectScopeRef(p.ParameterType, usedRefs);
                                }

                                break;
                            case FieldReference fr:
                                CollectScopeRef(fr.DeclaringType, usedRefs);
                                CollectScopeRef(fr.FieldType, usedRefs);
                                break;
                        }
                    }
                }
            }

            foreach (GenericParameter gp in type.GenericParameters)
            {
                foreach (GenericParameterConstraint constraint in gp.Constraints)
                {
                    CollectScopeRef(constraint.ConstraintType, usedRefs);
                }
            }
        }

        foreach (ExportedType et in module.ExportedTypes)
        {
            if (et.Scope is AssemblyNameReference asmRef)
            {
                usedRefs.Add(asmRef);
            }
        }

        // Remove chunk AssemblyRef entries that are not used
        foreach (AssemblyNameReference asmRef in chunkRefs.Values)
        {
            if (!usedRefs.Contains(asmRef))
            {
                module.AssemblyReferences.Remove(asmRef);
            }
        }
    }

    private static void CollectScopeRef(TypeReference? typeRef, HashSet<AssemblyNameReference> refs)
    {
        if (typeRef is null)
        {
            return;
        }

        // Unwrap to the element/declaring type to find the scope
        TypeReference current = typeRef;
        while (current is TypeSpecification spec)
        {
            if (spec.ElementType is null)
            {
                return;
            }

            current = spec.ElementType;
        }

        while (current.DeclaringType is not null)
        {
            current = current.DeclaringType;
        }

        if (current.Scope is AssemblyNameReference asmRef)
        {
            refs.Add(asmRef);
        }

        // Also walk generic arguments
        if (typeRef is GenericInstanceType git)
        {
            foreach (TypeReference arg in git.GenericArguments)
            {
                CollectScopeRef(arg, refs);
            }
        }
    }

    private static void RemapType(TypeDefinition type, Dictionary<TypeDefinition, TypeReference> remap)
    {
        type.BaseType = RemapTypeRef(type.BaseType, remap);

        for (int i = 0; i < type.Interfaces.Count; i++)
        {
            TypeReference? remapped = RemapTypeRef(type.Interfaces[i].InterfaceType, remap);
            if (remapped != type.Interfaces[i].InterfaceType)
            {
                type.Interfaces[i] = new InterfaceImplementation(remapped);
            }

            RemapCustomAttributes(type.Interfaces[i], remap);
        }

        foreach (FieldDefinition field in type.Fields)
        {
            field.FieldType = RemapTypeRef(field.FieldType, remap)!;
            RemapCustomAttributes(field, remap);
        }

        foreach (MethodDefinition method in type.Methods)
        {
            method.ReturnType = RemapTypeRef(method.ReturnType, remap)!;

            if (method.MethodReturnType is not null)
            {
                RemapCustomAttributes(method.MethodReturnType, remap);
            }

            foreach (ParameterDefinition param in method.Parameters)
            {
                param.ParameterType = RemapTypeRef(param.ParameterType, remap)!;
                RemapCustomAttributes(param, remap);
            }

            foreach (GenericParameter gp in method.GenericParameters)
            {
                RemapConstraints(gp, remap);
                RemapCustomAttributes(gp, remap);
            }

            if (method.HasBody)
            {
                RemapMethodBody(method, remap);
            }

            // Remap explicit interface impl overrides
            for (int i = 0; i < method.Overrides.Count; i++)
            {
                method.Overrides[i] = RemapMethodRef(method.Overrides[i], remap);
            }

            RemapCustomAttributes(method, remap);
        }

        foreach (GenericParameter gp in type.GenericParameters)
        {
            RemapConstraints(gp, remap);
            RemapCustomAttributes(gp, remap);
        }

        foreach (PropertyDefinition prop in type.Properties)
        {
            prop.PropertyType = RemapTypeRef(prop.PropertyType, remap)!;
            RemapCustomAttributes(prop, remap);
        }

        foreach (EventDefinition evt in type.Events)
        {
            evt.EventType = RemapTypeRef(evt.EventType, remap)!;
            RemapCustomAttributes(evt, remap);
        }

        RemapCustomAttributes(type, remap);
    }

    private static void RemapCustomAttributes(
        ICustomAttributeProvider provider,
        Dictionary<TypeDefinition, TypeReference> remap)
    {
        if (!provider.HasCustomAttributes)
        {
            return;
        }

        for (int i = 0; i < provider.CustomAttributes.Count; i++)
        {
            CustomAttribute attr = provider.CustomAttributes[i];
            MethodReference remappedCtor = RemapMethodRef(attr.Constructor, remap);
            if (remappedCtor != attr.Constructor)
            {
                provider.CustomAttributes[i] = new CustomAttribute(remappedCtor, attr.GetBlob());
            }
        }
    }

    private static void RemapConstraints(GenericParameter gp, Dictionary<TypeDefinition, TypeReference> remap)
    {
        for (int i = 0; i < gp.Constraints.Count; i++)
        {
            TypeReference? remapped = RemapTypeRef(gp.Constraints[i].ConstraintType, remap);
            if (remapped != gp.Constraints[i].ConstraintType)
            {
                gp.Constraints[i] = new GenericParameterConstraint(remapped);
            }

            RemapCustomAttributes(gp.Constraints[i], remap);
        }
    }

    private static void RemapMethodBody(MethodDefinition method, Dictionary<TypeDefinition, TypeReference> remap)
    {
        MethodBody body = method.Body;

        foreach (Instruction instr in body.Instructions)
        {
            switch (instr.Operand)
            {
                case TypeReference typeRef:
                    instr.Operand = RemapTypeRef(typeRef, remap);
                    break;
                case MethodReference methodRef:
                    instr.Operand = RemapMethodRef(methodRef, remap);
                    break;
                case FieldReference fieldRef:
                    instr.Operand = RemapFieldRef(fieldRef, remap);
                    break;
                case CallSite callSite:
                    RemapCallSite(callSite, remap);
                    break;
            }
        }

        if (body.HasExceptionHandlers)
        {
            foreach (ExceptionHandler handler in body.ExceptionHandlers)
            {
                if (handler.CatchType is not null)
                {
                    handler.CatchType = RemapTypeRef(handler.CatchType, remap);
                }
            }
        }

        if (body.HasVariables)
        {
            foreach (VariableDefinition variable in body.Variables)
            {
                variable.VariableType = RemapTypeRef(variable.VariableType, remap)!;
            }
        }

        // Create a new MethodBody to force Cecil to use the resolved write path.
        // Without this, Cecil reuses cached raw IL bytes and patches tokens using
        // the original (pre-remap) metadata references, causing "declared in another
        // module" errors during Write.
        MethodBody newBody = new(method)
        {
            MaxStackSize = body.MaxStackSize,
            InitLocals = body.InitLocals,
        };

        foreach (VariableDefinition variable in body.Variables)
        {
            newBody.Variables.Add(variable);
        }

        foreach (Instruction instr in body.Instructions)
        {
            newBody.Instructions.Add(instr);
        }

        foreach (ExceptionHandler handler in body.ExceptionHandlers)
        {
            newBody.ExceptionHandlers.Add(handler);
        }

        method.Body = newBody;
    }

    private static TypeReference? RemapTypeRef(TypeReference? typeRef, Dictionary<TypeDefinition, TypeReference> remap)
    {
        if (typeRef is null)
        {
            return null;
        }

        if (typeRef is TypeDefinition td && remap.TryGetValue(td, out TypeReference? replacement))
        {
            return replacement;
        }

        if (typeRef is GenericInstanceType git)
        {
            TypeReference? remappedElement = RemapTypeRef(git.ElementType, remap);
            GenericInstanceType newGit = new(remappedElement);
            bool changed = remappedElement != git.ElementType;

            foreach (TypeReference arg in git.GenericArguments)
            {
                TypeReference? remapped = RemapTypeRef(arg, remap)!;
                newGit.GenericArguments.Add(remapped);
                if (remapped != arg)
                {
                    changed = true;
                }
            }

            return changed ? newGit : typeRef;
        }

        if (typeRef is ArrayType arrayType)
        {
            TypeReference? remappedElement = RemapTypeRef(arrayType.ElementType, remap);

            return remappedElement == arrayType.ElementType
                ? typeRef
                : new ArrayType(remappedElement, arrayType.Rank);
        }

        if (typeRef is ByReferenceType byRef)
        {
            TypeReference? remappedElement = RemapTypeRef(byRef.ElementType, remap);

            return remappedElement == byRef.ElementType
                ? typeRef
                : new ByReferenceType(remappedElement);
        }

        if (typeRef is PointerType ptr)
        {
            TypeReference? remappedElement = RemapTypeRef(ptr.ElementType, remap);

            return remappedElement == ptr.ElementType
                ? typeRef
                : new PointerType(remappedElement);
        }

        if (typeRef is PinnedType pinned)
        {
            TypeReference? remappedElement = RemapTypeRef(pinned.ElementType, remap);

            return remappedElement == pinned.ElementType
                ? typeRef
                : new PinnedType(remappedElement);
        }

        if (typeRef is RequiredModifierType rmod)
        {
            TypeReference? remappedElement = RemapTypeRef(rmod.ElementType, remap);
            TypeReference? remappedModifier = RemapTypeRef(rmod.ModifierType, remap);

            return remappedElement == rmod.ElementType && remappedModifier == rmod.ModifierType
                ? typeRef
                : new RequiredModifierType(remappedModifier, remappedElement);
        }

        if (typeRef is OptionalModifierType omod)
        {
            TypeReference? remappedElement = RemapTypeRef(omod.ElementType, remap);
            TypeReference? remappedModifier = RemapTypeRef(omod.ModifierType, remap);

            return remappedElement == omod.ElementType && remappedModifier == omod.ModifierType
                ? typeRef
                : new OptionalModifierType(remappedModifier, remappedElement);
        }

        if (typeRef is FunctionPointerType fpt)
        {
            TypeReference? remappedReturn = RemapTypeRef(fpt.ReturnType, remap);
            bool changed = remappedReturn != fpt.ReturnType;

            List<TypeReference?>? remappedParams = null;
            for (int i = 0; i < fpt.Parameters.Count; i++)
            {
                TypeReference? remapped = RemapTypeRef(fpt.Parameters[i].ParameterType, remap);
                if (remapped != fpt.Parameters[i].ParameterType)
                {
                    changed = true;
                    remappedParams ??= new List<TypeReference?>(new TypeReference?[fpt.Parameters.Count]);
                    remappedParams[i] = remapped;
                }
            }

            if (!changed)
            {
                return typeRef;
            }

            FunctionPointerType newFpt = new()
            {
                ReturnType = remappedReturn!,
                HasThis = fpt.HasThis,
                ExplicitThis = fpt.ExplicitThis,
                CallingConvention = fpt.CallingConvention,
            };
            for (int i = 0; i < fpt.Parameters.Count; i++)
            {
                TypeReference? paramType = remappedParams is not null && remappedParams[i] is not null
                    ? remappedParams[i]!
                    : fpt.Parameters[i].ParameterType;
                newFpt.Parameters.Add(new ParameterDefinition(fpt.Parameters[i].Name, fpt.Parameters[i].Attributes, paramType));
            }

            return newFpt;
        }

        return typeRef;
    }

    private static MethodReference RemapMethodRef(MethodReference methodRef, Dictionary<TypeDefinition, TypeReference> remap)
    {
        TypeReference? remappedDeclaringType = RemapTypeRef(methodRef.DeclaringType, remap);
        TypeReference? remappedReturnType = RemapTypeRef(methodRef.ReturnType, remap);

        bool changed = remappedDeclaringType != methodRef.DeclaringType
                     || remappedReturnType != methodRef.ReturnType;

        // Check if any parameter types need remapping
        List<TypeReference?>? remappedParamTypes = null;
        for (int i = 0; i < methodRef.Parameters.Count; i++)
        {
            TypeReference? remapped = RemapTypeRef(methodRef.Parameters[i].ParameterType, remap);
            if (remapped != methodRef.Parameters[i].ParameterType)
            {
                changed = true;
                remappedParamTypes ??= new List<TypeReference?>(new TypeReference?[methodRef.Parameters.Count]);
                remappedParamTypes[i] = remapped;
            }
        }

        // Check generic instance arguments
        if (methodRef is GenericInstanceMethod gim)
        {
            foreach (TypeReference arg in gim.GenericArguments)
            {
                TypeReference? remapped = RemapTypeRef(arg, remap);
                if (remapped != arg)
                {
                    changed = true;
                    break;
                }
            }
        }

        if (!changed)
        {
            return methodRef;
        }

        MethodReference newRef = new(methodRef.Name, remappedReturnType, remappedDeclaringType)
        {
            HasThis = methodRef.HasThis,
            ExplicitThis = methodRef.ExplicitThis,
            CallingConvention = methodRef.CallingConvention,
        };

        for (int i = 0; i < methodRef.Parameters.Count; i++)
        {
            ParameterDefinition param = methodRef.Parameters[i];
            TypeReference? paramType = remappedParamTypes is not null && remappedParamTypes[i] is not null
                ? remappedParamTypes[i]!
                : param.ParameterType;
            newRef.Parameters.Add(new ParameterDefinition(param.Name, param.Attributes, paramType));
        }

        foreach (GenericParameter gp in methodRef.GenericParameters)
        {
            newRef.GenericParameters.Add(new GenericParameter(gp.Name, newRef));
        }

        if (methodRef is GenericInstanceMethod gim2)
        {
            GenericInstanceMethod newGim = new(newRef);
            foreach (TypeReference arg in gim2.GenericArguments)
            {
                newGim.GenericArguments.Add(RemapTypeRef(arg, remap)!);
            }

            return newGim;
        }

        return newRef;
    }

    private static FieldReference RemapFieldRef(FieldReference fieldRef, Dictionary<TypeDefinition, TypeReference> remap)
    {
        TypeReference? remappedDeclaringType = RemapTypeRef(fieldRef.DeclaringType, remap);
        TypeReference? remappedFieldType = RemapTypeRef(fieldRef.FieldType, remap);

        if (remappedDeclaringType == fieldRef.DeclaringType && remappedFieldType == fieldRef.FieldType)
        {
            return fieldRef;
        }

        return new FieldReference(fieldRef.Name, remappedFieldType!, remappedDeclaringType);
    }

    private static void RemapCallSite(CallSite callSite, Dictionary<TypeDefinition, TypeReference> remap)
    {
        callSite.ReturnType = RemapTypeRef(callSite.ReturnType, remap)!;
        foreach (ParameterDefinition param in callSite.Parameters)
        {
            param.ParameterType = RemapTypeRef(param.ParameterType, remap)!;
        }
    }

    private static IEnumerable<TypeDefinition> GetAllTypes(ModuleDefinition module)
    {
        foreach (TypeDefinition type in module.Types)
        {
            yield return type;
            foreach (TypeDefinition nested in GetAllNestedTypes(type))
            {
                yield return nested;
            }
        }
    }

    private static IEnumerable<TypeDefinition> GetAllNestedTypes(TypeDefinition type)
    {
        foreach (TypeDefinition nested in type.NestedTypes)
        {
            yield return nested;
            foreach (TypeDefinition deepNested in GetAllNestedTypes(nested))
            {
                yield return deepNested;
            }
        }
    }

    /// <summary>
    /// Resolves chunk assembly references (e.g., "MyApp.1", "MyApp.2") and the
    /// original assembly name by loading the original unsplit assembly, which
    /// contains all type definitions.
    /// Cecil needs this during Write to resolve types for constant encoding, enum
    /// underlying types, and similar metadata operations.
    /// </summary>
    private sealed class ChunkAssemblyResolver : DefaultAssemblyResolver
    {
        private readonly string _inputPath;
        private readonly string _originalName;
        private readonly int _chunkCount;
        private AssemblyDefinition? _cachedOriginal;

        public ChunkAssemblyResolver(string inputPath, string originalName, int chunkCount)
        {
            _inputPath = inputPath;
            _originalName = originalName;
            _chunkCount = chunkCount;
            AddSearchDirectory(Path.GetDirectoryName(inputPath)!);
        }

        public override AssemblyDefinition Resolve(AssemblyNameReference name, ReaderParameters parameters)
        {
            if (IsChunkName(name.Name) || name.Name == _originalName)
            {
                return GetOriginal();
            }

            return base.Resolve(name, parameters);
        }

        public override AssemblyDefinition Resolve(AssemblyNameReference name)
        {
            if (IsChunkName(name.Name) || name.Name == _originalName)
            {
                return GetOriginal();
            }

            return base.Resolve(name);
        }

        private bool IsChunkName(string name)
        {
            if (!name.StartsWith(_originalName + ".", StringComparison.Ordinal))
            {
                return false;
            }

            string suffix = name.Substring(_originalName.Length + 1);

            return int.TryParse(suffix, out int index) && index >= 0 && index < _chunkCount;
        }

        private AssemblyDefinition GetOriginal()
        {
            return _cachedOriginal ??= AssemblyDefinition.ReadAssembly(
                _inputPath,
                new ReaderParameters { ReadSymbols = false, ReadingMode = ReadingMode.Immediate });
        }
    }
}
