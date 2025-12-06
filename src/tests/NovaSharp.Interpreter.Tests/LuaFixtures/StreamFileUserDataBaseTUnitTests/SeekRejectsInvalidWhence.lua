-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/StreamFileUserDataBaseTUnitTests.cs:245
-- @test: StreamFileUserDataBaseTUnitTests.SeekRejectsInvalidWhence
-- @compat-notes: Lua 5.3+: bitwise operators
local ok, err = pcall(function()
                    return file:seek('bogus', 0)
                end)
                return ok, err
