-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/StreamFileUserDataBaseTUnitTests.cs:492
-- @test: StreamFileUserDataBaseTUnitTests.ReadParsesNumbersWithExponent
-- @compat-notes: Lua 5.3+: bitwise operators
local f = file
                local num = f:read('*n')
                local tail = f:read('*a')
                return num, tail
