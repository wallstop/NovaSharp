-- @lua-versions: 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @test: StringModuleTUnitTests.FormatHexFloatUppercase
-- @compat-notes: %A format with uppercase hex digits
return string.format('%A', 1.0)
