-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: true
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\IoModuleTUnitTests.cs:299
-- @test: IoModuleTUnitTests.ReadOnClosedHandleThrows
-- @compat-notes: NovaSharp: unresolved C# interpolation placeholder
local f = assert(io.open('{escapedPath}', 'r'))
                        f:close()
                        f:read('*l')
