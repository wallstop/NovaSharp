namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.EndToEnd
{
    using System;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Modules;
    using WallstopStudios.NovaSharp.Tests.TestInfrastructure.TUnit;

    public sealed class CoroutineTUnitTests
    {
        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task CoroutineBasicInterleavesLuaCoroutines(LuaCompatibilityVersion version)
        {
            string code =
                @"
                s = ''

                function foo()
                    for i = 1, 4 do
                        s = s .. i;
                        coroutine.yield();
                    end
                end

                function bar()
                    for i = 5, 9 do
                        s = s .. i;
                        coroutine.yield();
                    end
                end

                cf = coroutine.create(foo);
                cb = coroutine.create(bar);

                for i = 1, 4 do
                    coroutine.resume(cf);
                    s = s .. '-';
                    coroutine.resume(cb);
                    s = s .. ';';
                end

                return s;
                ";

            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString(code);
            await EndToEndDynValueAssert
                .ExpectAsync(result, "1-5;2-6;3-7;4-8;")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task CoroutineWrapPreservesSchedulingOrder(LuaCompatibilityVersion version)
        {
            string code =
                @"
                s = ''

                function foo()
                    for i = 1, 4 do
                        s = s .. i;
                        coroutine.yield();
                    end
                end

                function bar()
                    for i = 5, 9 do
                        s = s .. i;
                        coroutine.yield();
                    end
                end

                cf = coroutine.wrap(foo);
                cb = coroutine.wrap(bar);

                for i = 1, 4 do
                    cf();
                    s = s .. '-';
                    cb();
                    s = s .. ';';
                end

                return s;
                ";

            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString(code);
            await EndToEndDynValueAssert
                .ExpectAsync(result, "1-5;2-6;3-7;4-8;")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task CoroutineClrBoundaryDetection(LuaCompatibilityVersion version)
        {
            string code =
                @"
                function a()
                    callback(b)
                end

                function b()
                    coroutine.yield();
                end

                c = coroutine.create(a);
                return coroutine.resume(c);
                ";

            Script host = new Script(version, CoreModulePresets.Complete);
            host.Globals["callback"] = DynValue.NewCallback((ctx, args) => args[0].Function.Call());

            DynValue resumeResult = host.DoString(code);
            await Assert.That(resumeResult.Type).IsEqualTo(DataType.Tuple).ConfigureAwait(false);
            await Assert.That(resumeResult.Tuple.Length).IsEqualTo(2).ConfigureAwait(false);
            await Assert.That(resumeResult.Tuple[0].Boolean).IsFalse().ConfigureAwait(false);
            await Assert
                .That(resumeResult.Tuple[1].Type)
                .IsEqualTo(DataType.String)
                .ConfigureAwait(false);
            await Assert
                .That(resumeResult.Tuple[1].String)
                .EndsWith("attempt to yield across a CLR-call boundary", StringComparison.Ordinal)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Tests various coroutine error handling scenarios.
        /// This test uses setmetatable and __tostring metamethods which interact with
        /// the CLR-call boundary detection. The behavior requires Lua 5.4+ print/__tostring
        /// semantics where the __tostring metamethod is called in a way that allows CLR
        /// boundary detection to work correctly.
        /// </summary>
        [global::TUnit.Core.Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua54)]
        public async Task CoroutineVariousErrorHandlingMatchesNunitSuite(
            LuaCompatibilityVersion version
        )
        {
            string code =
                @"
                function checkresume(step, ex, ey)
                    local x, y = coroutine.resume(c)
                    assert(x == ex, 'Step ' .. step .. ': ' .. tostring(ex) .. ' was expected, got ' .. tostring(x));
                    assert(y:endsWith(ey), 'Step ' .. step .. ': ' .. tostring(ey) .. ' was expected, got ' .. tostring(y));
                end

                t = { }
                m = { __tostring = function() print('2'); coroutine.yield(); print('3'); end }
                setmetatable(t, m);

                function a()
                    checkresume(1, false, 'cannot resume non-suspended coroutine');
                    coroutine.yield('ok');
                    print(t);
                    coroutine.yield('ok');
                end

                c = coroutine.create(a);

                checkresume(2, true, 'ok');
                checkresume(3, false, 'attempt to yield across a CLR-call boundary');
                checkresume(4, false, 'cannot resume dead coroutine');
                checkresume(5, false, 'cannot resume dead coroutine');
                checkresume(6, false, 'cannot resume dead coroutine');
                ";

            string lastPrinted = "";
            Script host = new Script(version, CoreModulePresets.Complete);
            host.Options.DebugPrint = s => lastPrinted = s;
            host.DoString(code);
            await Assert.That(lastPrinted).IsEqualTo("2").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task CoroutineCanBeResumedDirectlyFromClr(LuaCompatibilityVersion version)
        {
            string code =
                @"
                return function()
                    local x = 0
                    while true do
                        x = x + 1
                        coroutine.yield(x)
                        if (x > 5) then
                            return 7
                        end
                    end
                end
                ";

            Script host = new Script(version, CoreModulePresets.Complete);
            DynValue factory = host.DoString(code);
            DynValue coroutine = host.CreateCoroutine(factory);

            string result = "";
            while (coroutine.Coroutine.State != CoroutineState.Dead)
            {
                DynValue yielded = coroutine.Coroutine.Resume();
                result += yielded.ToString();
            }

            await Assert.That(result).IsEqualTo("1234567").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task CoroutineSupportsTypedEnumerable(LuaCompatibilityVersion version)
        {
            string code =
                @"
                return function()
                    local x = 0
                    while true do
                        x = x + 1
                        coroutine.yield(x)
                        if (x > 5) then
                            return 7
                        end
                    end
                end
                ";

            Script host = new Script(version, CoreModulePresets.Complete);
            DynValue factory = host.DoString(code);
            DynValue coroutine = host.CreateCoroutine(factory);

            string result = "";
            foreach (DynValue yielded in coroutine.Coroutine.AsTypedEnumerable())
            {
                result += yielded.ToString();
            }

            await Assert.That(result).IsEqualTo("1234567").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task CoroutineAutoYieldResumesUntilCompletion(LuaCompatibilityVersion version)
        {
            string code =
                @"
                function fib(n)
                    if (n == 0 or n == 1) then
                        return 1;
                    else
                        return fib(n - 1) + fib(n - 2);
                    end
                end
                ";

            Script host = new Script(version, default(CoreModules));
            host.DoString(code);
            DynValue fib = host.Globals.Get("fib");

            DynValue coroutine = host.CreateCoroutine(fib);
            coroutine.Coroutine.AutoYieldCounter = 10;

            int cycles = 0;
            DynValue result = coroutine.Coroutine.Resume(8);
            while (result.Type == DataType.YieldRequest)
            {
                cycles++;
                result = coroutine.Coroutine.Resume();
            }

            await Assert.That(result.Type).IsEqualTo(DataType.Number).ConfigureAwait(false);
            await Assert.That(result.Number).IsEqualTo(34).ConfigureAwait(false);
            await Assert.That(cycles > 10).IsTrue().ConfigureAwait(false);
        }
    }
}
