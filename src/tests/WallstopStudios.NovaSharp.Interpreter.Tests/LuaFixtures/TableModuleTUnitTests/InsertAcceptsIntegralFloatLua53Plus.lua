-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/TableModuleTUnitTests.cs:365
-- @test: TableModuleTUnitTests.InsertAcceptsIntegralFloatLua53Plus
-- @compat-notes: Test targets Lua 5.3+; Lua 5.3+: bitwise operators
local t = {1, 2, 3}
                table.insert(t, 2.0, 'x')  -- 2.0 is integral
                return t[2]
