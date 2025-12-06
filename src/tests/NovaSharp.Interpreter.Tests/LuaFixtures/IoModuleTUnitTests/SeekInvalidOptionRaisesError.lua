-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/IoModuleTUnitTests.cs:300
-- @test: IoModuleTUnitTests.SeekInvalidOptionRaisesError
-- @compat-notes: Lua 5.3+: bitwise operators
local f = assert(io.open('{escapedPath}', 'r'))
                        f:seek('bad', 0)
