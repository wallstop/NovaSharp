-- @lua-versions: 5.1, 5.2, 5.3
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/StringArithmeticCoercionTUnitTests.cs
-- @test: StringArithmeticCoercionTUnitTests.StringMetatableDoesNotHaveArithmeticMetamethodsInPreLua54
-- @compat-notes: Lua 5.1-5.3 use built-in operator coercion, not string metatable metamethods

-- Test: String metatable lacks arithmetic metamethods in pre-5.4 Lua
-- Reference: Lua 5.1-5.3 manuals

local mt = getmetatable('')

-- Verify arithmetic metamethods are absent
assert(mt.__add == nil, "__add should be nil")
assert(mt.__sub == nil, "__sub should be nil")
assert(mt.__mul == nil, "__mul should be nil")
assert(mt.__div == nil, "__div should be nil")
assert(mt.__mod == nil, "__mod should be nil")
assert(mt.__pow == nil, "__pow should be nil")
-- Note: __idiv only exists in Lua 5.3+ (floor division operator)
-- Note: __unm is for unary minus

print("Arithmetic metamethods absent in string metatable (pre-5.4)")
return true
