-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/IoModuleTUnitTests.cs:78
-- @test: IoModuleTUnitTests.ReadNumberConsumesMultipleLinesFromStdin
-- @compat-notes: Test targets Lua 5.1
while true do
    local n1, n2, n3 = io.read('*number', '*number', '*number')
    if not n1 then
        break
    end

    print(math.max(n1, n2, n3))
end
