-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Spec/StringPackModuleTUnitTests.cs:187
-- @test: StringPackModuleTUnitTests.PackUnpackBigEndian
-- @compat-notes: Test targets Lua 5.3+; Lua 5.3+: string.pack (5.3+)
local packed = string.pack('>I2', 0x0102)
                local b1 = string.byte(packed, 1)
                local b2 = string.byte(packed, 2)
                return b1, b2
