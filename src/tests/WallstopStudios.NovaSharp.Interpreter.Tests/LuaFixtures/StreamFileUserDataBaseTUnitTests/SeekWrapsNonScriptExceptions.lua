-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StreamFileUserDataBaseTUnitTests.cs:268
-- @test: StreamFileUserDataBaseTUnitTests.SeekWrapsNonScriptExceptions
-- @compat-notes: Lua 5.3+: bitwise operators; Uses injected variable: file
local ok, err = pcall(function()
                    return file:seek('set', 0)
                end)
                return ok, err
