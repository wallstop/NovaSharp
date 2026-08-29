-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs:56
-- @test: DebugModuleTUnitTests.RegisteredDebugCallbacksUseArgumentViews
-- The representative callback path is common to every compatibility version.
local target = {}
local metatable = { marker = 42 }
debug.setmetatable(target, metatable)
local info = debug.getinfo(debug.getregistry, 'S')
return debug.getmetatable(target).marker, info.what
