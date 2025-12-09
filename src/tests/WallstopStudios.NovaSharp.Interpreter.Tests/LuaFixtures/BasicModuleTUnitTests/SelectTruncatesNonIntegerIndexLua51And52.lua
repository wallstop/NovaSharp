-- @lua-versions: 5.1, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/BasicModuleTUnitTests.cs:454
-- @test: BasicModuleTUnitTests.SelectTruncatesNonIntegerIndexLua51And52
-- @compat-notes: Test targets Lua 5.1
return select(1.5, 'a', 'b', 'c')
