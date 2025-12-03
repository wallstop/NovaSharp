using NovaSharp.Interpreter;
using NovaSharp.Interpreter.DataTypes;
using NovaSharp.Interpreter.Interop;

foreach (
    InteropAccessMode mode in new[]
    {
        InteropAccessMode.Reflection,
        InteropAccessMode.LazyOptimized,
        InteropAccessMode.Preoptimized,
    }
)
{
    if (UserData.IsTypeRegistered(typeof(SomeClass)))
    {
        UserData.UnregisterType(typeof(SomeClass));
    }
    UserData.RegisterType(typeof(SomeClass), mode);

    Script script = new();
    SomeClass.StaticProp = "qweqwe";
    script.Globals.Set("static", UserData.CreateStatic(typeof(SomeClass)));
    script.DoString("static.StaticProp = 'asdasd' .. static.StaticProp;");
    Console.WriteLine($"{mode}: {SomeClass.StaticProp}");
}

internal sealed class SomeClass
{
    public static string StaticProp { get; set; } = string.Empty;
}
