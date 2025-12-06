-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/IoModuleTUnitTests.cs:443
-- @test: IoModuleTUnitTests.IoReadUsesDefaultInputWhenNoFileProvided
-- @compat-notes: Lua 5.3+: bitwise operators
local first = io.read('*l')
                local second = io.read('*l')
                local eof = io.read('*l')
                return first, second, eof
