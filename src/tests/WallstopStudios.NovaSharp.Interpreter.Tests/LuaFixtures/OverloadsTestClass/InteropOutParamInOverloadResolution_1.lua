-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/UserDataOverloadsTUnitTests.cs:199
-- @test: OverloadsTestClass.InteropOutParamInOverloadResolution
local dict = DictionaryIntInt.__new(); local res, v = dict.TryGetValue(0)
