-- @lua-versions: 5.1, 5.2
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs
-- @test: StringModuleTUnitTests.ByteNegativeFractionalIndexUsesFloorLua51And52
-- Lua 5.1/5.2 use floor truncation for fractional indices.
-- -0.5 floored is -1, which means "last character" in Lua string semantics.
-- 'a' is the last character of 'Lua', ASCII 97.
return string.byte('Lua', -0.5)
