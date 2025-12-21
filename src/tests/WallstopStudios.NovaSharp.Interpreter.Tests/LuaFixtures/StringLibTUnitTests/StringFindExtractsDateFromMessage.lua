-- @lua-versions: 5.1, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/StringLibTUnitTests.cs:126
-- @test: StringLibTUnitTests.StringFindExtractsDateFromMessage
-- @compat-notes: Test targets Lua 5.1
s = 'Deadline is 30/05/1999, firm'
				date = '%d%d/%d%d/%d%d%d%d';
                return s:sub(s:find(date));
