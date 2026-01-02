-- @lua-versions: 5.2+
-- @novasharp-only: false
-- @expects-error: false
-- @test: StringModuleTUnitTests.FormatHexFloatUppercase
-- @compat-notes: %A format with uppercase hex digits
return string.format('%A', 1.0)
