-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTapParityTUnitTests.cs:183
-- @test: DebugModuleTapParityTUnitTests.SetUserValueRoundTrips
-- @compat-notes: Lua 5.2+: debug.getuservalue (5.2+); Lua 5.2+: debug.setuservalue (5.2+); Uses injected variable: userdata
debug.setuservalue(handle, { label = 'userdata' })
                local value = debug.getuservalue(handle)
                return value and value.label
