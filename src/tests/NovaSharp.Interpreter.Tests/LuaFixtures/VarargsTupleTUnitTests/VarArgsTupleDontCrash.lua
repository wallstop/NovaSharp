-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/EndToEnd/VarargsTupleTUnitTests.cs:63
-- @test: VarargsTupleTUnitTests.VarArgsTupleDontCrash
-- @compat-notes: Lua 5.3+: bitwise operators
function f(a,b)
                    local debug = 'a: ' .. tostring(a) .. ' b: ' .. tostring(b)
                    return debug
                end

                function g(a, b, ...)
                    local debug = 'a: ' .. tostring(a) .. ' b: ' .. tostring(b)
                    local arg = {...}
                    debug = debug .. ' arg: {'
                    for k, v in pairs(arg) do
                        debug = debug .. tostring(v) .. ', '
                    end
                    debug = debug .. '}'
                    return debug
                end

                function r()
                    return 1, 2, 3
                end

                function h(...)
                    return g(...)
                end

                function i(...)
                    return g('extra', ...)
                end
