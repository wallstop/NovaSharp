-- @lua-versions: 5.1, 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: true
-- @expects-error: false
-- @compat-notes: Platform-specific: Windows strftime doesn't support POSIX %e specifier. NovaSharp implements POSIX-compliant behavior.
-- %e should return space-padded day of month
-- January 1 should be " 1" (space-padded to 2 chars)
return os.date("!%e", 0)
-- Expected:  1 (with leading space)