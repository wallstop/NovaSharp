-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\IoModuleTUnitTests.cs:276
-- @test: IoModuleTUnitTests.ReadMultipleFixedLengthsReturnsExpectedChunks
-- @compat-notes: NovaSharp: unresolved C# interpolation placeholder
local f = assert(io.open('{escapedPath}', 'r'))
                    local first, second, third = f:read(4, 4, 4)
                    f:close()
                    return first, second, third
