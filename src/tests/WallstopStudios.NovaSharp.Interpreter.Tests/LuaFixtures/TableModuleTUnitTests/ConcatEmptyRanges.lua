-- @lua-versions: 5.1, 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/TableModuleTUnitTests.cs
-- @test: TableModuleTUnitTests.ConcatEmptyRangeEndBeforeStart
-- @compat-notes: table.concat returns empty string when end is before start

local t = {'a', 'b', 'c', 'd'}
local result = table.concat(t, '-', 3, 2)

assert(result == "", 
    string.format("table.concat with end before start should return '', got '%s'", result))
print("PASS: table.concat(t, '-', 3, 2) = '' (end before start)")
