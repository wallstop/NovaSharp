-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTapParityTUnitTests.cs:172
-- @test: DebugModuleTapParityTUnitTests.Unknown
-- @compat-notes: Lua 5.3+: bitwise operators; Uses injected variable: userdata
debug.setuservalue(handle, { label = 'userdata' })
                local value = debug.getuservalue(handle)
                return value and value.label
