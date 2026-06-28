-- @lua-versions: 5.2+
-- @novasharp-only: true
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\OsTimeModuleTUnitTests.cs:348
-- @test: OsTimeModuleTUnitTests.DateSupportsOyModifierInLua52Plus
-- %Oy strftime modifier behavior varies by platform C library; Windows strftime produces empty output for %Oy
return os.date('!%Oy', 0)
