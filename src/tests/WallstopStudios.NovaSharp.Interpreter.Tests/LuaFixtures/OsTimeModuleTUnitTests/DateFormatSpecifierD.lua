-- @lua-versions: all
-- @novasharp-only: true
-- @expects-error: false
-- @compat-notes: Platform-specific: Windows strftime doesn't support POSIX %D specifier. NovaSharp implements POSIX-compliant behavior.
-- %D should return MM/DD/YY format
-- Epoch timestamp 0 is Thursday, January 1, 1970
return os.date("!%D", 0)
-- Expected: 01/01/70