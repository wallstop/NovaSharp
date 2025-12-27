-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Spec/StringPackModuleTUnitTests.cs:248
-- @test: StringPackModuleTUnitTests.UnpackReturnsNextPosition
-- @compat-notes: Test targets Lua 5.3+; Lua 5.3+: string.pack (5.3+); Lua 5.3+: string.unpack (5.3+)
local packed = string.pack('i4 i4', 10, 20)
                local a, b, nextpos = string.unpack('i4 i4', packed)
                return nextpos
