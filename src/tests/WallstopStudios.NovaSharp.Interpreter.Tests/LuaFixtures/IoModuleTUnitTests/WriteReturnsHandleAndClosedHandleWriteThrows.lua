-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/IoModuleTUnitTests.cs:795
-- @test: IoModuleTUnitTests.WriteReturnsHandleAndClosedHandleWriteThrows
-- @compat-notes: NovaSharp: unresolved C# interpolation placeholder
local f = assert(io.open('{escapedPath}', 'w'))
                    local returned = f:write('payload')
                    f:close()
                    local ok, err = pcall(function() f:write('more') end)
                    return returned == f, ok, err
