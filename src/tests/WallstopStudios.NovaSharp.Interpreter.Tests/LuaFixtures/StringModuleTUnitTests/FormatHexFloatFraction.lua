-- @lua-versions: 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @test: StringModuleTUnitTests.FormatHexFloatFraction
-- @compat-notes: %a format with fractional value
return string.format('%a', 0.5)
