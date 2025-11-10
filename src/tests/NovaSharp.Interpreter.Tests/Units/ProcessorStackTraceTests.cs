namespace NovaSharp.Interpreter.Tests.Units
{
    using System.Linq;
    using NovaSharp;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Debugging;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Modules;
    using NUnit.Framework;

    [TestFixture]
    public sealed class ProcessorStackTraceTests
    {
        [Test]
        public void CoroutineStackTraceIncludesCurrentFrames()
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
            Assert.That(coroutineValue.Coroutine.State, Is.EqualTo(CoroutineState.Suspended));

            WatchItem[] stack = coroutineValue.Coroutine.GetStackTrace(0);
            string[] frameNames = stack
                .Select(w => w.Name)
                .Where(n => !string.IsNullOrEmpty(n))
                .ToArray();

            Assert.Multiple(() =>
            {
                Assert.That(frameNames.Any(name => name.Contains("level3")), Is.True);
                Assert.That(frameNames.Any(name => name.Contains("level2")), Is.True);
                Assert.That(frameNames.Any(name => name.Contains("level1")), Is.True);
            });
        }

        [Test]
        public void InterpreterExceptionIncludesCallStackFrames()
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

            InterpreterException captured = null;

            try
            {
                script.Call(script.Globals.Get("level1"));
            }
            catch (InterpreterException ex)
            {
                captured = ex;
            }

            Assert.That(captured, Is.Not.Null, "Expected InterpreterException");

            Assert.Multiple(() =>
            {
                Assert.That(captured.CallStack, Is.Not.Null);
                Assert.That(captured.CallStack.Count, Is.GreaterThanOrEqualTo(3));
                bool hasLevel3 = captured
                    .CallStack.Select(w => w.Name)
                    .Where(n => !string.IsNullOrEmpty(n))
                    .Any(n => n.Contains("level3"));
                Assert.That(hasLevel3, Is.True);
            });
        }
    }
}
