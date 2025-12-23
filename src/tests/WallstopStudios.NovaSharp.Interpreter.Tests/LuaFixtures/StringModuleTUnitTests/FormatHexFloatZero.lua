-- @lua-versions: 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @test: StringModuleTUnitTests.FormatHexFloatZero
-- @compat-notes: %a format with zero
return string.format('%a', 0.0)
