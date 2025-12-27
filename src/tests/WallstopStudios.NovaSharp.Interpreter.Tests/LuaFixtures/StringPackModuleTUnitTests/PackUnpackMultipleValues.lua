-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Spec/StringPackModuleTUnitTests.cs:206
-- @test: StringPackModuleTUnitTests.PackUnpackMultipleValues
-- @compat-notes: Test targets Lua 5.3+; Lua 5.3+: string.pack (5.3+); Lua 5.3+: string.unpack (5.3+)
local packed = string.pack('i4 i4 z', 100, 200, 'test')
                local a, b, c = string.unpack('i4 i4 z', packed)
                return a, b, c
