-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\IoModuleTUnitTests.cs:530
-- @test: IoModuleTUnitTests.TypeReturnsNilForNonUserData
return io.type(123)
