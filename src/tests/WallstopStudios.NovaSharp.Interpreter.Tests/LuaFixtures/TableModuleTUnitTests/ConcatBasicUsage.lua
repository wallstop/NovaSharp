-- @lua-versions: 5.1, 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/TableModuleTUnitTests.cs
-- @test: TableModuleTUnitTests.ConcatBasicUsage
-- @compat-notes: table.concat basic usage

local t = { 'a', 'b', 'c', 'd' }
local result = table.concat(t)
assert(result == "abcd", "table.concat with no separator should return 'abcd', got: " .. result)
print("PASS: table.concat({'a', 'b', 'c', 'd'}) = '" .. result .. "'")