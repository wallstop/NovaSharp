-- @lua-versions: 5.1, 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/OsTimeModuleTUnitTests.cs:325
-- @test: OsTimeModuleTUnitTests.DateSupportsOyModifier
-- @compat-notes: Windows Lua crashes when strftime encounters %Oy modifier. NovaSharp handles this gracefully.
return os.date('!%Oy', 0)