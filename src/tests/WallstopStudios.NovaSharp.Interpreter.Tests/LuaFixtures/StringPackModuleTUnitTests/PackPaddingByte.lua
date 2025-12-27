-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Spec/StringPackModuleTUnitTests.cs:335
-- @test: StringPackModuleTUnitTests.PackPaddingByte
-- @compat-notes: Test targets Lua 5.3+; Lua 5.3+: string.pack (5.3+)
local packed = string.pack('BxB', 1, 2)
                return #packed
