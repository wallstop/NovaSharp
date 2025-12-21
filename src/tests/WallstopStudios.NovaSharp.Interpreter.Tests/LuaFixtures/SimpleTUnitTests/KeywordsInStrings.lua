-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/SimpleTUnitTests.cs:301
-- @test: SimpleTUnitTests.KeywordsInStrings
-- @compat-notes: NovaSharp: unresolved C# interpolation placeholder
x = '{keywrd}';
				return x;
