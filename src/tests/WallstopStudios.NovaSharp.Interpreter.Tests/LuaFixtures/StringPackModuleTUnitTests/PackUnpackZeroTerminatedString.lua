-- @lua-versions: 5.3+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Spec\StringPackModuleTUnitTests.cs:120
-- @test: StringPackModuleTUnitTests.PackUnpackZeroTerminatedString
-- Test targets Lua 5.3+; Lua 5.3+: string.pack (5.3+); Lua 5.3+: string.unpack (5.3+)
local packed = string.pack('z', 'hello')
                local unpacked = string.unpack('z', packed)
                return unpacked
