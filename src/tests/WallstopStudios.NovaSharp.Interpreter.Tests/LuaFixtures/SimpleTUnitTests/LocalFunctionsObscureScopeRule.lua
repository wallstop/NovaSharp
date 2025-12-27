-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/SimpleTUnitTests.cs:1100
-- @test: SimpleTUnitTests.LocalFunctionsObscureScopeRule
local function fact()
					return fact;
				end

				return fact();
