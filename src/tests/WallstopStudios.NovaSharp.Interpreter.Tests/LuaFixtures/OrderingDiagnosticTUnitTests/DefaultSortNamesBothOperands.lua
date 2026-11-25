-- @lua-versions: all
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/OrderingDiagnosticTUnitTests.cs:21
-- @test: OrderingDiagnosticTUnitTests.DefaultSortNamesBothOperands

local modern = _VERSION ~= 'Lua 5.1' and _VERSION ~= 'Lua 5.2'
local actor = setmetatable({}, { __name = 'Actor' })
local prop = setmetatable({}, { __name = 'Prop' })
local list = { actor, prop }
local ok, err = pcall(table.sort, list)
local expected = modern and 'attempt to compare Prop with Actor' or 'attempt to compare two table values'
assert(not ok and err:match('attempt to compare.*') == expected, tostring(err))

table.sort(list, function(left, right) return left == prop and right == actor end)
assert(list[1] == prop and list[2] == actor)
