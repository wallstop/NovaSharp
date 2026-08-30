-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/IoModuleTUnitTests.cs:128
-- @test: IoModuleTUnitTests.IoIteratorsUseArgumentViews
local file = assert(io.tmpfile())
file:write('file iterator\n')
file:seek('set', 0)
local file_iterator = file:lines()
assert(file_iterator() == 'file iterator')
assert(file_iterator() == nil)
file:close()

local path = os.tmpname()
local output = assert(io.open(path, 'w'))
output:write('path iterator\n')
output:close()
local path_iterator = io.lines(path)
assert(path_iterator() == 'path iterator')
assert(path_iterator() == nil)
os.remove(path)

local default_path = os.tmpname()
local default_output = assert(io.open(default_path, 'w'))
default_output:write('default iterator\n')
default_output:close()
local default_input = assert(io.open(default_path, 'r'))
io.input(default_input)
local default_iterator = io.lines()
assert(default_iterator() == 'default iterator')
assert(default_iterator() == nil)
default_input:close()
os.remove(default_path)
print('PASS')
