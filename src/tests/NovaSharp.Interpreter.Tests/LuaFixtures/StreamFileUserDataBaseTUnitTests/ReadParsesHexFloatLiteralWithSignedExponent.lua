-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/StreamFileUserDataBaseTUnitTests.cs:622
-- @test: StreamFileUserDataBaseTUnitTests.ReadParsesHexFloatLiteralWithSignedExponent
-- @compat-notes: Lua 5.3+: bitwise operators
local f = file
                local number = f:read('*n')
                local remainder = f:read('*a')
                return number, remainder
