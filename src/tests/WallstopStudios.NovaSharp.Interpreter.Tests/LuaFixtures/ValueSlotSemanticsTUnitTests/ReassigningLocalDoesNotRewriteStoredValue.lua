-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/ValueSlotSemanticsTUnitTests.cs:30
-- @test: ValueSlotSemanticsTUnitTests.ReassigningLocalDoesNotRewriteStoredValue
local x = 1
                local t = { x, x }
                x = 2
                t[3] = x
                return t[1], t[2], t[3], x
