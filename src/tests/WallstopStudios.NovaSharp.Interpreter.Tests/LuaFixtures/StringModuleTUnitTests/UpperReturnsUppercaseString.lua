-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:179
-- @test: StringModuleTUnitTests.UpperReturnsUppercaseString
return string.upper('NovaSharp')
