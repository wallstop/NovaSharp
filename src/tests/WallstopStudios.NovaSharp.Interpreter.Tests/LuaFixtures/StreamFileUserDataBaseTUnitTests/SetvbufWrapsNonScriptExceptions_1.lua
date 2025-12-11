-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\StreamFileUserDataBaseTUnitTests.cs:314
-- @test: StreamFileUserDataBaseTUnitTests.SetvbufWrapsNonScriptExceptions
-- @compat-notes: Lua 5.3+: bitwise operators; Uses injected variable: file
local ok, err = pcall(function()
                    return file:setvbuf('line')
                end)
                return ok, err
