namespace NovaSharp.Interpreter.CoreLib.IO
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Text;
    using NovaSharp.Interpreter.Compatibility;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Execution;
    using NovaSharp.Interpreter.Interop.Attributes;

    /// <summary>
    /// Abstract class implementing a file Lua userdata. Methods are meant to be called by Lua code.
    /// </summary>
    internal abstract class FileUserDataBase : RefIdObject
    {
        public DynValue Lines(ScriptExecutionContext executionContext, CallbackArguments args)
        {
            List<DynValue> readLines = new();

            DynValue readValue = null;

            do
            {
                readValue = Read(executionContext, args);
                readLines.Add(readValue);
            } while (readValue.IsNotNil());

            return DynValue.FromObject(executionContext.GetScript(), readLines.Select(s => s));
        }

        public DynValue Read(ScriptExecutionContext executionContext, CallbackArguments args)
        {
            if (args.Count == 0)
            {
                string str = ReadLine();

                if (str == null)
                {
                    return DynValue.Nil;
                }

                str = str.TrimEnd('\n', '\r');
                return DynValue.NewString(str);
            }
            else
            {
                List<DynValue> rets = new();

                for (int i = 0; i < args.Count; i++)
                {
                    DynValue v;

                    if (args[i].Type == DataType.Number)
                    {
                        if (Eof())
                        {
                            return DynValue.Nil;
                        }

                        int howmany = (int)args[i].Number;

                        string str = ReadBuffer(howmany);
                        v = DynValue.NewString(str);
                    }
                    else
                    {
                        string opt = args.AsType(i, "read", DataType.String, false).String;

                        if (Eof())
                        {
                            v = opt.StartsWith("*a") ? DynValue.NewString("") : DynValue.Nil;
                        }
                        else if (opt.StartsWith("*n"))
                        {
                            double? d = ReadNumber();

                            if (d.HasValue)
                            {
                                v = DynValue.NewNumber(d.Value);
                            }
                            else
                            {
                                v = DynValue.Nil;
                            }
                        }
                        else if (opt.StartsWith("*a"))
                        {
                            string str = ReadToEnd();
                            v = DynValue.NewString(str);
                        }
                        else if (opt.StartsWith("*l"))
                        {
                            string str = ReadLine();
                            str = str.TrimEnd('\n', '\r');
                            v = DynValue.NewString(str);
                        }
                        else if (opt.StartsWith("*L"))
                        {
                            string str = ReadLine();

                            str = str.TrimEnd('\n', '\r');
                            str += "\n";

                            v = DynValue.NewString(str);
                        }
                        else
                        {
                            throw ScriptRuntimeException.BadArgument(i, "read", "invalid option");
                        }
                    }

                    rets.Add(v);
                }

                return DynValue.NewTuple(rets.ToArray());
            }
        }

        public DynValue Write(ScriptExecutionContext executionContext, CallbackArguments args)
        {
            try
            {
                for (int i = 0; i < args.Count; i++)
                {
                    //string str = args.AsStringUsingMeta(executionContext, i, "file:write");
                    string str = args.AsType(i, "write", DataType.String, false).String;
                    Write(str);
                }

                return UserData.Create(this);
            }
            catch (ScriptRuntimeException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return DynValue.NewTuple(
                    DynValue.Nil,
                    DynValue.NewString(ex.Message),
                    DynValue.NewNumber(-1)
                );
            }
        }

        public DynValue Close(ScriptExecutionContext executionContext, CallbackArguments args)
        {
            try
            {
                string msg = Close();
                if (msg == null)
                {
                    return DynValue.True;
                }
                else
                {
                    return DynValue.NewTuple(
                        DynValue.Nil,
                        DynValue.NewString(msg),
                        DynValue.NewNumber(-1)
                    );
                }
            }
            catch (ScriptRuntimeException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return DynValue.NewTuple(
                    DynValue.Nil,
                    DynValue.NewString(ex.Message),
                    DynValue.NewNumber(-1)
                );
            }
        }

        private double? ReadNumber()
        {
            bool canRewind = SupportsRewind;
            long startPosition = canRewind ? GetCurrentPosition() : 0;
            long lastConsumedPosition = startPosition;

            StringBuilder numberBuilder = new();
            bool hasDigits = false;
            bool hasDecimalPoint = false;
            bool exponentSeen = false;
            bool exponentHasDigits = false;

            ConsumeLeadingWhitespace(canRewind, ref lastConsumedPosition);

            while (true)
            {
                int peekValue = PeekRaw();
                if (peekValue == -1)
                {
                    break;
                }

                char current = (char)peekValue;

                if (char.IsDigit(current))
                {
                    if (!TryConsumeChar(ref lastConsumedPosition, canRewind, numberBuilder))
                    {
                        break;
                    }

                    hasDigits = true;

                    if (exponentSeen)
                    {
                        exponentHasDigits = true;
                    }

                    continue;
                }

                if (IsSignAllowed(numberBuilder, exponentSeen, exponentHasDigits, current))
                {
                    if (!TryConsumeChar(ref lastConsumedPosition, canRewind, numberBuilder))
                    {
                        break;
                    }

                    continue;
                }

                if (current == '.' && !hasDecimalPoint && !exponentSeen)
                {
                    if (!TryConsumeChar(ref lastConsumedPosition, canRewind, numberBuilder))
                    {
                        break;
                    }

                    hasDecimalPoint = true;
                    continue;
                }

                if ((current == 'e' || current == 'E') && !exponentSeen && hasDigits)
                {
                    if (!TryConsumeChar(ref lastConsumedPosition, canRewind, numberBuilder))
                    {
                        break;
                    }

                    exponentSeen = true;
                    exponentHasDigits = false;
                    continue;
                }

                break;
            }

            string numericText = numberBuilder.ToString();

            bool isParsable =
                numericText.Length > 0
                && !IsStandaloneSignOrDot(numericText)
                && !(exponentSeen && !exponentHasDigits);

            if (!isParsable)
            {
                if (canRewind)
                {
                    ResetToPosition(startPosition);
                }

                return null;
            }

            if (
                !double.TryParse(
                    numericText,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out double parsedValue
                )
            )
            {
                if (canRewind)
                {
                    ResetToPosition(startPosition);
                }

                return null;
            }

            if (canRewind)
            {
                ResetToPosition(lastConsumedPosition);
            }

            return parsedValue;
        }

        private void ConsumeLeadingWhitespace(bool canRewind, ref long lastConsumedPosition)
        {
            while (true)
            {
                int peekValue = PeekRaw();
                if (peekValue == -1)
                {
                    break;
                }

                char peekChar = (char)peekValue;
                if (!char.IsWhiteSpace(peekChar))
                {
                    break;
                }

                string chunk = ReadBuffer(1);
                if (chunk.Length == 0)
                {
                    break;
                }

                if (canRewind)
                {
                    lastConsumedPosition = GetCurrentPosition();
                }
            }
        }

        private bool TryConsumeChar(
            ref long lastConsumedPosition,
            bool canRewind,
            StringBuilder numberBuilder
        )
        {
            string chunk = ReadBuffer(1);
            if (chunk.Length == 0)
            {
                return false;
            }

            numberBuilder.Append(chunk);

            if (canRewind)
            {
                lastConsumedPosition = GetCurrentPosition();
            }

            return true;
        }

        private static bool IsSignAllowed(
            StringBuilder numberBuilder,
            bool exponentSeen,
            bool exponentHasDigits,
            char candidate
        )
        {
            if (candidate != '+' && candidate != '-')
            {
                return false;
            }

            if (numberBuilder.Length == 0)
            {
                return true;
            }

            if (
                exponentSeen
                && !exponentHasDigits
                && numberBuilder.Length > 0
                && (numberBuilder[^1] == 'e' || numberBuilder[^1] == 'E')
            )
            {
                return true;
            }

            return false;
        }

        private static bool IsStandaloneSignOrDot(string numericText)
        {
            if (numericText.Length != 1)
            {
                return false;
            }

            char single = numericText[0];
            return single == '+' || single == '-' || single == '.';
        }

        protected abstract bool Eof();
        protected abstract string ReadLine();
        protected abstract string ReadBuffer(int p);
        protected abstract string ReadToEnd();
        protected abstract char Peek();
        protected abstract int PeekRaw();
        protected abstract void Write(string value);
        protected abstract bool SupportsRewind { get; }
        protected abstract long GetCurrentPosition();
        protected abstract void ResetToPosition(long position);

        protected internal abstract bool IsOpen();
        protected abstract string Close();

        public abstract bool Flush();
        public abstract long Seek(string whence, long offset = 0);
        public abstract bool Setvbuf(string mode);

        public override string ToString()
        {
            if (IsOpen())
            {
                return $"file ({ReferenceId:X8})";
            }
            else
            {
                return "file (closed)";
            }
        }
    }
}
