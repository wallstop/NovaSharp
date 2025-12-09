-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/IoModuleTUnitTests.cs:116
-- @test: IoModuleTUnitTests.TypeReportsClosedFileAfterClose
-- @compat-notes: NovaSharp: unresolved C# interpolation placeholder; Test targets Lua 5.1
local f = io.open('{path}', 'w')
                local openType = io.type(f)
                f:close()
                return openType, io.type(f)
