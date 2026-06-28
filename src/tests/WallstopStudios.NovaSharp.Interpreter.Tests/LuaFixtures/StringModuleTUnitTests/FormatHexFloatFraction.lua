-- @lua-versions: 5.2+
-- @novasharp-only: false
-- @expects-error: false
-- @test: StringModuleTUnitTests.FormatHexFloatFraction
-- %a format with fractional value
return string.format('%a', 0.5)
