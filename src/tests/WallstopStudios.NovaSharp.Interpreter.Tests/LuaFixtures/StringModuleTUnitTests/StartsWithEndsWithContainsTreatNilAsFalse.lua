-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:670
-- @test: StringModuleTUnitTests.StartsWithEndsWithContainsTreatNilAsFalse
-- @compat-notes: NovaSharp: NovaSharp string extension; NovaSharp: NovaSharp string extension; NovaSharp: NovaSharp string extension
return string.startswith(nil, 'prefix'),
                       string.endswith('suffix', nil),
                       string.contains(nil, nil)
