-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\IoModuleTUnitTests.cs:647
-- @test: IoModuleTUnitTests.FlushReturnsTrueForCurrentOutput
-- @compat-notes: NovaSharp: unresolved C# interpolation placeholder
local f = assert(io.open('{escapedPath}', 'w'))
                    io.output(f)
                    io.write('buffered')
                    local ok = io.flush()
                    io.output():close()
                    return ok
