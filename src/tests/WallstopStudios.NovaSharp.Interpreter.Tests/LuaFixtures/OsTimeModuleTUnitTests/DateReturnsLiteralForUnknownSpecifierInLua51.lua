-- @lua-versions: 5.1, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\OsTimeModuleTUnitTests.cs:259
-- @test: OsTimeModuleTUnitTests.DateReturnsLiteralForUnknownSpecifierInLua51
-- @compat-notes: Test targets Lua 5.1
return os.date('%Q', 1609459200)
