-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\IoModuleTUnitTests.cs:751
-- @test: IoModuleTUnitTests.TypeReportsClosedFileState
-- @compat-notes: NovaSharp: unresolved C# interpolation placeholder
local f = assert(io.open('{escapedPath}', 'r'))
                    local before = io.type(f)
                    f:close()
                    local after = io.type(f)
                    return before, after
