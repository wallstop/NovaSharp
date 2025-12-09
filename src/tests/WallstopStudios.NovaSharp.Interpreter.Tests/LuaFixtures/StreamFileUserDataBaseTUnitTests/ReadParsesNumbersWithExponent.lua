-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StreamFileUserDataBaseTUnitTests.cs:492
-- @test: StreamFileUserDataBaseTUnitTests.ReadParsesNumbersWithExponent
-- @compat-notes: Lua 5.3+: bitwise operators; Uses injected variable: file
local f = file
                local num = f:read('*n')
                local tail = f:read('*a')
                return num, tail
