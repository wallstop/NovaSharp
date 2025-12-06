-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/StreamFileUserDataBaseTUnitTests.cs:445
-- @test: StreamFileUserDataBaseTUnitTests.ReadSupportsNumericAndAllModes
-- @compat-notes: Lua 5.3+: bitwise operators
local f = file
                local num = f:read('*n')
                f:seek('set', 5)
                local rest = f:read(2)
                local all = f:read('*a')
                return num, rest, all
