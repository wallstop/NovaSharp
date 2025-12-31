-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:2756
-- @test: StringModuleTUnitTests.FormatQMixedContentString
return string.format('%q', 'hello"world\\	ab' .. string.char(9))
