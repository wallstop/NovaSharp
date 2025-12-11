-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\StreamFileUserDataBaseTUnitTests.cs:290
-- @test: StreamFileUserDataBaseTUnitTests.SeekPropagatesExceptionThroughPcall
-- @compat-notes: Lua 5.3+: bitwise operators; Uses injected variable: file
local ok, err = pcall(function()
                    return file:seek('set', 1)
                end)
                return ok, err
