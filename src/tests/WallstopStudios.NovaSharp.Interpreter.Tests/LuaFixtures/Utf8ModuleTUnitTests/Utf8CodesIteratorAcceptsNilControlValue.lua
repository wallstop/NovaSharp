-- @lua-versions: 5.3+
-- @novasharp-only: false
-- @expects-error: true
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\Utf8ModuleTUnitTests.cs:286
-- @test: Utf8ModuleTUnitTests.Utf8CodesIteratorAcceptsNilControlValue
-- Test targets Lua 5.3+; Lua 5.3+: utf8 library
local iter, state = utf8.codes('ab')
                local pos, cp = iter(state, nil)
                return pos, cp
