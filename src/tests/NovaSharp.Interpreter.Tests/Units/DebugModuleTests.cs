namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using System.Collections.Generic;
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
            DynValue registry = script.DoString("return debug.getregistry()");

            Assert.Multiple(() =>
            {
                Assert.That(registry.Type == DataType.Table || registry.IsNil(), Is.True);
                if (registry.Type == DataType.Table)
                {
                    Assert.That(registry.Table, Is.Not.Null);
                }
            });
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
        public void GetMetatableReturnsNilForUnsupportedTypes()
        {
            Script script = CreateScript();
            DynValue result = script.DoString(
                "local co = coroutine.create(function() end); return debug.getmetatable(co)"
            );

            Assert.That(result.IsNil(), Is.True);
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
                @"
                local idx = 1
                while true do
                    local name, value = debug.getupvalue(fn, idx)
                    if name == nil then return -1, nil end
                    if name == 'secret' then return idx, value end
                    idx = idx + 1
                end
                "
            );

            int secretIndex = (int)tuple.Tuple[0].Number;
            Assert.Multiple(() =>
            {
                Assert.That(secretIndex, Is.GreaterThan(0));
                Assert.That(tuple.Tuple[1].Number, Is.EqualTo(21));
            });

            DynValue setupReturn = script.DoString(
                $"return debug.setupvalue(fn, {secretIndex}, 64)"
            );
            Assert.That(setupReturn.String, Is.EqualTo("secret"));

            DynValue callResult = script.DoString("return fn()");
            Assert.That(callResult.Number, Is.EqualTo(64));
        }

        [Test]
        public void GetUpvalueReturnsNilForClrFunctions()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return debug.getupvalue(print, 1)");

            Assert.That(result.IsNil(), Is.True);
        }

        [Test]
        public void SetupvalueReturnsNilForClrFunctions()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return debug.setupvalue(print, 1, 10)");

            Assert.That(result.IsNil(), Is.True);
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

            DynValue result = script.DoString(
                @"
                local function find_index(fn)
                    local idx = 1
                    while true do
                        local name = select(1, debug.getupvalue(fn, idx))
                        if name == nil then return -1 end
                        if name == 'value' then return idx end
                        idx = idx + 1
                    end
                end

                local idxA = find_index(fnA)
                local idxB = find_index(fnB)
                if idxA < 0 or idxB < 0 then return false end

                local idA = debug.upvalueid(fnA, idxA)
                local idB = debug.upvalueid(fnB, idxB)
                if idA == idB then return false end

                debug.upvaluejoin(fnA, idxA, fnB, idxB)
                local idBAfter = debug.upvalueid(fnB, idxB)
                fnA()
                local shared = (idBAfter == idA) and (fnB() == 2)
                return shared
                "
            );

            Assert.That(result.Boolean, Is.True);
        }

        [Test]
        public void UpvalueJoinThrowsOnInvalidIndex()
        {
            Script script = CreateScript();

            Assert.That(
                () =>
                    script.DoString(
                        @"
                        local function factory()
                            local value = 0
                            return function() return value end
                        end
                        local fn = factory()
                        debug.upvaluejoin(fn, 5, fn, 1)
                        "
                    ),
                Throws
                    .TypeOf<ScriptRuntimeException>()
                    .With.Message.Contains("invalid upvalue index")
            );
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

        [Test]
        public void TracebackReturnsOriginalValueForNonStringMessages()
        {
            Script script = CreateScript();
            DynValue result = script.DoString(
                "local t = { key = 'value' }; return debug.traceback(t) == t"
            );

            Assert.That(result.Boolean, Is.True);
        }

        [Test]
        public void TracebackAcceptsThreadArgument()
        {
            Script script = CreateScript();
            DynValue trace = script.DoString(
                @"
                local co = coroutine.create(function()
                    return debug.traceback(coroutine.running(), 'from coroutine', 0)
                end)
                local ok, result = coroutine.resume(co)
                assert(ok)
                return result
                "
            );

            Assert.Multiple(() =>
            {
                Assert.That(trace.String, Does.Contain("from coroutine"));
                Assert.That(trace.String, Does.Contain("stack traceback"));
            });
        }

        [Test]
        public void DebugLoopProcessesQueuedCommands()
        {
            Script script = CreateScript();
            Queue<string> commands = new(new[] { "print('hello')", "return" });
            List<string> output = new();

            script.Options.DebugInput = _ => commands.Count > 0 ? commands.Dequeue() : null;
            script.Options.DebugPrint = s => output.Add(s);

            DynValue result = script.DoString("return debug.debug()");

            Assert.Multiple(() =>
            {
                Assert.That(result.IsNil(), Is.True);
                Assert.That(commands.Count, Is.EqualTo(0));
                Assert.That(output, Has.Some.Contains("hello"));
            });
        }

        [Test]
        public void DebugLoopReportsErrorsAndRespectsReturnCommand()
        {
            Script script = CreateScript();
            Queue<string> commands = new(new[] { "error('boom')", "return" });
            List<string> output = new();

            script.Options.DebugInput = _ => commands.Count > 0 ? commands.Dequeue() : null;
            script.Options.DebugPrint = s => output.Add(s);

            script.DoString("debug.debug()");

            Assert.Multiple(() =>
            {
                Assert.That(commands.Count, Is.EqualTo(0));
                Assert.That(output, Has.Some.Contains("boom"));
            });
        }

        [Test]
        public void DebugLoopPrintsReturnedValues()
        {
            Script script = CreateScript();
            Queue<string> commands = new(new[] { "return 42", "return" });
            List<string> output = new();

            script.Options.DebugInput = _ => commands.Count > 0 ? commands.Dequeue() : null;
            script.Options.DebugPrint = s => output.Add(s);

            script.DoString("debug.debug()");

            Assert.Multiple(() =>
            {
                Assert.That(commands.Count, Is.EqualTo(0));
                Assert.That(output, Has.Some.Contains("42"));
            });
        }

        [Test]
        public void DebugLoopHandlesGeneralExceptions()
        {
            Script script = CreateScript();
            Queue<string> commands = new(new[] { "callClr()", "return" });
            List<string> output = new();

            script.Globals["callClr"] = DynValue.NewCallback(
                (context, args) => throw new InvalidOperationException("unexpected boom"),
                "callClr"
            );

            script.Options.DebugInput = _ => commands.Count > 0 ? commands.Dequeue() : null;
            script.Options.DebugPrint = s => output.Add(s);

            script.DoString("debug.debug()");

            Assert.Multiple(() =>
            {
                Assert.That(commands.Count, Is.EqualTo(0));
                Assert.That(output, Has.Some.Contains("unexpected boom"));
            });
        }

        [Test]
        public void DebugLoopStopsWhenInputReturnsNullImmediately()
        {
            Script script = CreateScript();
            Queue<string> commands = new(new string[] { null });
            List<string> output = new();

            script.Options.DebugInput = _ => commands.Count > 0 ? commands.Dequeue() : null;
            script.Options.DebugPrint = s => output.Add(s);

            DynValue result = script.DoString("return debug.debug()");

            Assert.Multiple(() =>
            {
                Assert.That(result.IsNil(), Is.True);
                Assert.That(output, Is.Empty);
            });
        }

        [Test]
        public void DebugLoopTreatsWhitespaceInputAsNoOp()
        {
            Script script = CreateScript();
            Queue<string> commands = new(new[] { "   ", "\treturn" });
            List<string> output = new();

            script.Options.DebugInput = _ => commands.Count > 0 ? commands.Dequeue() : null;
            script.Options.DebugPrint = s => output.Add(s);

            DynValue result = script.DoString("return debug.debug()");

            Assert.Multiple(() =>
            {
                Assert.That(result.IsNil(), Is.True);
                Assert.That(commands.Count, Is.EqualTo(0));
                Assert.That(output, Is.Empty);
            });
        }

        [Test]
        public void DebugLoopHonoursReturnCommandWithDifferentCase()
        {
            Script script = CreateScript();
            Queue<string> commands = new(new[] { "RETURN" });
            List<string> output = new();

            script.Options.DebugInput = _ => commands.Count > 0 ? commands.Dequeue() : null;
            script.Options.DebugPrint = s => output.Add(s);

            DynValue result = script.DoString("return debug.debug()");

            Assert.Multiple(() =>
            {
                Assert.That(result.IsNil(), Is.True);
                Assert.That(commands.Count, Is.EqualTo(0));
                Assert.That(output, Is.Empty);
            });
        }

        [Test]
        public void DebugLoopThrowsWhenInputProviderMissing()
        {
            Script script = new Script(CoreModules.PresetComplete);
            script.Options.DebugPrint = _ => { };
            script.Options.DebugInput = null;

            Assert.That(
                () => script.DoString("debug.debug()"),
                Throws
                    .TypeOf<ScriptRuntimeException>()
                    .With.Property(nameof(ScriptRuntimeException.DecoratedMessage))
                    .Contains("debug.debug not supported")
            );
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
