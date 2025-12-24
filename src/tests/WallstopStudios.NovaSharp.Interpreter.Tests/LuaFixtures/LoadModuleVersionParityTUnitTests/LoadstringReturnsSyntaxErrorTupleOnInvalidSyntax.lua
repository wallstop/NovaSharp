-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/LoadModuleVersionParityTUnitTests.cs:90
-- @test: LoadModuleVersionParityTUnitTests.LoadstringReturnsSyntaxErrorTupleOnInvalidSyntax
-- @compat-notes: Test targets Lua 5.1
local f, err = loadstring('function(')
                return f, err
