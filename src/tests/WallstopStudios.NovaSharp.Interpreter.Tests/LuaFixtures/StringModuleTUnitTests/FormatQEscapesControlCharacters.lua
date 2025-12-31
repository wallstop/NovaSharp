-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:2714
-- @test: StringModuleTUnitTests.FormatQEscapesControlCharacters
-- @compat-notes: NovaSharp: unresolved C# interpolation placeholder
return string.format('%q', string.char({charCode}))
