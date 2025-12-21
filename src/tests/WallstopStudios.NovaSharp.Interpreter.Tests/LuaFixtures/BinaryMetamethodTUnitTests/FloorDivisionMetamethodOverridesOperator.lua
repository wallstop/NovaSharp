-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/BinaryMetamethodTUnitTests.cs:19
-- @test: BinaryMetamethodTUnitTests.FloorDivisionMetamethodOverridesOperator
-- @compat-notes: Test targets Lua 5.3+; Lua 5.3+: floor division
local meta = {}
                function meta.__idiv(lhs, rhs)
                    assert(lhs.value == 10)
                    assert(rhs == 3)
                    return lhs.value + rhs
                end

                local operand = setmetatable({ value = 10 }, meta)
                return operand // 3
