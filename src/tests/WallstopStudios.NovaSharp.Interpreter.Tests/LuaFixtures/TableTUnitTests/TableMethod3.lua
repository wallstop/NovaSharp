-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/TableTUnitTests.cs:68
-- @test: TableTUnitTests.TableMethod3
x = 0 a = { value = 1912 } function a.val(self, num) x = self.value + num end
