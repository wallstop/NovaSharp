-- @lua-versions: 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: true
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\EndToEnd\DynamicTUnitTests.cs:74
-- @test: DynamicTUnitTests.DynamicAccessScopeSecurityReturnsNil
-- @compat-notes: NovaSharp: dynamic access; Test targets Lua 5.2+
-- Note: The worker function must reference a global so it captures the shadowed _ENV.
-- In Lua 5.2+, closures only capture _ENV when they actually reference global variables.
-- Without the dummy reference to `_`, the worker function wouldn't have _ENV as an upvalue,
-- and dynamic.eval would find the script's global _ENV instead of the empty local one.
a = 5;
local prepared = dynamic.prepare('a');
local eval = dynamic.eval;
local _ENV = { }
function worker()
    local _ = _  -- Force capture of _ENV by referencing a global
    return eval(prepared);
end
return worker();
