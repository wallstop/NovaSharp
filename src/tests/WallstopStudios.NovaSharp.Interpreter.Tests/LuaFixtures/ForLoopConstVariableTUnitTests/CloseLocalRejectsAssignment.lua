-- @lua-versions: 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ForLoopConstVariableTUnitTests.cs:144
-- @test: ForLoopConstVariableTUnitTests.CloseLocalRejectsAssignment
-- Compatibility notes: Test targets Lua 5.4+; Lua 5.4+: close attribute
local value <close> = setmetatable({}, { __close = function() end })
                      value = setmetatable({}, { __close = function() end })
