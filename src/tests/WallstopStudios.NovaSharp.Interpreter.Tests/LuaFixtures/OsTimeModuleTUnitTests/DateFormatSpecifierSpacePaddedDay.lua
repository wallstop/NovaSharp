-- @lua-versions: all
-- @novasharp-only: true
-- @expects-error: false
-- Platform-specific: Windows strftime doesn't support POSIX %e specifier. NovaSharp implements POSIX-compliant behavior.
-- %e should return space-padded day of month
-- January 1 should be " 1" (space-padded to 2 chars)
return os.date("!%e", 0)
-- Expected:  1 (with leading space)