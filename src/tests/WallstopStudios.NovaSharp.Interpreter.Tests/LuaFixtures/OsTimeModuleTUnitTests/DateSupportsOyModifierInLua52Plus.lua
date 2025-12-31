-- @lua-versions: 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\OsTimeModuleTUnitTests.cs:348
-- @test: OsTimeModuleTUnitTests.DateSupportsOyModifierInLua52Plus
-- @compat-notes: Test targets Lua 5.2+
return os.date('!%Oy', 0)
