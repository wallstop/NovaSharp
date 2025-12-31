-- @lua-versions: 5.1, 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: true
-- @expects-error: false
-- @compat-notes: Platform-specific: Windows strftime doesn't support POSIX %t specifier. NovaSharp implements POSIX-compliant behavior.
-- %t should return a tab character
return os.date("!%t", 0) == "\t"
-- Expected: true