-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Spec/LuaBasicMultiVersionSpecTUnitTests.cs:57
-- @test: LuaBasicMultiVersionSpecTUnitTests.ToNumberErrorsWhenBaseIsOutOfRange
-- @compat-notes: Lua 5.3+: bitwise operators
local ok, err = pcall(tonumber, '1', 40) return ok, err
