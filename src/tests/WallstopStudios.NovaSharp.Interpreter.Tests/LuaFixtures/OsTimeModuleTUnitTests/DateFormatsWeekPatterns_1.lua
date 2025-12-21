-- @lua-versions: 5.1, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/OsTimeModuleTUnitTests.cs:274
-- @test: OsTimeModuleTUnitTests.DateFormatsWeekPatterns
-- @compat-notes: Test targets Lua 5.1
return os.date('!%U-%W-%V', 345600)
