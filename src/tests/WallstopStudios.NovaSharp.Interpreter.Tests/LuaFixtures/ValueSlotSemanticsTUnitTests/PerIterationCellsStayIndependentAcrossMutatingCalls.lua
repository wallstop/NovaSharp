-- @lua-versions: 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/ValueSlotSemanticsTUnitTests.cs:152
-- @test: ValueSlotSemanticsTUnitTests.PerIterationCellsStayIndependentAcrossMutatingCalls
-- Compatibility notes: Test targets Lua 5.2+
local fns = {}
                for i = 1, 3 do
                    local k = i
                    fns[#fns + 1] = function() k = k + 1 return k end
                end
                local function drain()
                    local out = {}
                    for i = 1, #fns do out[i] = tostring(fns[i]()) end
                    return table.concat(out, ",")
                end
                return drain(), drain()
