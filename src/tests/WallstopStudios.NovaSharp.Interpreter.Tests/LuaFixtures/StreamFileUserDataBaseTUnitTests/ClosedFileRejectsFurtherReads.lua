-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/StreamFileUserDataBaseTUnitTests.cs:359
-- @test: StreamFileUserDataBaseTUnitTests.ClosedFileRejectsFurtherReads
-- @compat-notes: Lua 5.3+: bitwise operators
local f = file
                f:close()
                local ok, err = pcall(function()
                    return f:read()
                end)
                return ok, err
