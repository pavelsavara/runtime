#r "d:\bin\ilspy\Mono.Cecil.dll"

using Mono.Cecil;
using Mono.Cecil.Cil;

var fw = @"d:\runtime2\src\mono\sample\wasm\browser\bin\publish\wwwroot\_framework";
var dlls = System.IO.Directory.GetFiles(fw, "*.dll");

int totalPatterns = 0;

foreach (var dllPath in dlls)
{
    var asm = AssemblyDefinition.ReadAssembly(dllPath);
    foreach (var module in asm.Modules)
    {
        foreach (var type in module.GetTypes())
        {
            foreach (var method in type.Methods)
            {
                if (!method.HasBody) continue;
                var instrs = method.Body.Instructions;
                for (int i = 4; i < instrs.Count; i++)
                {
                    var call = instrs[i];
                    if (call.OpCode.Code != Code.Call && call.OpCode.Code != Code.Callvirt)
                        continue;
                    if (call.Operand is not MethodReference mr)
                        continue;
                    if (mr.DeclaringType.FullName != "System.Type")
                        continue;
                    if (mr.Name != "op_Equality" && mr.Name != "op_Inequality")
                        continue;

                    // Check if any of the ldtoken operands contain a generic parameter
                    var i0 = instrs[i - 4];
                    var i2 = instrs[i - 2];
                    
                    bool hasGenericParam = false;
                    bool hasConcrete = false;
                    string type1Str = "", type2Str = "";

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

                    // Also check concrete typeof==typeof patterns
                    if (i0.OpCode.Code == Code.Ldtoken && i2.OpCode.Code == Code.Ldtoken)
                        hasConcrete = true;

                    if (hasGenericParam || hasConcrete)
                    {
                        totalPatterns++;
                        string kind = hasGenericParam ? "GENERIC" : "CONCRETE";
                        Console.WriteLine($"[{kind}] {method.DeclaringType.FullName}::{method.Name}");
                        Console.WriteLine($"  {mr.Name}({type1Str}, {type2Str})");
                        Console.WriteLine();
                    }
                }
            }
        }
    }
    asm.Dispose();
}

Console.WriteLine($"\nTotal typeof patterns found: {totalPatterns}");
