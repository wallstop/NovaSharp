-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\EndToEnd\RealWorldScriptTUnitTests.cs:120
-- @test: RealWorldScriptTUnitTests.InspectLuaFixtureFormatsTables
-- @compat-notes: Lua 5.3+: bitwise operators
local ui = setmetatable(
                        { isVisible = true },
                        { __tostring = function() return 'ui-meta' end }
                    )

                    return {
                        player = { name = 'Nova', stats = { hp = 100, mana = 45 } },
                        tags = { 'alpha', 'beta' },
                        overlay = ui
                    }
