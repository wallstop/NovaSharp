namespace NovaSharp.Interpreter.CoreLib
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using NovaSharp.Interpreter.Compatibility;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Execution;
    using NovaSharp.Interpreter.Interop.Attributes;
    using NovaSharp.Interpreter.Modules;

    /// <summary>
    /// Implements the Lua 5.3+ utf8 library (§6.5).
    /// </summary>
    [NovaSharpModule(Namespace = "utf8")]
    internal static class Utf8Module
    {
        private const string InvalidUtf8CodeMessage = "invalid UTF-8 code";

        [NovaSharpModuleConstant(Name = "charpattern")]
        public const string CharPattern = "[\0-\x7F\xC2-\xF4][\x80-\xBF]*";

        [NovaSharpModuleMethod(Name = "len")]
        public static DynValue Len(ScriptExecutionContext executionContext, CallbackArguments args)
        {
            DynValue value = args.AsType(0, "utf8.len", DataType.String, false);
            DynValue start = args.AsType(1, "utf8.len", DataType.Number, true);
            DynValue end = args.AsType(2, "utf8.len", DataType.Number, true);

            (int startIndex, int endExclusive) = NormalizeRange(value.String, start, end);
            DynValue result = CountRunesOrError(value.String, startIndex, endExclusive);

            return result;
        }

        [NovaSharpModuleMethod(Name = "codepoint")]
        public static DynValue Codepoint(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            DynValue value = args.AsType(0, "utf8.codepoint", DataType.String, false);
            DynValue start = args.AsType(1, "utf8.codepoint", DataType.Number, true);
            DynValue end = args.AsType(2, "utf8.codepoint", DataType.Number, true);

            (int startIndex, int endExclusive) = NormalizeRange(
                value.String,
                start,
                end,
                endDefaultsToStart: true
            );

            List<DynValue> numbers = new();
            int index = startIndex;

            while (index < endExclusive)
            {
                if (
                    !TryDecodeScalarWithinRange(
                        value.String,
                        index,
                        endExclusive,
                        out int codePoint,
                        out int width
                    )
                )
                {
                    throw new ScriptRuntimeException(InvalidUtf8CodeMessage);
                }

                numbers.Add(DynValue.NewNumber(codePoint));
                index += width;
            }

            if (numbers.Count == 0)
            {
                return DynValue.Void;
            }

            return DynValue.NewTuple(numbers.ToArray());
        }

        [NovaSharpModuleMethod(Name = "char")]
        public static DynValue Char(ScriptExecutionContext executionContext, CallbackArguments args)
        {
            StringBuilder builder = new();

            for (int i = 0; i < args.Count; i++)
            {
                int codePoint = args.AsInt(i, "utf8.char");

                if (
                    codePoint < 0
                    || codePoint > 0x10FFFF
                    || (codePoint >= 0xD800 && codePoint <= 0xDFFF)
                )
                {
                    throw new ScriptRuntimeException(
                        $"bad argument #{i + 1} to 'utf8.char' (value out of range)"
                    );
                }

                builder.Append(char.ConvertFromUtf32(codePoint));
            }

            return DynValue.NewString(builder.ToString());
        }

        [NovaSharpModuleMethod(Name = "codes")]
        public static DynValue Codes(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            DynValue value = args.AsType(0, "utf8.codes", DataType.String, false);

            return DynValue.NewTuple(
                DynValue.NewCallback(CodesIterator),
                value,
                DynValue.NewNumber(0)
            );
        }

        [NovaSharpModuleMethod(Name = "offset")]
        public static DynValue Offset(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            DynValue value = args.AsType(0, "utf8.offset", DataType.String, false);
            int n = args.AsInt(1, "utf8.offset");
            DynValue indexArg = args.AsType(2, "utf8.offset", DataType.Number, true);

            if (n == 0)
            {
                int boundary = NormalizeBoundary(
                    value.String,
                    indexArg.IsNil() ? 1 : (int)indexArg.Number
                );
                int containing = FindRuneStartContainingBoundary(value.String, boundary);

                return containing >= 0 ? DynValue.NewNumber(containing + 1) : DynValue.Nil;
            }

            int initial = indexArg.IsNil()
                ? (n > 0 ? 1 : value.String.Length + 1)
                : (int)indexArg.Number;
            int boundaryOffset = NormalizeBoundary(value.String, initial);

            if (n > 0)
            {
                if (!IsRuneBoundary(value.String, boundaryOffset))
                {
                    return DynValue.Nil;
                }

                int index = boundaryOffset;
                int remaining = n;

                while (remaining > 0)
                {
                    if (
                        !TryDecodeScalarWithinRange(
                            value.String,
                            index,
                            value.String.Length,
                            out int _,
                            out int width
                        )
                    )
                    {
                        return DynValue.Nil;
                    }

                    remaining--;

                    if (remaining == 0)
                    {
                        return DynValue.NewNumber(index + 1);
                    }

                    index += width;
                }
            }
            else
            {
                int index = boundaryOffset;
                int remaining = -n;

                while (remaining > 0)
                {
                    index = MoveToPreviousRuneBoundary(value.String, index);

                    if (index < 0)
                    {
                        return DynValue.Nil;
                    }

                    remaining--;

                    if (remaining == 0)
                    {
                        return DynValue.NewNumber(index + 1);
                    }
                }
            }

            return DynValue.Nil;
        }

        private static DynValue CodesIterator(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            DynValue state = args.AsType(0, "utf8.codes", DataType.String, false);
            DynValue control = args.AsType(1, "utf8.codes", DataType.Number, true);

            string value = state.String;
            int index = GetNextIteratorIndex(value, control);

            if (index >= value.Length)
            {
                return DynValue.Nil;
            }

            if (
                !TryDecodeScalarWithinRange(
                    value,
                    index,
                    value.Length,
                    out int codePoint,
                    out int width
                )
            )
            {
                throw new ScriptRuntimeException(InvalidUtf8CodeMessage);
            }

            return DynValue.NewTuple(DynValue.NewNumber(index + 1), DynValue.NewNumber(codePoint));
        }

        private static DynValue CountRunesOrError(string value, int startIndex, int endExclusive)
        {
            int count = 0;
            int index = startIndex;

            while (index < endExclusive)
            {
                if (
                    !TryDecodeScalarWithinRange(
                        value,
                        index,
                        endExclusive,
                        out int _,
                        out int width
                    )
                )
                {
                    return DynValue.NewTuple(DynValue.Nil, DynValue.NewNumber(index + 1));
                }

                count++;
                index += width;
            }

            return DynValue.NewNumber(count);
        }

        private static (int StartIndex, int EndExclusive) NormalizeRange(
            string value,
            DynValue start,
            DynValue end,
            bool endDefaultsToStart = false
        )
        {
            int i = start.IsNil() ? 1 : (int)start.Number;
            int j = end.IsNil() ? (endDefaultsToStart ? i : value.Length) : (int)end.Number;

            if (i < 0)
            {
                i = value.Length + i + 1;
            }

            if (j < 0)
            {
                j = value.Length + j + 1;
            }

            if (i < 1)
            {
                i = 1;
            }

            if (j > value.Length)
            {
                j = value.Length;
            }

            if (i > j)
            {
                return (i - 1, i - 1);
            }

            return (i - 1, j);
        }

        private static bool TryDecodeScalarWithinRange(
            string value,
            int index,
            int limit,
            out int codePoint,
            out int width
        )
        {
            codePoint = 0;
            width = 0;

            if (index < 0 || index >= value.Length || index >= limit)
            {
                return false;
            }

            int remaining = Math.Min(limit - index, value.Length - index);

            if (remaining <= 0)
            {
                return false;
            }

            char current = value[index];

            if (char.IsHighSurrogate(current))
            {
                if (remaining < 2)
                {
                    return false;
                }

                char next = value[index + 1];

                if (!char.IsLowSurrogate(next))
                {
                    return false;
                }

                codePoint = char.ConvertToUtf32(current, next);
                width = 2;
                return true;
            }

            if (char.IsLowSurrogate(current))
            {
                return false;
            }

            codePoint = current;
            width = 1;
            return true;
        }

        private static int GetNextIteratorIndex(string value, DynValue control)
        {
            if (control.IsNil() || control.IsVoid())
            {
                return 0;
            }

            int previousPos = (int)Math.Floor(control.Number);

            if (previousPos <= 0)
            {
                return 0;
            }

            int previousIndex = Math.Min(previousPos - 1, value.Length);

            if (previousIndex >= value.Length)
            {
                return value.Length;
            }

            if (
                !TryDecodeScalarWithinRange(
                    value,
                    previousIndex,
                    value.Length,
                    out int _,
                    out int width
                )
            )
            {
                throw new ScriptRuntimeException(InvalidUtf8CodeMessage);
            }

            return previousIndex + width;
        }

        private static int NormalizeBoundary(string value, int position)
        {
            int length = value.Length;
            int normalized = position;

            if (normalized < 0)
            {
                normalized = length + normalized + 1;
            }

            if (normalized < 1)
            {
                normalized = 1;
            }

            if (normalized > length + 1)
            {
                normalized = length + 1;
            }

            return normalized - 1;
        }

        private static bool IsRuneBoundary(string value, int boundary)
        {
            if (boundary <= 0 || boundary >= value.Length)
            {
                return true;
            }

            return !char.IsLowSurrogate(value[boundary]);
        }

        private static int MoveToPreviousRuneBoundary(string value, int boundary)
        {
            if (boundary <= 0)
            {
                return -1;
            }

            int index = boundary - 1;

            if (index < 0)
            {
                return -1;
            }

            if (char.IsLowSurrogate(value[index]))
            {
                if (index == 0 || !char.IsHighSurrogate(value[index - 1]))
                {
                    return -1;
                }

                index--;
            }

            if (!TryDecodeScalarWithinRange(value, index, value.Length, out int _, out int _))
            {
                return -1;
            }

            return index;
        }

        private static int FindRuneStartContainingBoundary(string value, int boundary)
        {
            if (value.Length == 0)
            {
                return -1;
            }

            if (boundary < 0 || boundary >= value.Length)
            {
                return -1;
            }

            int index = boundary;

            if (index < 0)
            {
                return -1;
            }

            if (char.IsLowSurrogate(value[index]) && index > 0)
            {
                index--;
            }

            if (!TryDecodeScalarWithinRange(value, index, value.Length, out int _, out int _))
            {
                return -1;
            }

            return index;
        }
    }
}
