using Mono.Cecil;
using Mono.Cecil.Cil;

var fw = @"d:\runtime2\src\mono\sample\wasm\browser\bin\publish\wwwroot\_framework";
var dlls = Directory.GetFiles(fw, "*.dll");

int totalGeneric = 0;
int totalConcrete = 0;

// Track which generic types/methods are instantiated
var typeInstantiations = new Dictionary<string, HashSet<string>>();
var methodInstantiations = new Dictionary<string, HashSet<string>>();
var patternMethods = new HashSet<string>();

foreach (var dllPath in dlls)
{
    using var asm = AssemblyDefinition.ReadAssembly(dllPath);
    foreach (var module in asm.Modules)
    {
        foreach (var type in module.GetTypes())
        {
            foreach (var method in type.Methods)
            {
                if (!method.HasBody) continue;
                var instrs = method.Body.Instructions;

                // Collect instantiations
                foreach (var instr in instrs)
                {
                    if (instr.OpCode.Code != Code.Call && instr.OpCode.Code != Code.Callvirt &&
                        instr.OpCode.Code != Code.Newobj && instr.OpCode.Code != Code.Ldftn &&
                        instr.OpCode.Code != Code.Ldvirtftn)
                        continue;
                    if (instr.Operand is not MethodReference mr) continue;

                    if (mr is GenericInstanceMethod gim)
                    {
                        var key = $"{mr.DeclaringType.FullName}::{gim.ElementMethod.Name}";
                        if (!methodInstantiations.TryGetValue(key, out var set))
                        {
                            set = new HashSet<string>();
                            methodInstantiations[key] = set;
                        }
                        set.Add(string.Join(",", gim.GenericArguments.Select(a => a.FullName)));
                    }

                    if (mr.DeclaringType is GenericInstanceType git)
                    {
                        var key = git.ElementType.FullName;
                        if (!typeInstantiations.TryGetValue(key, out var set))
                        {
                            set = new HashSet<string>();
                            typeInstantiations[key] = set;
                        }
                        set.Add(string.Join(",", git.GenericArguments.Select(a => a.FullName)));
                    }
                }

                // Check for typeof patterns
                for (int i = 4; i < instrs.Count; i++)
                {
                    var call = instrs[i];
                    if (call.OpCode.Code != Code.Call && call.OpCode.Code != Code.Callvirt)
                        continue;
                    if (call.Operand is not MethodReference mr2)
                        continue;
                    if (mr2.DeclaringType.FullName != "System.Type")
                        continue;
                    if (mr2.Name != "op_Equality" && mr2.Name != "op_Inequality")
                        continue;

                    var i0 = instrs[i - 4];
                    var i2 = instrs[i - 2];

                    bool hasGenericParam = false;
                    string type1Str = "?", type2Str = "?";

                    if (i0.OpCode.Code == Code.Ldtoken && i0.Operand is TypeReference t1)
                    {
                        type1Str = t1.FullName;
                        if (t1.ContainsGenericParameter) hasGenericParam = true;
                    }
                    if (i2.OpCode.Code == Code.Ldtoken && i2.Operand is TypeReference t2)
                    {
                        type2Str = t2.FullName;
                        if (t2.ContainsGenericParameter) hasGenericParam = true;
                    }

                    if (i0.OpCode.Code == Code.Ldtoken && i2.OpCode.Code == Code.Ldtoken)
                    {
                        if (hasGenericParam)
                        {
                            totalGeneric++;
                            var methodKey = $"{method.DeclaringType.FullName}::{method.Name}";
                            patternMethods.Add(methodKey);
                        }
                        else
                        {
                            totalConcrete++;
                        }
                    }
                }
            }
        }
    }
}

Console.WriteLine($"Total GENERIC typeof patterns: {totalGeneric}");
Console.WriteLine($"Total CONCRETE typeof patterns: {totalConcrete}");
Console.WriteLine();

// For each pattern method, show instantiation count
Console.WriteLine("=== Pattern methods and their instantiation counts ===");
foreach (var pm in patternMethods.OrderBy(p => p))
{
    var typeName = pm.Split("::")[0];

    // Check type-level instantiations
    int typeInsts = 0;
    if (typeInstantiations.TryGetValue(typeName, out var tset))
        typeInsts = tset.Count;

    // Check method-level instantiations
    int methodInsts = 0;
    if (methodInstantiations.TryGetValue(pm, out var mset))
        methodInsts = mset.Count;

    Console.WriteLine($"  {pm}");
    Console.WriteLine($"    Type insts: {typeInsts}, Method insts: {methodInsts}");
    if (typeInsts > 0 && typeInsts <= 10)
    {
        foreach (var inst in typeInstantiations[typeName].OrderBy(x => x))
            Console.WriteLine($"      T={inst}");
    }
    if (methodInsts > 0 && methodInsts <= 10)
    {
        foreach (var inst in methodInstantiations[pm].OrderBy(x => x))
            Console.WriteLine($"      M<{inst}>");
    }
}
