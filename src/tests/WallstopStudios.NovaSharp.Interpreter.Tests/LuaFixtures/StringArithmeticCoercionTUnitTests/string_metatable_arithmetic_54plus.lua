-- @lua-versions: 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/StringArithmeticCoercionTUnitTests.cs
-- @test: StringArithmeticCoercionTUnitTests.StringMetatableHasArithmeticMetamethodsInLua54Plus
-- @compat-notes: Lua 5.4+ adds arithmetic metamethods to string metatable for string-to-number coercion

-- Test: String metatable arithmetic metamethods exist in Lua 5.4+
-- Reference: Lua 5.4 manual §3.4.3 (Coercions and Conversions)

local mt = getmetatable('')

-- Verify arithmetic metamethods exist
assert(type(mt.__add) == "function", "__add should be function")
assert(type(mt.__sub) == "function", "__sub should be function")
assert(type(mt.__mul) == "function", "__mul should be function")
assert(type(mt.__div) == "function", "__div should be function")
assert(type(mt.__mod) == "function", "__mod should be function")
assert(type(mt.__pow) == "function", "__pow should be function")
assert(type(mt.__idiv) == "function", "__idiv should be function")
assert(type(mt.__unm) == "function", "__unm should be function")

print("All arithmetic metamethods present in string metatable")
return true
