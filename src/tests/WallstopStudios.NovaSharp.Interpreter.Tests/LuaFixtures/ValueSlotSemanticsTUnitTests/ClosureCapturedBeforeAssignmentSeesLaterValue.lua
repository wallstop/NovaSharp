-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/ValueSlotSemanticsTUnitTests.cs:231
-- @test: ValueSlotSemanticsTUnitTests.ClosureCapturedBeforeAssignmentSeesLaterValue
local x
                local read = function() return x end
                local before = read()
                x = 7
                return before == nil, read()
