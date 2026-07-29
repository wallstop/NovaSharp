-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ScriptLoadTUnitTests.cs:1697
-- @test: ScriptLoadTUnitTests.BindFunctionSupportsCallableMetamethods
local target = setmetatable({}, { __call = function(_, value) return value * 2 end }); return target
