-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\OsTimeModuleTUnitTests.cs:504
-- @test: OsTimeModuleTUnitTests.DateLua51OutputsLiteralForUnknownSpecifiers
-- NovaSharp: unresolved C# interpolation placeholder; Test targets Lua 5.1
return os.date('{formatString}', 1609459200)
