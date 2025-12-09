-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\StreamFileUserDataBaseTUnitTests.cs:837
-- @test: StreamFileUserDataBaseTUnitTests.ReadUppercaseLineKeepsTrailingNewLine
-- @compat-notes: Lua 5.3+: bitwise operators; Uses injected variable: file
local f = file
                local a = f:read('*L')
                local b = f:read('*L')
                return a, b
