-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Units/BinaryMetamethodTUnitTests.cs:15
-- @test: BinaryMetamethodTUnitTests.FloorDivisionMetamethodOverridesOperator
-- @compat-notes: Lua 5.3+: bitwise operators
local meta = {}
                function meta.__idiv(lhs, rhs)
                    assert(lhs.value == 10)
                    assert(rhs == 3)
                    return lhs.value + rhs
                end

                local operand = setmetatable({ value = 10 }, meta)
                return operand // 3
