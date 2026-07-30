-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/TableTUnitTests.cs:19
-- @test: TableTUnitTests.TableAccessAndEmptyCtor
a = {} a[1] = 1 return a[1]
