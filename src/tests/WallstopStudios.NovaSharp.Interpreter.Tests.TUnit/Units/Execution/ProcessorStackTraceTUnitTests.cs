namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Units.Execution
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Debugging;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Interpreter.Modules;

    public sealed class ProcessorStackTraceTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task CoroutineStackTraceIncludesCurrentFrames()
        {
            Script script = new(CoreModules.PresetComplete);

            script.DoString(
                @"
                function level3()
                    coroutine.yield('pause')
                end

                function level2()
                    level3()
                end

                function level1()
                    level2()
                end
            "
            );

            DynValue coroutineValue = script.CreateCoroutine(script.Globals.Get("level1"));

            coroutineValue.Coroutine.Resume();
            await Assert.That(coroutineValue.Coroutine.State).IsEqualTo(CoroutineState.Suspended);

            WatchItem[] stack = coroutineValue.Coroutine.GetStackTrace(0);
            string[] frameNames = stack
                .Select(w => w.Name)
                .Where(n => !string.IsNullOrEmpty(n))
                .ToArray();

            await Assert.That(frameNames.Any(name => ContainsOrdinal(name, "level3"))).IsTrue();
            await Assert.That(frameNames.Any(name => ContainsOrdinal(name, "level2"))).IsTrue();
            await Assert.That(frameNames.Any(name => ContainsOrdinal(name, "level1"))).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task InterpreterExceptionIncludesCallStackFrames()
        {
            Script script = new(CoreModules.PresetComplete);

            script.DoString(
                @"
                local function level3()
                    return missing_function()
                end

                local function level2()
                    return level3()
                end

                function level1()
                    return level2()
                end
            "
            );

            InterpreterException exception = Assert.Throws<InterpreterException>(() =>
                script.Call(script.Globals.Get("level1"))
            );

            await Assert.That(exception).IsNotNull();
            await Assert.That(exception.CallStack).IsNotNull();
            await Assert.That(exception.CallStack.Count).IsGreaterThanOrEqualTo(3);
            bool hasLevel3 = exception
                .CallStack.Select(w => w.Name)
                .Where(n => !string.IsNullOrEmpty(n))
                .Any(n => ContainsOrdinal(n, "level3"));
            await Assert.That(hasLevel3).IsTrue();
        }

        private static bool ContainsOrdinal(string text, string value)
        {
            return text != null && text.Contains(value, StringComparison.Ordinal);
        }
    }
}
