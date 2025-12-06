-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Units/ClosureTUnitTests.cs:67
-- @test: ClosureTUnitTests.GetUpValuesExposesCapturedSymbols
-- @compat-notes: Lua 5.3+: bitwise operators
local x = 3
                local y = 4
                return function()
                    return x + y
                end
