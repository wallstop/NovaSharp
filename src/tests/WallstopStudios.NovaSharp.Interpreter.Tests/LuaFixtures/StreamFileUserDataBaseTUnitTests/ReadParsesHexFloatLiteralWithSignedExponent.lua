-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StreamFileUserDataBaseTUnitTests.cs:635
-- @test: StreamFileUserDataBaseTUnitTests.ReadParsesHexFloatLiteralWithSignedExponent
-- Uses injected variable: file
local f = file
                local number, overflow, underflow, subnormal, rounding, fullMask, maxInteger =
                    f:read('*n', '*n', '*n', '*n', '*n', '*n', '*n')
                local remainder = f:read('*a')
                return number, overflow, underflow, subnormal, rounding,
                    fullMask, maxInteger, remainder
