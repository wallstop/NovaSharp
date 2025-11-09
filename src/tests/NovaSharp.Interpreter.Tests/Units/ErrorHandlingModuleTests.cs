namespace NovaSharp.Interpreter.Tests.Units
{
    using NovaSharp;
    using NovaSharp.Interpreter;
    using NUnit.Framework;

    [TestFixture]
    public sealed class ErrorHandlingModuleTests
    {
        [Test]
        public void PcallReportsForcedYieldForClrFunctions()
        {
            Script script = new(CoreModules.PresetComplete);

            CallbackFunction forcedYield = new(
                (context, args) => DynValue.NewYieldReq(System.Array.Empty<DynValue>())
            );
            script.Globals["forcedYieldFn"] = DynValue.NewCallback(forcedYield);

            DynValue pcallFunc = script.Globals.Get("pcall");
            DynValue result = script.Call(pcallFunc, script.Globals.Get("forcedYieldFn"));

            Assert.That(result.Type, Is.EqualTo(DataType.Tuple));
            Assert.That(result.Tuple.Length, Is.GreaterThanOrEqualTo(2));
            Assert.That(result.Tuple[0].Type, Is.EqualTo(DataType.Boolean));
            Assert.That(result.Tuple[0].Boolean, Is.False);
            Assert.That(result.Tuple[1].Type, Is.EqualTo(DataType.String));
            Assert.That(result.Tuple[1].String, Does.Contain("cannot be called directly by pcall"));
        }

        [Test]
        public void XpcallAppliesMessageDecoration()
        {
            Script script = new(CoreModules.PresetComplete);
            script.DoString(
                @"
                function explode()
                    error('boom', 0)
                end

                function decorator(message)
                    return 'decorated:' .. message
                end

                function wrapped()
                    return xpcall(explode, decorator)
                end
            "
            );

            DynValue result = script.Call(script.Globals.Get("wrapped"));
            Assert.That(result.Type, Is.EqualTo(DataType.Tuple));
            Assert.That(result.Tuple.Length, Is.GreaterThanOrEqualTo(2));
            Assert.That(result.Tuple[0].Boolean, Is.False);
            Assert.That(result.Tuple[1].String, Is.EqualTo("decorated:boom"));
        }
    }
}
