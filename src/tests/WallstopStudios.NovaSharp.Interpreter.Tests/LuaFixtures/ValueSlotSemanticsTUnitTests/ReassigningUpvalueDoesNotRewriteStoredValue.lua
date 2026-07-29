-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/ValueSlotSemanticsTUnitTests.cs:51
-- @test: ValueSlotSemanticsTUnitTests.ReassigningUpvalueDoesNotRewriteStoredValue
local x = 1
                local set = function(v) x = v end
                local t = { x }
                set(2)
                t[2] = x
                return t[1], t[2]
