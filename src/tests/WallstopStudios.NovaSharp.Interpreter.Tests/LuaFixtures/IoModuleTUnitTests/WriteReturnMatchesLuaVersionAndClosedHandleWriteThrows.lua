-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/IoModuleTUnitTests.cs:1284
-- @test: IoModuleTUnitTests.WriteReturnMatchesLuaVersionAndClosedHandleWriteThrows
local file = assert(io.tmpfile())
local returned = file:write('payload')

if _VERSION == 'Lua 5.1' then
    assert(returned == true)
else
    assert(returned == file)
end

assert(file:close() == true)
local ok, err = pcall(function()
    file:write('more')
end)
assert(ok == false)
assert(type(err) == 'string')
print('PASS')
