-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/RealWorldScriptTUnitTests.cs:137
-- @test: RealWorldScriptTUnitTests.InspectLuaFixtureFormatsTables
local ui = setmetatable(
                        { isVisible = true },
                        { __tostring = function() return 'ui-meta' end }
                    )

                    return {
                        player = { name = 'Nova', stats = { hp = 100, mana = 45 } },
                        tags = { 'alpha', 'beta' },
                        overlay = ui
                    }
