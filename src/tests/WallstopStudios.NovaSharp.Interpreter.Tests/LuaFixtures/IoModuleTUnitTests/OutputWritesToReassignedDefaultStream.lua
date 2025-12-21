-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/IoModuleTUnitTests.cs:190
-- @test: IoModuleTUnitTests.OutputWritesToReassignedDefaultStream
-- @compat-notes: NovaSharp: unresolved C# interpolation placeholder; Test targets Lua 5.1
local f = io.open('{path}', 'w')
                io.output(f)
                io.write('abc', '123')
                io.output():close()
