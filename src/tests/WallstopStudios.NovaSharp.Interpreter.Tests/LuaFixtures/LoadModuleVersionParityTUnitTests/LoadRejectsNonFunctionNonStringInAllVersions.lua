-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/LoadModuleVersionParityTUnitTests.cs
-- @test: LoadModuleVersionParityTUnitTests.LoadRejectsNonFunctionNonStringInLua51
-- @compat-notes: Lua 5.1's load() only accepts functions, not strings or numbers
load(123)
