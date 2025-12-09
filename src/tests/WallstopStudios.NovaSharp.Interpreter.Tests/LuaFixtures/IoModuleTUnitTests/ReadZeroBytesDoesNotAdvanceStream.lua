-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\IoModuleTUnitTests.cs:253
-- @test: IoModuleTUnitTests.ReadZeroBytesDoesNotAdvanceStream
-- @compat-notes: NovaSharp: unresolved C# interpolation placeholder
local f = assert(io.open('{escapedPath}', 'r'))
                    local zero = f:read(0)
                    local chunk = f:read(3)
                    local remainder = f:read('*a')
                    f:close()
                    return zero, chunk, remainder
