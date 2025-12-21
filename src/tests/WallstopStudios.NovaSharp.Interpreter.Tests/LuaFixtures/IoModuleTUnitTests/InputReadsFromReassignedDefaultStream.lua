-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/IoModuleTUnitTests.cs:168
-- @test: IoModuleTUnitTests.InputReadsFromReassignedDefaultStream
-- @compat-notes: NovaSharp: unresolved C# interpolation placeholder; Test targets Lua 5.1
io.input('{path}')
                return io.read('*l')
