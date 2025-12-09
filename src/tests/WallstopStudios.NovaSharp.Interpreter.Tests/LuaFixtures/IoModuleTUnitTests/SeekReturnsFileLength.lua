-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: true
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\IoModuleTUnitTests.cs:339
-- @test: IoModuleTUnitTests.SeekReturnsFileLength
-- @compat-notes: NovaSharp: unresolved C# interpolation placeholder
local f = assert(io.open('{escapedPath}', 'r'))
                    local size = f:seek('end', 0)
                    f:close()
                    return size
