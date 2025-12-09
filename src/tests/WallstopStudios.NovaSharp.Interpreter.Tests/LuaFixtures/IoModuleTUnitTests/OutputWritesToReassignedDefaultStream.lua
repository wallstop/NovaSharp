-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\IoModuleTUnitTests.cs:153
-- @test: IoModuleTUnitTests.OutputWritesToReassignedDefaultStream
-- @compat-notes: NovaSharp: unresolved C# interpolation placeholder
local f = io.open('{path}', 'w')
                io.output(f)
                io.write('abc', '123')
                io.output():close()
