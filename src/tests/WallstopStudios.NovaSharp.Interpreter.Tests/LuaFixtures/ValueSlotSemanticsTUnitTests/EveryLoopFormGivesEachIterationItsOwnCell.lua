-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/ValueSlotSemanticsTUnitTests.cs:121
-- @test: ValueSlotSemanticsTUnitTests.EveryLoopFormGivesEachIterationItsOwnCell
-- Compatibility notes: NovaSharp: unresolved C# interpolation placeholder
local fns = {{}}
                    {body}
                    local out = {{}}
                    for i = 1, #fns do out[i] = tostring(fns[i]()) end
                    return table.concat(out, ",")
