-- @lua-versions: 5.1
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/IoModuleTUnitTests.cs:599
-- @test: IoModuleTUnitTests.IoReadUsesDefaultInputWhenNoFileProvided
-- @compat-notes: Test targets Lua 5.1
local first = io.read('*l')
                local second = io.read('*l')
                local eof = io.read('*l')
                return first, second, eof
