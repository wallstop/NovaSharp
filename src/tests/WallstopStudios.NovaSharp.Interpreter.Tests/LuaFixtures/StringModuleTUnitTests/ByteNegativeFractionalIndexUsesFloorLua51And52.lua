-- @lua-versions: 5.1, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:333
-- @test: StringModuleTUnitTests.ByteNegativeFractionalIndexUsesFloorLua51And52
-- @compat-notes: Test targets Lua 5.1
return string.byte('Lua', -0.5)
