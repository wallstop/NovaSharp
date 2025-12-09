-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\IoModuleTUnitTests.cs:986
-- @test: IoModuleTUnitTests.LinesMethodIteratesOverHandle
-- @compat-notes: NovaSharp: unresolved C# interpolation placeholder
local f = assert(io.open('{escapedPath}', 'r'))
                    local out = {{}}
                    for line in f:lines() do
                        out[#out + 1] = line
                    end
                    return out[1], out[2], out[3], io.type(f)
