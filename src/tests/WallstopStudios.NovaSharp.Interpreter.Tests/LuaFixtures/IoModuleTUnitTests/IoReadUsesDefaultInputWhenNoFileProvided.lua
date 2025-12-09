-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/IoModuleTUnitTests.cs:463
-- @test: IoModuleTUnitTests.IoReadUsesDefaultInputWhenNoFileProvided
-- @compat-notes: Lua 5.3+: bitwise operators
local first = io.read('*l')
                local second = io.read('*l')
                local eof = io.read('*l')
                return first, second, eof
