-- @lua-versions: 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/TableModuleTUnitTests.cs:38
-- @test: TableModuleTUnitTests.UnpackHonorsExplicitBounds
-- @compat-notes: Test targets Lua 5.2+; Lua 5.2+: table.unpack (5.2+)
local values = { 10, 20, 30, 40 }
                return table.unpack(values, 2, 3)
