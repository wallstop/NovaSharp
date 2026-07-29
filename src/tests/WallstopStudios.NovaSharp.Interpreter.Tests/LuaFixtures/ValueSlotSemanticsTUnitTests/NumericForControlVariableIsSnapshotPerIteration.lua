-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/ValueSlotSemanticsTUnitTests.cs:73
-- @test: ValueSlotSemanticsTUnitTests.NumericForControlVariableIsSnapshotPerIteration
local values = {}
                local fns = {}
                for i = 1, 3 do
                    values[i] = i
                    fns[i] = function() return i end
                end
                return values[1], values[2], values[3], fns[1](), fns[2](), fns[3]()
