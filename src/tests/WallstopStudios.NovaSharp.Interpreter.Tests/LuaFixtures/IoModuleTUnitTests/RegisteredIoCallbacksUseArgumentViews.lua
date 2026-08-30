-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/IoModuleTUnitTests.cs:29
-- @test: IoModuleTUnitTests.RegisteredIoCallbacksUseArgumentViews
local file = assert(io.tmpfile())
assert(io.type(file) == 'file')
local write_result = file:write('alpha\nbeta\n')
if _VERSION == 'Lua 5.1' then
    assert(write_result == true)
else
    assert(write_result == file)
end
assert(file:flush() == true)
assert(file:seek('set', 0) == 0)

local iterator = file:lines()
assert(iterator() == 'alpha')
assert(iterator() == 'beta')
assert(iterator() == nil)

assert(file:close() == true)
assert(io.type(file) == 'closed file')
assert(io.type(42) == nil)
print('PASS')
