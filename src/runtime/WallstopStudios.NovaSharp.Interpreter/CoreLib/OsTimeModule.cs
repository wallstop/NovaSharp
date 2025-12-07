namespace WallstopStudios.NovaSharp.Interpreter.CoreLib
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using Cysharp.Text;
    using WallstopStudios.NovaSharp.Interpreter.DataStructs;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Interpreter.Execution;
    using WallstopStudios.NovaSharp.Interpreter.Infrastructure;
    using WallstopStudios.NovaSharp.Interpreter.Interop.Attributes;
    using WallstopStudios.NovaSharp.Interpreter.Modules;

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

        private static DynValue GetUnixTime(DateTime dateTime, DateTime? epoch = null)
        {
            double time = (dateTime - (epoch ?? Epoch)).TotalSeconds;

            if (time < 0.0)
            {
                return DynValue.Nil;
            }

            return DynValue.NewNumber(time);
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
            DynValue t = GetUnixTime(now, ResolveStartTimeUtc(executionContext));
            if (t.IsNil())
            {
                return DynValue.FromNumber(0);
            }

            return t;
        }

        /// <summary>
        /// Implements Lua `os.difftime`, subtracting two time values (t2 - t1) in seconds (§6.9).
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
            DynValue t2 = args.AsType(0, "difftime", DataType.Number, false);
            DynValue t1 = args.AsType(1, "difftime", DataType.Number, true);

            if (t1.IsNil())
            {
                return DynValue.NewNumber(t2.Number);
            }

            return DynValue.NewNumber(t2.Number - t1.Number);
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

            if (args.Count > 0)
            {
                DynValue vt = args.AsType(0, "time", DataType.Table, true);
                if (vt.Type == DataType.Table)
                {
                    date = ParseTimeTable(vt.Table);
                }
            }

            return GetUnixTime(date);
        }

        private static DateTime ParseTimeTable(Table t)
        {
            int sec = GetTimeTableField(t, "sec") ?? 0;
            int min = GetTimeTableField(t, "min") ?? 0;
            int hour = GetTimeTableField(t, "hour") ?? 12;
            int? day = GetTimeTableField(t, "day");
            int? month = GetTimeTableField(t, "month");
            int? year = GetTimeTableField(t, "year");

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

        private static int? GetTimeTableField(Table t, string key)
        {
            DynValue v = t.Get(key);
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
            DateTime reference = ResolveTimeProvider(executionContext).GetUtcNow().UtcDateTime;

            DynValue vformat = args.AsType(0, "date", DataType.String, true);
            DynValue vtime = args.AsType(1, "date", DataType.Number, true);

            string format = (vformat.IsNil()) ? "%c" : vformat.String;
            bool forceUtc = executionContext.Script?.Options?.ForceUtcDateTime == true;

            if (vtime.IsNotNil())
            {
                reference = FromUnixTime(vtime.Number);
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
                return DynValue.NewString(StrFTime(format, reference));
            }
        }

        private static string StrFTime(string format, DateTime d)
        {
            // ref: http://www.cplusplus.com/reference/ctime/strftime/

            Dictionary<char, string> standardPatterns = new()
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

                if (c == 'O' || c == 'E')
                {
                    continue; // no modifiers
                }

                isEscapeSequence = false;

                if (standardPatterns.TryGetValue(c, out string pattern))
                {
                    sb.Append(d.ToString(pattern, CultureInfo.InvariantCulture));
                }
                else if (c == 'e')
                {
                    string s = d.ToString("%d", CultureInfo.InvariantCulture);
                    if (s.Length < 2)
                    {
                        s = " " + s;
                    }

                    sb.Append(s);
                }
                else if (c == 'n')
                {
                    sb.Append('\n');
                }
                else if (c == 't')
                {
                    sb.Append('\t');
                }
                else if (c == 'C')
                {
                    sb.Append((int)(d.Year / 100));
                }
                else if (c == 'j')
                {
                    sb.Append(d.DayOfYear.ToString("000", CultureInfo.InvariantCulture));
                }
                else if (c == 'u')
                {
                    int weekDay = (int)d.DayOfWeek;
                    if (weekDay == 0)
                    {
                        weekDay = 7;
                    }

                    sb.Append(weekDay);
                }
                else if (c == 'w')
                {
                    int weekDay = (int)d.DayOfWeek;
                    sb.Append(weekDay);
                }
                else if (c == 'U')
                {
                    int week = GetWeekNumberWithFirstDay(d, DayOfWeek.Sunday);
                    sb.Append(week.ToString("00", CultureInfo.InvariantCulture));
                }
                else if (c == 'V')
                {
                    int isoWeek = GetIso8601WeekNumber(d);
                    sb.Append(isoWeek.ToString("00", CultureInfo.InvariantCulture));
                }
                else if (c == 'W')
                {
                    int week = GetWeekNumberWithFirstDay(d, DayOfWeek.Monday);
                    sb.Append(week.ToString("00", CultureInfo.InvariantCulture));
                }
                else
                {
                    throw new ScriptRuntimeException(
                        "bad argument #1 to 'date' (invalid conversion specifier '{0}')",
                        format
                    );
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
