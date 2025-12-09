namespace WallstopStudios.NovaSharp.Interpreter.CoreLib
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using Cysharp.Text;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.DataStructs;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Interpreter.Execution;
    using WallstopStudios.NovaSharp.Interpreter.Infrastructure;
    using WallstopStudios.NovaSharp.Interpreter.Modules;
    using WallstopStudios.NovaSharp.Interpreter.Utilities;

    /// <summary>
    /// Class implementing time related Lua functions from the 'os' module.
    /// </summary>
    [NovaSharpModule(Namespace = "os")]
    public static class OsTimeModule
    {
        private static readonly DateTime Epoch = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime GlobalStartTimeUtc = SystemTimeProvider
            .Instance.GetUtcNow()
            .UtcDateTime;

        private static readonly Dictionary<char, string> StandardPatterns = new()
        {
            { 'a', "ddd" },
            { 'A', "dddd" },
            { 'b', "MMM" },
            { 'B', "MMMM" },
            { 'c', "f" },
            { 'd', "dd" },
            { 'D', "MM/dd/yy" },
            { 'F', "yyyy-MM-dd" },
            { 'g', "yy" },
            { 'G', "yyyy" },
            { 'h', "MMM" },
            { 'H', "HH" },
            { 'I', "hh" },
            { 'm', "MM" },
            { 'M', "mm" },
            { 'p', "tt" },
            { 'r', "h:mm:ss tt" },
            { 'R', "HH:mm" },
            { 'S', "ss" },
            { 'T', "HH:mm:ss" },
            { 'y', "yy" },
            { 'Y', "yyyy" },
            { 'x', "d" },
            { 'X', "T" },
            { 'z', "zzz" },
            { 'Z', "zzz" },
        };

        private static DynValue GetUnixTime(DateTime dateTime, DateTime? epoch = null)
        {
            double time = (dateTime - (epoch ?? Epoch)).TotalSeconds;
            // Negative times (before epoch) are valid in Lua
            return DynValue.NewNumber(time);
        }

        /// <summary>
        /// Computes elapsed time since a start point, clamping to 0 for backward time movement.
        /// Used by os.clock() which should never return negative values.
        /// </summary>
        private static DynValue GetElapsedTime(DateTime dateTime, DateTime startTime)
        {
            double time = (dateTime - startTime).TotalSeconds;
            // Elapsed time should never be negative
            return time < 0.0 ? DynValue.NewNumber(0.0) : DynValue.NewNumber(time);
        }

        private static DateTime FromUnixTime(double unixtime)
        {
            TimeSpan ts = TimeSpan.FromSeconds(unixtime);
            return Epoch + ts;
        }

        /// <summary>
        /// Implements Lua `os.clock`, returning CPU time in seconds since the script started (§6.9).
        /// </summary>
        /// <param name="executionContext">Current script execution context.</param>
        /// <param name="args">Unused arguments.</param>
        /// <returns>Elapsed CPU time represented as seconds.</returns>
        [NovaSharpModuleMethod(Name = "clock")]
        public static DynValue Clock(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            executionContext = ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            ModuleArgumentValidation.RequireArguments(args, nameof(args));
            DateTime now = ResolveTimeProvider(executionContext).GetUtcNow().UtcDateTime;
            // os.clock() returns elapsed time since script start, clamped to 0
            return GetElapsedTime(now, ResolveStartTimeUtc(executionContext));
        }

        /// <summary>
        /// Implements Lua `os.difftime`, subtracting two time values (t2 - t1) in seconds (§6.9).
        /// In Lua 5.1/5.2, the second argument is optional (defaults to 0).
        /// In Lua 5.3+, the second argument is required.
        /// </summary>
        /// <param name="executionContext">Current script execution context.</param>
        /// <param name="args">Arguments providing the timestamps.</param>
        /// <returns>Difference in seconds.</returns>
        [NovaSharpModuleMethod(Name = "difftime")]
        public static DynValue DiffTime(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

            LuaCompatibilityVersion version = executionContext.Script.CompatibilityVersion;
            LuaCompatibilityVersion resolvedVersion = LuaVersionDefaults.Resolve(version);

            DynValue t2 = args.AsType(0, "difftime", DataType.Number, false);

            // Lua 5.3+: second argument is required
            // Lua 5.1/5.2: second argument is optional (defaults to implicit 0 behavior)
            bool t1Optional = resolvedVersion < LuaCompatibilityVersion.Lua53;
            DynValue t1 = args.AsType(1, "difftime", DataType.Number, t1Optional);

            // Lua 5.3+: time arguments must have integer representation
            LuaNumberHelpers.ValidateIntegerArgument(version, t2, "difftime", 1);
            LuaNumberHelpers.ValidateIntegerArgument(version, t1, "difftime", 2);

            // Use LuaNumber for proper value extraction
            if (t1.IsNil())
            {
                // Only reachable in Lua 5.1/5.2 mode where t1 is optional
                LuaNumber t2Num = t2.LuaNumber;
                double t2Value = t2Num.IsInteger ? t2Num.AsInteger : t2Num.AsFloat;
                return DynValue.NewNumber(t2Value);
            }

            LuaNumber t2NumVal = t2.LuaNumber;
            LuaNumber t1NumVal = t1.LuaNumber;
            double t2Val = t2NumVal.IsInteger ? t2NumVal.AsInteger : t2NumVal.AsFloat;
            double t1Val = t1NumVal.IsInteger ? t1NumVal.AsInteger : t1NumVal.AsFloat;
            return DynValue.NewNumber(t2Val - t1Val);
        }

        /// <summary>
        /// Implements Lua `os.time`, returning the current time as Unix seconds or building one from
        /// a table description (§6.9).
        /// </summary>
        /// <param name="executionContext">Current script execution context.</param>
        /// <param name="args">Optional date table argument.</param>
        /// <returns>Unix timestamp as a number.</returns>
        [NovaSharpModuleMethod(Name = "time")]
        public static DynValue Time(ScriptExecutionContext executionContext, CallbackArguments args)
        {
            executionContext = ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));
            DateTime date = ResolveTimeProvider(executionContext).GetUtcNow().UtcDateTime;

            LuaCompatibilityVersion version = executionContext.Script.CompatibilityVersion;

            if (args.Count > 0)
            {
                DynValue vt = args.AsType(0, "time", DataType.Table, true);
                if (vt.Type == DataType.Table)
                {
                    date = ParseTimeTable(vt.Table, version);
                }
            }

            return GetUnixTime(date);
        }

        private static DateTime ParseTimeTable(Table t, LuaCompatibilityVersion version)
        {
            int sec = GetTimeTableField(t, "sec", version) ?? 0;
            int min = GetTimeTableField(t, "min", version) ?? 0;
            int hour = GetTimeTableField(t, "hour", version) ?? 12;
            int? day = GetTimeTableField(t, "day", version);
            int? month = GetTimeTableField(t, "month", version);
            int? year = GetTimeTableField(t, "year", version);

            if (day == null)
            {
                throw new ScriptRuntimeException("field 'day' missing in date table");
            }

            if (month == null)
            {
                throw new ScriptRuntimeException("field 'month' missing in date table");
            }

            if (year == null)
            {
                throw new ScriptRuntimeException("field 'year' missing in date table");
            }

            return new DateTime(year.Value, month.Value, day.Value, hour, min, sec);
        }

        private static int? GetTimeTableField(Table t, string key, LuaCompatibilityVersion version)
        {
            DynValue v = t.Get(key);

            // In Lua 5.3+, fields must be integers (not strings or other types)
            LuaCompatibilityVersion resolvedVersion = LuaVersionDefaults.Resolve(version);
            if (resolvedVersion >= LuaCompatibilityVersion.Lua53)
            {
                if (v.IsNil())
                {
                    return null;
                }

                if (v.Type != DataType.Number)
                {
                    throw new ScriptRuntimeException($"field '{key}' is not an integer");
                }

                // Check if it's a valid integer
                LuaNumber num = v.LuaNumber;
                if (num.IsInteger)
                {
                    return (int)num.AsInteger;
                }

                // Float with integer value is OK
                double floatVal = num.AsFloat;
                double floored = Math.Floor(floatVal);
                if (floored == floatVal && !double.IsNaN(floatVal) && !double.IsInfinity(floatVal))
                {
                    return (int)floored;
                }

                throw new ScriptRuntimeException($"field '{key}' is not an integer");
            }

            // Lua 5.1/5.2: use CastToNumber which accepts strings
            double? d = v.CastToNumber();

            if (d.HasValue)
            {
                return (int)d.Value;
            }

            return null;
        }

        /// <summary>
        /// Implements Lua `os.date`, formatting the current time (or a supplied timestamp) according
        /// to the requested format string (§6.9).
        /// </summary>
        /// <param name="executionContext">Current script execution context.</param>
        /// <param name="args">
        /// Format string (defaults to <c>%c</c>) and optional Unix timestamp. Supports the special
        /// `*t` structure return and `!` UTC prefix.
        /// </param>
        /// <returns>Formatted string or table describing the broken-down date.</returns>
        [NovaSharpModuleMethod(Name = "date")]
        public static DynValue Date(ScriptExecutionContext executionContext, CallbackArguments args)
        {
            executionContext = ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

            LuaCompatibilityVersion version = executionContext.Script.CompatibilityVersion;

            DateTime reference = ResolveTimeProvider(executionContext).GetUtcNow().UtcDateTime;

            DynValue vformat = args.AsType(0, "date", DataType.String, true);
            DynValue vtime = args.AsType(1, "date", DataType.Number, true);

            // Lua 5.3+: time argument must have integer representation
            LuaNumberHelpers.ValidateIntegerArgument(version, vtime, "date", 2);

            string format = (vformat.IsNil()) ? "%c" : vformat.String;
            bool forceUtc = executionContext.Script?.Options?.ForceUtcDateTime == true;

            if (vtime.IsNotNil())
            {
                // Use LuaNumber for proper value extraction
                LuaNumber timeNum = vtime.LuaNumber;
                double timeValue = timeNum.IsInteger ? timeNum.AsInteger : timeNum.AsFloat;
                reference = FromUnixTime(timeValue);
            }

            bool isDst = false;

            if (!string.IsNullOrEmpty(format) && format[0] == '!')
            {
                format = format.Substring(1);
            }
            else if (!forceUtc)
            {
#if !(PCL || ENABLE_DOTNET || NETFX_CORE)

                try
                {
                    reference = TimeZoneInfo.ConvertTimeFromUtc(reference, TimeZoneInfo.Local);
                    isDst = reference.IsDaylightSavingTime();
                }
                catch (TimeZoneNotFoundException)
                {
                    // this catches a weird mono bug: https://bugzilla.xamarin.com/show_bug.cgi?id=11817
                    // however the behavior is definitely not correct. damn.
                }
#endif
            }

            if (format == "*t")
            {
                Table t = new(executionContext.Script);

                t.Set("year", DynValue.FromNumber(reference.Year));
                t.Set("month", DynValue.FromNumber(reference.Month));
                t.Set("day", DynValue.FromNumber(reference.Day));
                t.Set("hour", DynValue.FromNumber(reference.Hour));
                t.Set("min", DynValue.FromNumber(reference.Minute));
                t.Set("sec", DynValue.FromNumber(reference.Second));
                t.Set("wday", DynValue.FromNumber(((int)reference.DayOfWeek) + 1));
                t.Set("yday", DynValue.FromNumber(reference.DayOfYear));
                t.Set("isdst", DynValue.NewBoolean(isDst));

                return DynValue.NewTable(t);
            }
            else
            {
                LuaCompatibilityVersion resolvedVersion = LuaVersionDefaults.Resolve(version);
                return DynValue.NewString(StrFTime(format, reference, resolvedVersion));
            }
        }

        private static string StrFTime(string format, DateTime d, LuaCompatibilityVersion version)
        {
            // ref: http://www.cplusplus.com/reference/ctime/strftime/

            using Utf16ValueStringBuilder sb = ZStringBuilder.Create();

            bool isEscapeSequence = false;

            for (int i = 0; i < format.Length; i++)
            {
                char c = format[i];

                if (c == '%')
                {
                    if (isEscapeSequence)
                    {
                        sb.Append('%');
                        isEscapeSequence = false;
                    }
                    else
                    {
                        isEscapeSequence = true;
                    }

                    continue;
                }

                if (!isEscapeSequence)
                {
                    sb.Append(c);
                    continue;
                }

                if (c is 'O' or 'E')
                {
                    continue; // no modifiers
                }

                isEscapeSequence = false;

                if (StandardPatterns.TryGetValue(c, out string pattern))
                {
                    sb.Append(d.ToString(pattern, CultureInfo.InvariantCulture));
                }
                else
                {
                    switch (c)
                    {
                        case 'e':
                        {
                            string s = d.ToString("%d", CultureInfo.InvariantCulture);
                            if (s.Length < 2)
                            {
                                s = " " + s;
                            }

                            sb.Append(s);
                            break;
                        }
                        case 'n':
                            sb.Append('\n');
                            break;
                        case 't':
                            sb.Append('\t');
                            break;
                        case 'C':
                            sb.Append((int)(d.Year / 100));
                            break;
                        case 'j':
                            sb.Append(d.DayOfYear.ToString("000", CultureInfo.InvariantCulture));
                            break;
                        case 'u':
                        {
                            int weekDay = (int)d.DayOfWeek;
                            if (weekDay == 0)
                            {
                                weekDay = 7;
                            }

                            sb.Append(weekDay);
                            break;
                        }
                        case 'w':
                        {
                            int weekDay = (int)d.DayOfWeek;
                            sb.Append(weekDay);
                            break;
                        }
                        case 'U':
                        {
                            int week = GetWeekNumberWithFirstDay(d, DayOfWeek.Sunday);
                            sb.Append(week.ToString("00", CultureInfo.InvariantCulture));
                            break;
                        }
                        case 'V':
                        {
                            int isoWeek = GetIso8601WeekNumber(d);
                            sb.Append(isoWeek.ToString("00", CultureInfo.InvariantCulture));
                            break;
                        }
                        case 'W':
                        {
                            int week = GetWeekNumberWithFirstDay(d, DayOfWeek.Monday);
                            sb.Append(week.ToString("00", CultureInfo.InvariantCulture));
                            break;
                        }
                        default:
                        {
                            // Version-specific handling for unknown conversion specifiers:
                            // Lua 5.1: Returns the literal specifier (e.g., "%Q" -> "%Q")
                            // Lua 5.2+: Throws "bad argument #1 to 'date' (invalid conversion specifier '%Qa')"
                            // Lua includes trailing characters after the invalid specifier for context
                            if (version == LuaCompatibilityVersion.Lua51)
                            {
                                sb.Append('%');
                                sb.Append(c);
                            }
                            else
                            {
                                // Build the invalid specifier string for the error message,
                                // including trailing characters as Lua does
                                Utf16ValueStringBuilder specBuilder = ZString.CreateStringBuilder();
                                specBuilder.Append(c);
                                // Include up to one trailing character for context (like Lua does)
                                // Note: i points to current character c, so we need i+1 for the next character
                                if (i + 1 < format.Length)
                                {
                                    specBuilder.Append(format[i + 1]);
                                }
                                string invalidSpec = specBuilder.ToString();
                                specBuilder.Dispose();

                                throw new ScriptRuntimeException(
                                    "bad argument #1 to 'date' (invalid conversion specifier '%{0}')",
                                    invalidSpec
                                );
                            }

                            break;
                        }
                    }
                }
            }

            return sb.ToString();
        }

        private static ITimeProvider ResolveTimeProvider(ScriptExecutionContext context)
        {
            return context?.OwnerScript?.TimeProvider ?? SystemTimeProvider.Instance;
        }

        private static DateTime ResolveStartTimeUtc(ScriptExecutionContext context) =>
            context?.OwnerScript?.StartTimeUtc ?? GlobalStartTimeUtc;

        private static int GetWeekNumberWithFirstDay(DateTime dateTime, DayOfWeek firstDayOfWeek)
        {
            DateTime date = dateTime.Date;
            DateTime jan1 = new(date.Year, 1, 1);
            int offset = ((int)firstDayOfWeek - (int)jan1.DayOfWeek + 7) % 7;
            DateTime firstWeekStart = jan1.AddDays(offset);

            if (firstWeekStart > date)
            {
                return 0;
            }

            double totalDays = (date - firstWeekStart).TotalDays;
            int week = (int)(totalDays / 7) + 1;
            return Math.Min(Math.Max(week, 0), 53);
        }

        private static int GetIso8601WeekNumber(DateTime dateTime)
        {
            DateTime date = dateTime.Date;
            Calendar calendar = CultureInfo.InvariantCulture.Calendar;
            DayOfWeek day = calendar.GetDayOfWeek(date);

            if (day >= DayOfWeek.Monday && day <= DayOfWeek.Wednesday)
            {
                date = date.AddDays(3);
            }

            return calendar.GetWeekOfYear(
                date,
                CalendarWeekRule.FirstFourDayWeek,
                DayOfWeek.Monday
            );
        }
    }
}
