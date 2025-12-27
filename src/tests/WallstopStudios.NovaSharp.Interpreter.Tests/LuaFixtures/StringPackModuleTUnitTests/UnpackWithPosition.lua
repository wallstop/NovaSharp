-- @lua-versions: none
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Spec/StringPackModuleTUnitTests.cs:264
-- @test: StringPackModuleTUnitTests.UnpackWithPosition
-- @compat-notes: Test targets Lua 5.1; Lua 5.3+: string.pack (5.3+); Lua 5.3+: string.unpack (5.3+)
local packed = string.pack('i4 i4', 10, 20)
                local b = string.unpack('i4', packed, 5)
                return b
