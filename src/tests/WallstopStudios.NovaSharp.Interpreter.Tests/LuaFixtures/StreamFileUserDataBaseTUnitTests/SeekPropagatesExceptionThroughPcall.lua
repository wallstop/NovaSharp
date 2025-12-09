-- @lua-versions: 5.3, 5.4
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/StreamFileUserDataBaseTUnitTests.cs:290
-- @test: StreamFileUserDataBaseTUnitTests.SeekPropagatesExceptionThroughPcall
-- @compat-notes: Lua 5.3+: bitwise operators
local ok, err = pcall(function()
                    return file:seek('set', 1)
                end)
                return ok, err
