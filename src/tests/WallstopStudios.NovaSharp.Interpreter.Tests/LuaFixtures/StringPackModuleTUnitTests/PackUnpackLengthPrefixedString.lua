-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Spec/StringPackModuleTUnitTests.cs:136
-- @test: StringPackModuleTUnitTests.PackUnpackLengthPrefixedString
-- @compat-notes: Test targets Lua 5.3+; Lua 5.3+: string.pack (5.3+); Lua 5.3+: string.unpack (5.3+)
local packed = string.pack('s4', 'world')
                local unpacked = string.unpack('s4', packed)
                return unpacked
