#pragma warning disable CA2007
namespace NovaSharp.Interpreter.Tests.TUnit.EndToEnd
{
    using System;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Modules;

    public sealed class CoroutineTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task CoroutineBasicInterleavesLuaCoroutines()
        {
            string script =
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

            DynValue result = Script.RunString(script);
            await EndToEndDynValueAssert.ExpectAsync(result, "1-5;2-6;3-7;4-8;");
        }

        [global::TUnit.Core.Test]
        public async Task CoroutineWrapPreservesSchedulingOrder()
        {
            string script =
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

            DynValue result = Script.RunString(script);
            await EndToEndDynValueAssert.ExpectAsync(result, "1-5;2-6;3-7;4-8;");
        }

        [global::TUnit.Core.Test]
        public async Task CoroutineClrBoundaryDetection()
        {
            string script =
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

            Script host = new()
            {
                Globals =
                {
                    ["callback"] = DynValue.NewCallback((ctx, args) => args[0].Function.Call()),
                },
            };

            DynValue resumeResult = host.DoString(script);
            await Assert.That(resumeResult.Type).IsEqualTo(DataType.Tuple);
            await Assert.That(resumeResult.Tuple.Length).IsEqualTo(2);
            await Assert.That(resumeResult.Tuple[0].Boolean).IsFalse();
            await Assert.That(resumeResult.Tuple[1].Type).IsEqualTo(DataType.String);
            await Assert
                .That(resumeResult.Tuple[1].String)
                .EndsWith("attempt to yield across a CLR-call boundary", StringComparison.Ordinal);
        }

        [global::TUnit.Core.Test]
        public async Task CoroutineVariousErrorHandlingMatchesNunitSuite()
        {
            string script =
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
            Script host = new() { Options = { DebugPrint = s => lastPrinted = s } };
            host.DoString(script);
            await Assert.That(lastPrinted).IsEqualTo("2");
        }

        [global::TUnit.Core.Test]
        public async Task CoroutineCanBeResumedDirectlyFromClr()
        {
            string script =
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

            Script host = new();
            DynValue factory = host.DoString(script);
            DynValue coroutine = host.CreateCoroutine(factory);

            string result = "";
            while (coroutine.Coroutine.State != CoroutineState.Dead)
            {
                DynValue yielded = coroutine.Coroutine.Resume();
                result += yielded.ToString();
            }

            await Assert.That(result).IsEqualTo("1234567");
        }

        [global::TUnit.Core.Test]
        public async Task CoroutineSupportsTypedEnumerable()
        {
            string script =
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

            Script host = new();
            DynValue factory = host.DoString(script);
            DynValue coroutine = host.CreateCoroutine(factory);

            string result = "";
            foreach (DynValue yielded in coroutine.Coroutine.AsTypedEnumerable())
            {
                result += yielded.ToString();
            }

            await Assert.That(result).IsEqualTo("1234567");
        }

        [global::TUnit.Core.Test]
        public async Task CoroutineAutoYieldResumesUntilCompletion()
        {
            string script =
                @"
                function fib(n)
                    if (n == 0 or n == 1) then
                        return 1;
                    else
                        return fib(n - 1) + fib(n - 2);
                    end
                end
                ";

            Script host = new(default(CoreModules));
            host.DoString(script);
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

            await Assert.That(result.Type).IsEqualTo(DataType.Number);
            await Assert.That(result.Number).IsEqualTo(34);
            await Assert.That(cycles > 10).IsTrue();
        }
    }
}
#pragma warning restore CA2007
