Set-Location D:/Code/NovaSharp
 = @'
using System;
using NovaSharp.Interpreter;
using NovaSharp.Interpreter.DataTypes;
using NovaSharp.Interpreter.Modules;
class Dummy { }
public static class Runner {
    public static void Main() {
        UserData.RegisterType<Dummy>();
        var script = new Script(CoreModules.PresetComplete);
        var dyn = UserData.Create(new Dummy());
        Console.WriteLine(dyn == null ? \
null\ : dyn.Type.ToString());
    }
}
'@
Add-Type -ReferencedAssemblies src/runtime/NovaSharp.Interpreter/bin/Release/netstandard2.1/NovaSharp.Interpreter.dll -TypeDefinition 
[Runner]::Main()

