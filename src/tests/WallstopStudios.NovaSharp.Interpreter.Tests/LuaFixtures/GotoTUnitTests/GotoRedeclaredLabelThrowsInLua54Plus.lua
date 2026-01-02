-- @lua-versions: 5.2+
-- @novasharp-only: false
-- @expects-error: true
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\EndToEnd\GotoTUnitTests.cs:166
-- @test: GotoTUnitTests.GotoRedeclaredLabelThrowsInLua54Plus
-- @compat-notes: Test targets Lua 5.2+; Lua 5.2+: goto statement (5.2+); Lua 5.2+: label (5.2+)
::label::
                do
                    goto label
                    do return 5 end
                    ::label::
                    return 3
                end
