-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/VarargsTupleTUnitTests.cs:77
-- @test: VarargsTupleTUnitTests.VarArgsTupleDontCrash
-- @compat-notes: Uses injected variable: r
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
