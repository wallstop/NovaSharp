-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/DataTypes/ClosureTUnitTests.cs:79
-- @test: ClosureTUnitTests.GetUpValuesExposesCapturedSymbols
local x = 3
                local y = 4
                return function()
                    return x + y
                end
