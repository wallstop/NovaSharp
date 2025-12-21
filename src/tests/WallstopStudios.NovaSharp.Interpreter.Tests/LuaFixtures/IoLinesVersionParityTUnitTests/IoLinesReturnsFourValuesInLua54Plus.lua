-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/IoLinesVersionParityTUnitTests.cs:62
-- @test: IoLinesVersionParityTUnitTests.IoLinesReturnsFourValuesInLua54Plus
-- @compat-notes: NovaSharp: unresolved C# interpolation placeholder; Test targets Lua 5.4+
local a, b, c, d = io.lines('{path}')
                -- a is callable (either function or userdata with __call)
                local isCallable = type(a) == 'function' or (type(a) == 'userdata' and pcall(function() return a() end))
                return isCallable, b, c, io.type(d)
