-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\OsTimeModuleTUnitTests.cs:442
-- @test: OsTimeModuleTUnitTests.DateErrorMessageIncludesCorrectSpecifierContext
-- NovaSharp: unresolved C# interpolation placeholder; Test targets Lua 5.2+
return os.date('{formatString}', 1609459200)
