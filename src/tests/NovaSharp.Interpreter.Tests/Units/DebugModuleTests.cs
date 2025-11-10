namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Interop;
    using NovaSharp.Interpreter.Modules;
    using NUnit.Framework;

    [TestFixture]
    public sealed class DebugModuleTests
    {
        [OneTimeSetUp]
        public void RegisterTypes()
        {
            UserData.RegisterType<SampleUserData>();
        }

        [Test]
        public void GetUserValueReturnsStoredValue()
        {
            Script script = CreateScript();
            DynValue userdata = UserData.Create(new SampleUserData());
            userdata.UserData.UserValue = DynValue.NewString("stored");
            script.Globals["ud"] = userdata;

            DynValue result = script.DoString("return debug.getuservalue(ud)");

            Assert.That(result.String, Is.EqualTo("stored"));
        }

        [Test]
        public void SetUserValueUpdatesDescriptor()
        {
            Script script = CreateScript();
            DynValue userdata = UserData.Create(new SampleUserData());
            script.Globals["ud"] = userdata;

            script.DoString("debug.setuservalue(ud, { foo = 42 })");
            DynValue userValue = script.DoString(
                "local result = debug.getuservalue(ud); return result.foo"
            );

            Assert.That(userValue.Number, Is.EqualTo(42));
        }

        [Test]
        public void GetRegistryExposesGlobals()
        {
            Script script = CreateScript();
            DynValue registryType = script.DoString(
                "local reg = debug.getregistry(); return type(reg._G)"
            );

            Assert.That(registryType.String, Is.EqualTo("table"));
        }

        [Test]
        public void GetMetatableReturnsTableMetatable()
        {
            Script script = CreateScript();
            DynValue result = script.DoString(
                "local mt = { flag = true }; local t = setmetatable({}, mt); return debug.getmetatable(t).flag"
            );

            Assert.That(result.Boolean, Is.True);
        }

        [Test]
        public void GetMetatableForTypeReturnsTypeMetatable()
        {
            Script script = CreateScript();
            DynValue result = script.DoString(
                "debug.setmetatable(true, { value = 7 }); return debug.getmetatable(true).value"
            );

            Assert.That(result.Number, Is.EqualTo(7));
        }

        [Test]
        public void SetMetatableThrowsOnUnsupportedType()
        {
            Script script = CreateScript();

            Assert.That(
                () => script.DoString("debug.setmetatable(print, {})"),
                Throws.TypeOf<ScriptRuntimeException>()
            );
        }

        [Test]
        public void GetUpvalueAndSetupvalueRoundtrip()
        {
            Script script = CreateScript();
            script.DoString(
                @"
                local function makeClosure()
                    local secret = 21
                    return function() return secret end
                end
                fn = makeClosure()
                "
            );

            DynValue tuple = script.DoString(
                "local name, val = debug.getupvalue(fn, 1); return name, val"
            );
            Assert.Multiple(() =>
            {
                Assert.That(tuple.Tuple[0].String, Is.EqualTo("secret"));
                Assert.That(tuple.Tuple[1].Number, Is.EqualTo(21));
            });

            DynValue setupReturn = script.DoString("return debug.setupvalue(fn, 1, 64)");
            Assert.That(setupReturn.String, Is.EqualTo("secret"));

            DynValue callResult = script.DoString("return fn()");
            Assert.That(callResult.Number, Is.EqualTo(64));
        }

        [Test]
        public void UpvalueIdAndJoinShareClosures()
        {
            Script script = CreateScript();
            script.DoString(
                @"
                local function factory(start)
                    local value = start
                    return function()
                        value = value + 1
                        return value
                    end
                end
                fnA = factory(0)
                fnB = factory(100)
                "
            );

            double idA = script.DoString("return debug.upvalueid(fnA, 1)").Number;
            double idB = script.DoString("return debug.upvalueid(fnB, 1)").Number;
            Assert.That(idA, Is.Not.EqualTo(idB));

            script.DoString("debug.upvaluejoin(fnA, 1, fnB, 1)");
            double idBAfterJoin = script.DoString("return debug.upvalueid(fnB, 1)").Number;
            Assert.That(idBAfterJoin, Is.EqualTo(idA));

            DynValue result = script.DoString("fnA(); return fnB()");
            Assert.That(result.Number, Is.EqualTo(2)); // fnB now shares fnA upvalue (0 -> 1 by fnA, then 2)
        }

        [Test]
        public void TracebackFormatsStack()
        {
            Script script = CreateScript();
            DynValue trace = script.DoString("return debug.traceback('custom error', 0)");

            Assert.Multiple(() =>
            {
                Assert.That(trace.String, Does.Contain("custom error"));
                Assert.That(trace.String, Does.Contain("stack traceback"));
            });
        }

        private static Script CreateScript()
        {
            Script script = new Script(CoreModules.PresetComplete);
            script.Options.DebugPrint = _ => { };
            return script;
        }

        private sealed class SampleUserData { }
    }
}
