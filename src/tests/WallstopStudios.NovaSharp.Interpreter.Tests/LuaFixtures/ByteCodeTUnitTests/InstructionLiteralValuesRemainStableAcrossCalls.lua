-- @lua-versions: all
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ByteCodeTUnitTests.cs
-- @test: ByteCodeTUnitTests.EmitLiteralFreezesWritableValues
local function literal()
    return 7
end

local tableValue = { before = 3 }

local function indexed()
    return tableValue.before
end

for _ = 1, 5 do
    assert(literal() == 7, 'numeric literal changed between calls')
    assert(indexed() == 3, 'literal table index changed between calls')
end

return literal(), indexed()
