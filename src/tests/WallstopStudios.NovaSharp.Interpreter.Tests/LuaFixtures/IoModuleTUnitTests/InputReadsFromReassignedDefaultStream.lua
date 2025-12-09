-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\IoModuleTUnitTests.cs:136
-- @test: IoModuleTUnitTests.InputReadsFromReassignedDefaultStream
-- @compat-notes: NovaSharp: unresolved C# interpolation placeholder
io.input('{path}')
                return io.read('*l')
