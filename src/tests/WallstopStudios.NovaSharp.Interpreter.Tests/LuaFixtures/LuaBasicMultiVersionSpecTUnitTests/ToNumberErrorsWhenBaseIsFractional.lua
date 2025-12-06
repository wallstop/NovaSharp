-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Spec/LuaBasicMultiVersionSpecTUnitTests.cs:72
-- @test: LuaBasicMultiVersionSpecTUnitTests.ToNumberErrorsWhenBaseIsFractional
-- @compat-notes: Lua 5.3+: bitwise operators
local ok, err = pcall(tonumber, '10', 2.5) return ok, err
