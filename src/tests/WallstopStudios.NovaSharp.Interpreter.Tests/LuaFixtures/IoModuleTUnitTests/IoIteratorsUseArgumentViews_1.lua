-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/IoModuleTUnitTests.cs:127
-- @test: IoModuleTUnitTests.IoIteratorsUseArgumentViews
local file = assert(io.tmpfile())
file:write('first\nsecond\n')
file:seek('set', 0)
local file_iterator = file:lines()
file:close()

local path = os.tmpname()
local output = assert(io.open(path, 'w'))
output:write('path input\n')
output:close()
local path_iterator = io.lines(path)
assert(path_iterator() == 'path input')
assert(path_iterator() == nil)
os.remove(path)

local default_iterator = io.lines()
return file_iterator, path_iterator, default_iterator
