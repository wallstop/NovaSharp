-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/TableTUnitTests.cs:80
-- @test: TableTUnitTests.TableMethod4
x = 0 local a = { value = 1912 } function a:val(num) x = self.value + num end
