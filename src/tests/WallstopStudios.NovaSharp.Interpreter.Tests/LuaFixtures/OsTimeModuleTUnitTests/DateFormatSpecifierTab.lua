-- @lua-versions: all
-- @novasharp-only: true
-- @expects-error: false
-- @compat-notes: Platform-specific: Windows strftime doesn't support POSIX %t specifier. NovaSharp implements POSIX-compliant behavior.
-- %t should return a tab character
return os.date("!%t", 0) == "\t"
-- Expected: true