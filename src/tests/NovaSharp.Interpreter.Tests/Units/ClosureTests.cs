namespace NovaSharp.Interpreter.Tests.Units
{
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NUnit.Framework;

    [TestFixture]
    public sealed class ClosureTests
    {
        [Test]
        public void GetUpvaluesTypeReturnsEnvironmentWhenOnlyEnvIsCaptured()
        {
            Script script = new();
            DynValue function = script.DoString("return function(a) return a end");

            Closure closure = function.Function;

            Assert.Multiple(() =>
            {
                Assert.That(closure.GetUpvaluesCount(), Is.EqualTo(1));
                Assert.That(closure.GetUpvalueName(0), Is.EqualTo(WellKnownSymbols.ENV));
                Assert.That(
                    closure.GetUpvaluesType(),
                    Is.EqualTo(Closure.UpvaluesType.Environment)
                );
            });
        }

        [Test]
        public void MetadataPropertiesExposeScriptAndEntryPoint()
        {
            Script script = new();
            DynValue function = script.DoString("return function() return 42 end");
            Closure closure = function.Function;

            Assert.Multiple(() =>
            {
                Assert.That(closure.OwnerScript, Is.SameAs(script));
                Assert.That(closure.EntryPointByteCodeLocation, Is.GreaterThanOrEqualTo(0));
            });
        }

        [Test]
        public void GetUpvaluesTypeDetectsEnvironmentUpvalue()
        {
            Script script = new();
            DynValue function = script.DoString("return function() return _ENV end");

            Closure closure = function.Function;

            Assert.Multiple(() =>
            {
                Assert.That(closure.GetUpvaluesCount(), Is.EqualTo(1));
                Assert.That(closure.GetUpvalueName(0), Is.EqualTo(WellKnownSymbols.ENV));
                Assert.That(
                    closure.GetUpvaluesType(),
                    Is.EqualTo(Closure.UpvaluesType.Environment)
                );
            });
        }

        [Test]
        public void GetUpvaluesExposesCapturedSymbols()
        {
            Script script = new();
            DynValue function = script.DoString(
                @"
                local x = 3
                local y = 4
                return function()
                    return x + y
                end
                "
            );

            Closure closure = function.Function;

            int upvalueCount = closure.GetUpvaluesCount();
            string[] names = new string[upvalueCount];
            for (int i = 0; i < upvalueCount; i++)
            {
                names[i] = closure.GetUpvalueName(i);
            }

            int envIndex = System.Array.IndexOf(names, WellKnownSymbols.ENV);
            int xIndex = System.Array.IndexOf(names, "x");
            int yIndex = System.Array.IndexOf(names, "y");

            Assert.Multiple(() =>
            {
                Assert.That(upvalueCount, Is.EqualTo(3));
                Assert.That(envIndex, Is.GreaterThanOrEqualTo(0));
                Assert.That(xIndex, Is.GreaterThanOrEqualTo(0));
                Assert.That(yIndex, Is.GreaterThanOrEqualTo(0));
                Assert.That(closure.GetUpvalue(xIndex).Number, Is.EqualTo(3d));
                Assert.That(closure.GetUpvalue(yIndex).Number, Is.EqualTo(4d));
                Assert.That(closure.GetUpvaluesType(), Is.EqualTo(Closure.UpvaluesType.Closure));
            });
        }

        [Test]
        public void DelegatesInvokeScriptFunction()
        {
            Script script = new();
            DynValue function = script.DoString("return function(a, b) return a + b end");
            Closure closure = function.Function;

            ScriptFunctionDelegate generic = closure.GetDelegate();
            object genericResult = generic(1, 2);

            ScriptFunctionDelegate<int> typed = closure.GetDelegate<int>();
            int typedResult = typed(5, 7);

            Assert.Multiple(() =>
            {
                Assert.That(genericResult, Is.EqualTo(3d));
                Assert.That(typedResult, Is.EqualTo(12));
            });
        }

        [Test]
        public void CallOverloadsInvokeUnderlyingFunction()
        {
            Script script = new();
            DynValue function = script.DoString(
                "return function(a, b) return (a or 0) + (b or 0) end"
            );
            Closure closure = function.Function;

            DynValue noArgs = closure.Call();
            DynValue objectArgs = closure.Call(2, 3);
            DynValue dynValues = closure.Call(DynValue.NewNumber(10), DynValue.NewNumber(5));

            Assert.Multiple(() =>
            {
                Assert.That(noArgs.Number, Is.EqualTo(0d));
                Assert.That(objectArgs.Number, Is.EqualTo(5d));
                Assert.That(dynValues.Number, Is.EqualTo(15d));
            });
        }

        [Test]
        public void UpvaluesTypeIsNoneWhenNoUpvaluesAreCaptured()
        {
            Script script = new();
            Closure closure = new(
                script,
                idx: 0,
                symbols: System.Array.Empty<SymbolRef>(),
                resolvedLocals: System.Array.Empty<DynValue>()
            );

            Assert.Multiple(() =>
            {
                Assert.That(closure.GetUpvaluesCount(), Is.EqualTo(0));
                Assert.That(closure.GetUpvaluesType(), Is.EqualTo(default(Closure.UpvaluesType)));
            });
        }
    }
}
