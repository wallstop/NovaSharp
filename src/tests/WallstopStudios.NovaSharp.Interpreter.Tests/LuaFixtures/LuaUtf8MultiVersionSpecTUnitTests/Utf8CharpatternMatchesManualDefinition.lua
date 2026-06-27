-- @lua-versions: 5.3+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Spec\LuaUtf8MultiVersionSpecTUnitTests.cs:109
-- @test: LuaUtf8MultiVersionSpecTUnitTests.Utf8CharpatternMatchesManualDefinition
-- Test targets Lua 5.3+; Lua 5.3+: utf8 library
return utf8.charpattern
