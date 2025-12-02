namespace NovaSharp.Interpreter.Tests.TUnit.Modules
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.CoreLib;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Interop;
    using NovaSharp.Interpreter.Modules;
    using NovaSharp.Interpreter.Tests;
    using NovaSharp.Tests.TestInfrastructure.Scopes;

    [UserDataIsolation]
    public sealed class DebugModuleTUnitTests
    {
        private static readonly string[] PrintThenReturn = { "print('hello')", "return" };
        private static readonly string[] ErrorThenReturn = { "error('boom')", "return" };
        private static readonly string[] ReturnValueSequence = { "return 42", "return" };
        private static readonly string[] CallClrSequence = { "callClr()", "return" };
        private static readonly string[] WhitespaceReturnSequence = { "   ", "\treturn" };
        private static readonly string[] SingleReturnSequence = { "RETURN" };

        [global::TUnit.Core.Test]
        public async Task GetUserValueReturnsStoredValue()
        {
            using UserDataRegistrationScope registrationScope = RegisterSampleUserData();
            Script script = CreateScript();
            DynValue userdata = UserData.Create(new SampleUserData());
            userdata.UserData.UserValue = DynValue.NewString("stored");
            script.Globals["ud"] = userdata;

            DynValue result = script.DoString("return debug.getuservalue(ud)");

            await Assert.That(result.String).IsEqualTo("stored").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task GetUserValueDefaultsToNilWhenUnset()
        {
            using UserDataRegistrationScope registrationScope = RegisterSampleUserData();
            Script script = CreateScript();
            DynValue userdata = UserData.Create(new SampleUserData());
            script.Globals["ud"] = userdata;

            DynValue result = script.DoString("return debug.getuservalue(ud)");

            await Assert.That(result.IsNil()).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task GetUserValueReturnsNilWhenNotUserData()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return debug.getuservalue('not-userdata')");

            await Assert.That(result.IsNil()).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task SetUserValueUpdatesDescriptor()
        {
            using UserDataRegistrationScope registrationScope = RegisterSampleUserData();
            Script script = CreateScript();
            DynValue userdata = UserData.Create(new SampleUserData());
            script.Globals["ud"] = userdata;

            script.DoString("debug.setuservalue(ud, { foo = 42 })");
            DynValue userValue = script.DoString(
                "local result = debug.getuservalue(ud); return result.foo"
            );

            await Assert.That(userValue.Number).IsEqualTo(42).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task SetUserValueAllowsClearingWithNil()
        {
            using UserDataRegistrationScope registrationScope = RegisterSampleUserData();
            Script script = CreateScript();
            DynValue userdata = UserData.Create(new SampleUserData());
            script.Globals["ud"] = userdata;

            script.DoString("debug.setuservalue(ud, { foo = 'value' })");
            script.DoString("debug.setuservalue(ud, nil)");

            DynValue cleared = script.DoString("return debug.getuservalue(ud)");
            await Assert.That(cleared.IsNil()).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task GetRegistryExposesGlobals()
        {
            Script script = CreateScript();
            DynValue registry = script.DoString("return debug.getregistry()");

            bool isTableOrNil = registry.Type == DataType.Table || registry.IsNil();
            await Assert.That(isTableOrNil).IsTrue().ConfigureAwait(false);

            if (registry.Type == DataType.Table)
            {
                await Assert.That(registry.Table).IsNotNull().ConfigureAwait(false);
            }
        }

        [global::TUnit.Core.Test]
        public async Task GetMetatableReturnsTableMetatable()
        {
            Script script = CreateScript();
            DynValue result = script.DoString(
                "local mt = { flag = true }; local t = setmetatable({}, mt); return debug.getmetatable(t).flag"
            );

            await Assert.That(result.Boolean).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task GetMetatableReturnsNilForUnsupportedTypes()
        {
            Script script = CreateScript();
            DynValue result = script.DoString(
                "local co = coroutine.create(function() end); return debug.getmetatable(co)"
            );

            await Assert.That(result.IsNil()).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task SetMetatableAllowsClearingTableMetatable()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return debug.setmetatable({}, nil) ~= nil");

            await Assert.That(result.Boolean).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task GetMetatableForTypeReturnsTypeMetatable()
        {
            Script script = CreateScript();
            DynValue result = script.DoString(
                "debug.setmetatable(true, { value = 7 }); return debug.getmetatable(true).value"
            );

            await Assert.That(result.Number).IsEqualTo(7).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task SetMetatableThrowsOnUnsupportedType()
        {
            Script script = CreateScript();

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                script.DoString("debug.setmetatable(print, {})")
            );

            await Assert.That(exception).IsNotNull().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task GetUpValueAndSetupvalueRoundtrip()
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
            await Assert.That(secretIndex).IsGreaterThan(0).ConfigureAwait(false);
            await Assert.That(tuple.Tuple[1].Number).IsEqualTo(21).ConfigureAwait(false);

            DynValue setupReturn = script.DoString(
                $"return debug.setupvalue(fn, {secretIndex}, 64)"
            );
            await Assert.That(setupReturn.String).IsEqualTo("secret").ConfigureAwait(false);

            DynValue callResult = script.DoString("return fn()");
            await Assert.That(callResult.Number).IsEqualTo(64).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task GetUpValueReturnsNilForClrFunctions()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return debug.getupvalue(print, 1)");

            await Assert.That(result.IsNil()).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task GetUpValueReturnsNilWhenIndexOutOfRange()
        {
            Script script = CreateScript();
            script.DoString(
                @"
                local function factory()
                    local secret = 10
                    return function(a) return secret + a end
                end
                fn = factory()
                "
            );

            DynValue result = script.DoString("return debug.getupvalue(fn, 99)");

            await Assert.That(result.IsNil()).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task GetUpValueReturnsNilWhenZeroIndexRequested()
        {
            Script script = CreateScript();
            script.DoString(
                @"
                local function factory()
                    local secret = 5
                    return function() return secret end
                end
                fn = factory()
                "
            );

            DynValue result = script.DoString("return debug.getupvalue(fn, 0)");

            await Assert.That(result.IsNil()).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task SetupvalueReturnsNilForClrFunctions()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return debug.setupvalue(print, 1, 10)");

            await Assert.That(result.IsNil()).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task SetupvalueReturnsNilWhenIndexOutOfRange()
        {
            Script script = CreateScript();
            script.DoString(
                @"
                local function factory()
                    local secret = 10
                    return function(a) return secret + a end
                end
                fn = factory()
                "
            );

            DynValue result = script.DoString("return debug.setupvalue(fn, 99, 20)");

            await Assert.That(result.IsNil()).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task UpValueIdAndJoinShareClosures()
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

            await Assert.That(result.Boolean).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task UpValueIdReturnsNilForClrFunctions()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return debug.upvalueid(print, 1)");

            await Assert.That(result.IsNil()).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task UpValueIdReturnsNilWhenIndexOutOfRange()
        {
            Script script = CreateScript();
            script.DoString(
                @"
                local function factory()
                    local value = 0
                    return function() return value end
                end
                fn = factory()
                "
            );

            DynValue result = script.DoString("return debug.upvalueid(fn, 99)");

            await Assert.That(result.IsNil()).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task UpValueJoinThrowsOnInvalidIndex()
        {
            Script script = CreateScript();

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                script.DoString(
                    @"
                    local function factory()
                        local value = 0
                        return function() return value end
                    end
                    local fn = factory()
                    debug.upvaluejoin(fn, 5, fn, 1)
                    "
                )
            );

            await Assert
                .That(exception.Message)
                .Contains("invalid upvalue index")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task UpValueJoinThrowsWhenSecondIndexInvalid()
        {
            Script script = CreateScript();

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                script.DoString(
                    @"
                    local function factory()
                        local value = 0
                        return function() return value end
                    end
                    local fn = factory()
                    debug.upvaluejoin(fn, 1, fn, 5)
                    "
                )
            );

            await Assert
                .That(exception.Message)
                .Contains("invalid upvalue index")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task TracebackFormatsStack()
        {
            Script script = CreateScript();
            DynValue trace = script.DoString("return debug.traceback('custom error', 0)");

            await Assert.That(trace.String).Contains("custom error").ConfigureAwait(false);
            await Assert.That(trace.String).Contains("stack traceback").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task TracebackReturnsOriginalValueForNonStringMessages()
        {
            Script script = CreateScript();
            DynValue result = script.DoString(
                "local t = { key = 'value' }; return debug.traceback(t) == t"
            );

            await Assert.That(result.Boolean).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task TracebackOmitsMessageWhenNil()
        {
            Script script = CreateScript();
            DynValue trace = script.DoString("return debug.traceback(nil, 0)");

            bool startsWithTraceback = trace.String.StartsWith(
                "stack traceback:",
                StringComparison.Ordinal
            );
            bool containsNil = trace.String.Contains("nil", StringComparison.Ordinal);

            await Assert.That(startsWithTraceback).IsTrue().ConfigureAwait(false);
            await Assert.That(containsNil).IsFalse().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task TracebackIncludesFunctionNameWhenAvailable()
        {
            Script script = CreateScript();
            DynValue trace = script.DoString(
                @"
                local function inner()
                    return debug.traceback('from inner', 0)
                end
                return inner()
                "
            );

            await Assert.That(trace.String).Contains("function 'inner").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task TracebackAcceptsThreadArgument()
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

            await Assert.That(trace.String).Contains("from coroutine").ConfigureAwait(false);
            await Assert.That(trace.String).Contains("stack traceback").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task DebugLoopProcessesQueuedCommands()
        {
            Script script = CreateScript();
            Queue<string> commands = new Queue<string>(PrintThenReturn);
            List<string> output = new();

            script.Options.DebugInput = _ => commands.Count > 0 ? commands.Dequeue() : null;
            script.Options.DebugPrint = s => output.Add(s);

            DynValue result = script.DoString("return debug.debug()");

            await Assert.That(result.IsNil()).IsTrue().ConfigureAwait(false);
            await Assert.That(commands.Count).IsEqualTo(0).ConfigureAwait(false);
            bool printedHello = output.Exists(value =>
                value?.Contains("hello", StringComparison.Ordinal) == true
            );
            await Assert.That(printedHello).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task DebugLoopReportsErrorsAndRespectsReturnCommand()
        {
            Script script = CreateScript();
            Queue<string> commands = new Queue<string>(ErrorThenReturn);
            List<string> output = new();

            script.Options.DebugInput = _ => commands.Count > 0 ? commands.Dequeue() : null;
            script.Options.DebugPrint = s => output.Add(s);

            script.DoString("debug.debug()");

            await Assert.That(commands.Count).IsEqualTo(0).ConfigureAwait(false);
            bool printedBoom = output.Exists(value =>
                value?.Contains("boom", StringComparison.Ordinal) == true
            );
            await Assert.That(printedBoom).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task DebugLoopPrintsReturnedValues()
        {
            Script script = CreateScript();
            Queue<string> commands = new Queue<string>(ReturnValueSequence);
            List<string> output = new();

            script.Options.DebugInput = _ => commands.Count > 0 ? commands.Dequeue() : null;
            script.Options.DebugPrint = s => output.Add(s);

            script.DoString("debug.debug()");

            await Assert.That(commands.Count).IsEqualTo(0).ConfigureAwait(false);
            bool printedValue = output.Exists(value =>
                value?.Contains("42", StringComparison.Ordinal) == true
            );
            await Assert.That(printedValue).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task DebugLoopHandlesGeneralExceptions()
        {
            Script script = CreateScript();
            Queue<string> commands = new Queue<string>(CallClrSequence);
            List<string> output = new();

            script.Globals["callClr"] = DynValue.NewCallback(
                (_, _) => throw new InvalidOperationException("unexpected boom"),
                "callClr"
            );

            script.Options.DebugInput = _ => commands.Count > 0 ? commands.Dequeue() : null;
            script.Options.DebugPrint = s => output.Add(s);

            script.DoString("debug.debug()");

            await Assert.That(commands.Count).IsEqualTo(0).ConfigureAwait(false);
            bool printedUnexpectedBoom = output.Exists(value =>
                value?.Contains("unexpected boom", StringComparison.Ordinal) == true
            );
            await Assert.That(printedUnexpectedBoom).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task DebugLoopStopsWhenInputReturnsNullImmediately()
        {
            Script script = CreateScript();
            Queue<string> commands = new Queue<string>(new string[] { null });
            List<string> output = new();

            script.Options.DebugInput = _ => commands.Count > 0 ? commands.Dequeue() : null;
            script.Options.DebugPrint = s => output.Add(s);

            DynValue result = script.DoString("return debug.debug()");

            await Assert.That(result.IsNil()).IsTrue().ConfigureAwait(false);
            await Assert.That(output.Count).IsEqualTo(0).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task DebugLoopTreatsWhitespaceInputAsNoOp()
        {
            Script script = CreateScript();
            Queue<string> commands = new Queue<string>(WhitespaceReturnSequence);
            List<string> output = new();

            script.Options.DebugInput = _ => commands.Count > 0 ? commands.Dequeue() : null;
            script.Options.DebugPrint = s => output.Add(s);

            DynValue result = script.DoString("return debug.debug()");

            await Assert.That(result.IsNil()).IsTrue().ConfigureAwait(false);
            await Assert.That(commands.Count).IsEqualTo(0).ConfigureAwait(false);
            await Assert.That(output.Count).IsEqualTo(0).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task DebugLoopHonoursReturnCommandWithDifferentCase()
        {
            Script script = CreateScript();
            Queue<string> commands = new Queue<string>(SingleReturnSequence);
            List<string> output = new();

            script.Options.DebugInput = _ => commands.Count > 0 ? commands.Dequeue() : null;
            script.Options.DebugPrint = s => output.Add(s);

            DynValue result = script.DoString("return debug.debug()");

            await Assert.That(result.IsNil()).IsTrue().ConfigureAwait(false);
            await Assert.That(commands.Count).IsEqualTo(0).ConfigureAwait(false);
            await Assert.That(output.Count).IsEqualTo(0).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task DebugLoopThrowsWhenInputProviderMissing()
        {
            Script script = new Script(CoreModules.PresetComplete);
            script.Options.DebugPrint = _ => { };
            script.Options.DebugInput = null;

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                script.DoString("debug.debug()")
            );

            string message = exception.DecoratedMessage ?? exception.Message;
            await Assert.That(message).Contains("debug.debug not supported").ConfigureAwait(false);
        }

        private static Script CreateScript()
        {
            Script script = new Script(CoreModules.PresetComplete);
            script.Options.DebugPrint = _ => { };
            return script;
        }

        private static UserDataRegistrationScope RegisterSampleUserData()
        {
            UserDataRegistrationScope scope = UserDataRegistrationScope.Track<SampleUserData>(
                ensureUnregistered: true
            );
            scope.RegisterType<SampleUserData>();
            return scope;
        }

        private sealed class SampleUserData { }
    }
}
