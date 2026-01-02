-- @lua-versions: 5.2+
-- @novasharp-only: false
-- @expects-error: false
-- @test: StringModuleTUnitTests.FormatHexFloatZero
-- @compat-notes: %a format with zero
return string.format('%a', 0.0)
