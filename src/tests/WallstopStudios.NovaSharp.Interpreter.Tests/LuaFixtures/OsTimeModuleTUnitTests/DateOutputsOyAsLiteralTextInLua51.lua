-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/OsTimeModuleTUnitTests.cs:330
-- @test: OsTimeModuleTUnitTests.DateOutputsOyAsLiteralTextInLua51
-- @compat-notes: Test targets Lua 5.1
return os.date('!%Oy', 0)
