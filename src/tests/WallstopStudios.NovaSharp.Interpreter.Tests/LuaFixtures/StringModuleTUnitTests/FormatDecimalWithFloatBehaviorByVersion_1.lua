-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:2102
-- @test: StringModuleTUnitTests.FormatDecimalWithFloatBehaviorByVersion
return string.format('%d', 123.456)
