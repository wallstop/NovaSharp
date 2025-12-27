-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/IoModuleTUnitTests.cs:1233
-- @test: IoModuleTUnitTests.OpenFileInvokesPlatformAccessorAndStillWritesToDisk
-- @compat-notes: NovaSharp: unresolved C# interpolation placeholder; Test targets Lua 5.1
local f = assert(io.open('{escapedPath}', 'w'))
                f:write('hooked payload')
                f:close()
