-- @lua-versions: 5.4
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/Utf8ModuleTUnitTests.cs:236
-- @test: Utf8ModuleTUnitTests.Utf8CodesIteratorThrowsWhenControlPointsInsideRune
-- @compat-notes: Test targets Lua 5.4+; Lua 5.3+: bitwise operators; Lua 5.3+: utf8 library
local iter, state = utf8.codes(emoji)
                    iter(state, 3)
