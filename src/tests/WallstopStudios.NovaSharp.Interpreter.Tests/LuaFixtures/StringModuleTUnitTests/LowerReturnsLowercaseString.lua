-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:143
-- @test: StringModuleTUnitTests.LowerReturnsLowercaseString
return string.lower('NovaSharp')
