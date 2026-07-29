-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/ValueSlotSemanticsTUnitTests.cs:211
-- @test: ValueSlotSemanticsTUnitTests.ClosuresShareOneCellForTheSameLocal
local n = 0
                local bump = function() n = n + 1 end
                local read = function() return n end
                bump()
                bump()
                return read(), n
