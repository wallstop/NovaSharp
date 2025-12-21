-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/CoroutineTUnitTests.cs:177
-- @test: CoroutineTUnitTests.CoroutineVariousErrorHandlingMatchesNunitSuite
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
