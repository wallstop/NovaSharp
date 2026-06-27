-- @lua-versions: 5.2+
-- @novasharp-only: false
-- @expects-error: true
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\EndToEnd\GotoTUnitTests.cs:124
-- @test: GotoTUnitTests.GotoLabelShadowingProhibitedInLua54Plus
-- Test targets Lua 5.2+; Lua 5.2+: label (5.2+)
::label::
                do
                    ::label::
                end
