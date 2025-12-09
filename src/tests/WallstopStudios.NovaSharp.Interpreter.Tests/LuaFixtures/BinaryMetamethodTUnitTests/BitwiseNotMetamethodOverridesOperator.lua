-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Units\Execution\BinaryMetamethodTUnitTests.cs:39
-- @test: BinaryMetamethodTUnitTests.BitwiseNotMetamethodOverridesOperator
-- @compat-notes: Lua 5.3+: bitwise operators; Lua 5.3+: bitwise XOR/NOT
local meta = {}
                function meta.__bnot(value)
                    assert(value.tag == 'payload')
                    return 64
                end

                local operand = setmetatable({ tag = 'payload' }, meta)
                return ~operand
