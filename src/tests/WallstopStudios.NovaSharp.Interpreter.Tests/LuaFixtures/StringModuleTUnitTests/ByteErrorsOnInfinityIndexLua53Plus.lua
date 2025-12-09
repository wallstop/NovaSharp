-- @lua-versions: 5.1, 5.5
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:298
-- @test: StringModuleTUnitTests.ByteErrorsOnInfinityIndexLua53Plus
-- @compat-notes: Test targets Lua 5.1
return string.byte('Lua', 1/0)
