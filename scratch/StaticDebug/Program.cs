using System;
using NovaSharp.Interpreter;
using NovaSharp.Interpreter.DataTypes;
using NovaSharp.Interpreter.Interop;
using NovaSharp.Interpreter.Modules;

public static class Program
{
    public static void Main()
    {
        RunTest(InteropAccessMode.Reflection);
        RunTest(InteropAccessMode.LazyOptimized);
        RunTest(InteropAccessMode.Preoptimized);
    }

    private static void RunTest(InteropAccessMode mode)
    {
        try
        {
            UserData.UnregisterType(typeof(SomeClass));
        }
        catch { }

        UserData.RegisterType(typeof(SomeClass), mode, nameof(SomeClass));

        SomeClass.Reset();
        Script script = new Script();
        script.Globals.Set("static", UserData.CreateStatic<SomeClass>());
        script.DoString("static.StaticProp = 'asdasd' .. static.StaticProp;");
        Console.WriteLine($"{mode}: count={SomeClass.SetterCount}, value={SomeClass.StaticProp}");
    }

    private sealed class SomeClass
    {
        private static string _staticProp = "qweqwe";

        public static int SetterCount { get; private set; }

        public static string StaticProp
        {
            get => _staticProp;
            set
            {
                SetterCount++;
                _staticProp = value;
            }
        }

        public static void Reset()
        {
            _staticProp = "qweqwe";
            SetterCount = 0;
        }
    }
}
