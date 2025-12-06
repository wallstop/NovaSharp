-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/IoModuleTUnitTests.cs:133
-- @test: IoModuleTUnitTests.OutputWritesToReassignedDefaultStream
-- @compat-notes: Lua 5.3+: bitwise operators
local f = io.open('{path}', 'w')
                io.output(f)
                io.write('abc', '123')
                io.output():close()
