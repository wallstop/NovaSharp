-- @lua-versions: all
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:2085
-- @test: StringModuleTUnitTests.FormatDecimalWithFloatBehaviorByVersion
-- Test targets Lua 5.1
return string.format('%d', 123.456)
