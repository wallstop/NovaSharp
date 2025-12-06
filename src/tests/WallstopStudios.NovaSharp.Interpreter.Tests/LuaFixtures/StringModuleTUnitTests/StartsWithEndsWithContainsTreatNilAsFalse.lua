-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:305
-- @test: StringModuleTUnitTests.StartsWithEndsWithContainsTreatNilAsFalse
return string.startswith(nil, 'prefix'),
                       string.endswith('suffix', nil),
                       string.contains(nil, nil)
