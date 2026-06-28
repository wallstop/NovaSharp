-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\IoModuleTUnitTests.cs:1150
-- @test: IoModuleTUnitTests.OpenSupportsExplicitEncoding
-- NovaSharp: unresolved C# interpolation placeholder; Test targets Lua 5.1
local f = assert(io.open('{escapedPath}', 'w', 'utf-16'))
                f:write('hello')
                f:close()
