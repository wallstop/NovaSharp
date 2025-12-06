-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/LoadModuleTUnitTests.cs:180
-- @test: LoadModuleTUnitTests.LoadFileSafeUsesSafeEnvironmentWhenNotProvided
-- @compat-notes: Lua 5.3+: bitwise operators
local fn = loadfilesafe('safe.lua'); return fn()
