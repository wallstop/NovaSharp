-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/StreamFileUserDataBaseTUnitTests.cs:1031
-- @test: StreamFileUserDataBaseTUnitTests.ReadThrowsOnInvalidOption
-- @compat-notes: Lua 5.3+: bitwise operators
local ok, err = pcall(function()
                    return file:read('*z')
                end)
                return ok, err
