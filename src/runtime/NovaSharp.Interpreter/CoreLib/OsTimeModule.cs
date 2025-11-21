// Disable warnings about XML documentation
namespace NovaSharp.Interpreter.CoreLib
{
#pragma warning disable 1591

    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Text;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Execution;
    using NovaSharp.Interpreter.Infrastructure;
    using NovaSharp.Interpreter.Interop.Attributes;
    using NovaSharp.Interpreter.Modules;

    /// <summary>
    /// Class implementing time related Lua functions from the 'os' module.
    /// </summary>
    [SuppressMessage(
        "Design",
        "CA1052:Static holder types should be static or not inheritable",
        Justification = "Module types participate in generic registration requiring instance types."
    )]
    [NovaSharpModule(Namespace = "os")]
    public class OsTimeModule
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

        [NovaSharpModuleMethod(Name = "clock")]
        public static DynValue Clock(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            DateTime now = ResolveTimeProvider(executionContext).GetUtcNow().UtcDateTime;
            DynValue t = GetUnixTime(now, ResolveStartTimeUtc(executionContext));
            if (t.IsNil())
            {
                return DynValue.NewNumber(0.0);
            }

            return t;
        }

        [NovaSharpModuleMethod(Name = "difftime")]
        public static DynValue DiffTime(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            DynValue t2 = args.AsType(0, "difftime", DataType.Number, false);
            DynValue t1 = args.AsType(1, "difftime", DataType.Number, true);

            if (t1.IsNil())
            {
                return DynValue.NewNumber(t2.Number);
            }

            return DynValue.NewNumber(t2.Number - t1.Number);
        }

        [NovaSharpModuleMethod(Name = "time")]
        public static DynValue Time(ScriptExecutionContext executionContext, CallbackArguments args)
        {
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

        [NovaSharpModuleMethod(Name = "date")]
        public static DynValue Date(ScriptExecutionContext executionContext, CallbackArguments args)
        {
            DateTime reference = ResolveTimeProvider(executionContext).GetUtcNow().UtcDateTime;

            DynValue vformat = args.AsType(0, "date", DataType.String, true);
            DynValue vtime = args.AsType(1, "date", DataType.Number, true);

            string format = (vformat.IsNil()) ? "%c" : vformat.String;

            if (vtime.IsNotNil())
            {
                reference = FromUnixTime(vtime.Number);
            }

            bool isDst = false;

            if (format.StartsWith("!"))
            {
                format = format.Substring(1);
            }
            else
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
                Table t = new(executionContext.GetScript());

                t.Set("year", DynValue.NewNumber(reference.Year));
                t.Set("month", DynValue.NewNumber(reference.Month));
                t.Set("day", DynValue.NewNumber(reference.Day));
                t.Set("hour", DynValue.NewNumber(reference.Hour));
                t.Set("min", DynValue.NewNumber(reference.Minute));
                t.Set("sec", DynValue.NewNumber(reference.Second));
                t.Set("wday", DynValue.NewNumber(((int)reference.DayOfWeek) + 1));
                t.Set("yday", DynValue.NewNumber(reference.DayOfYear));
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

            StringBuilder sb = new();

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

                if (standardPatterns.ContainsKey(c))
                {
                    sb.Append(d.ToString(standardPatterns[c]));
                }
                else if (c == 'e')
                {
                    string s = d.ToString("%d");
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
                    sb.Append(d.DayOfYear.ToString("000"));
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
                    // Week number with the first Sunday as the first day of week one (00-53)
                    sb.Append("??");
                }
                else if (c == 'V')
                {
                    // ISO 8601 week number (00-53)
                    sb.Append("??");
                }
                else if (c == 'W')
                {
                    // Week number with the first Monday as the first day of week one (00-53)
                    sb.Append("??");
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
    }
}
