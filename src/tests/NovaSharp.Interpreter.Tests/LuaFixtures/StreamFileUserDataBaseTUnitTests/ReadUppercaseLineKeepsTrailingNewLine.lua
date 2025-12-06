-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/StreamFileUserDataBaseTUnitTests.cs:837
-- @test: StreamFileUserDataBaseTUnitTests.ReadUppercaseLineKeepsTrailingNewLine
-- @compat-notes: Lua 5.3+: bitwise operators
local f = file
                local a = f:read('*L')
                local b = f:read('*L')
                return a, b
