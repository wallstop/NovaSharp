-- @lua-versions: 5.3, 5.4
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/StreamFileUserDataBaseTUnitTests.cs:407
-- @test: StreamFileUserDataBaseTUnitTests.SetvbufPropagatesExceptionThroughPcall
-- @compat-notes: Lua 5.3+: bitwise operators
local ok, err = pcall(function()
                    return file:setvbuf('line')
                end)
                return ok, err
