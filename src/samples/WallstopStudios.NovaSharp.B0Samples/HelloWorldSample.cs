namespace WallstopStudios.NovaSharp.B0Samples
{
    using System.Collections.Generic;
    using Facade = global::NovaSharp;

    internal static class HelloWorldSample
    {
        public static HelloWorldResult Run()
        {
            List<string> output = new List<string>();
            Facade.LuaEngineOptions options = Facade.LuaEngineOptions.Default;
            options.Print = output.Add;

            using Facade.LuaEngine lua = Facade.LuaEngine.Create(options);
            Facade.LuaValue answer = lua.Run(
                @"
print('Hello from Lua!')
return 40 + 2"
            );

            string printedLine = output.Count == 0 ? string.Empty : output[0];
            return new HelloWorldResult(printedLine, answer.AsInteger());
        }
    }

    internal sealed class HelloWorldResult
    {
        public HelloWorldResult(string printedLine, long answer)
        {
            PrintedLine = printedLine;
            Answer = answer;
        }

        public string PrintedLine { get; }

        public long Answer { get; }
    }
}
