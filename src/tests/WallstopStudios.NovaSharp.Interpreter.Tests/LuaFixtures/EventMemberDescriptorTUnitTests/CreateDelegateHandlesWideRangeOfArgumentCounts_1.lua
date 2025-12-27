-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Interop/Descriptors/EventMemberDescriptorTUnitTests.cs:540
-- @test: EventMemberDescriptorTUnitTests.CreateDelegateHandlesWideRangeOfArgumentCounts
local max = {MultiArityEventSource.MaxArity}
return function(...)
    local args = {{ ... }}
    local actual = 0
    for i = 1, max do
        if args[i] ~= nil then
            actual = i
        end
    end
    hits['{@case.Id}'] = {{ count = actual, args = args }}
end
