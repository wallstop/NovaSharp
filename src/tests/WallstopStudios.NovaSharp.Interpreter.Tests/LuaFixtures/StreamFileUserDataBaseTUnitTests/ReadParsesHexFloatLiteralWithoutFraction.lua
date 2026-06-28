-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\StreamFileUserDataBaseTUnitTests.cs:556
-- @test: StreamFileUserDataBaseTUnitTests.ReadParsesHexFloatLiteralWithoutFraction
-- Uses injected variable: file
local f = file
                local number = f:read('*n')
                local remainder = f:read('*a')
                return number, remainder
