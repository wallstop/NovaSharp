-- @lua-versions: 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/StringLibTUnitTests.cs:142
-- @test: StringLibTUnitTests.StringFindSupportsFrontierPatterns
-- @compat-notes: Test targets Lua 5.4+
s = 'Deadline is 30/05/1999, firm'
				date = '%f[%S]%d%d/%d%d/%d%d%d%d';
                return s:sub(s:find(date));
