-- @lua-versions: 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/Utf8ModuleTUnitTests.cs:218
-- @test: Utf8ModuleTUnitTests.Utf8CodesIteratorReturnsNilWhenControlIsPastEnd
-- @compat-notes: Test targets Lua 5.4+; Lua 5.3+: bitwise operators; Lua 5.3+: utf8 library
local iter, state = utf8.codes('abc')
                return iter(state, #state + 5)
