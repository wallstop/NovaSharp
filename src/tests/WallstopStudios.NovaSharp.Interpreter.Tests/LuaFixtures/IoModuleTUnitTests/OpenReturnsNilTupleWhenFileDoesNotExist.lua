-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\IoModuleTUnitTests.cs:33
-- @test: IoModuleTUnitTests.OpenReturnsNilTupleWhenFileDoesNotExist
-- @compat-notes: NovaSharp: unresolved C# interpolation placeholder
return io.open('{path}', 'r')
