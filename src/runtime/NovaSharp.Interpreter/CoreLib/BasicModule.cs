namespace NovaSharp.Interpreter.CoreLib
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Text;
    using Debugging;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.Compatibility;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Execution;
    using NovaSharp.Interpreter.Interop.Attributes;
    using NovaSharp.Interpreter.Modules;
    using NovaSharp.Interpreter.Utilities;

    /// <summary>
    /// Class implementing basic Lua functions (print, type, tostring, etc) as a NovaSharp module.
    /// </summary>
    [NovaSharpModule]
    public static class BasicModule
    {
        /// <summary>
        /// Implements Lua's <c>type</c> function (§6.1), returning the textual Lua type name for the first argument.
        /// </summary>
        /// <param name="executionContext">
        /// Execution context supplied by the runtime (unused but required by the module contract).
        /// </param>
        /// <param name="args">Arguments passed to <c>type</c>; the first entry is inspected.</param>
        /// <returns>
        /// A string <see cref="DynValue"/> representing the Lua type name (e.g., <c>"nil"</c>, <c>"table"</c>, <c>"function"</c>).
        /// </returns>
        [NovaSharpModuleMethod(Name = "type")]
        public static DynValue Type(ScriptExecutionContext executionContext, CallbackArguments args)
        {
            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            if (args.Count < 1)
            {
                throw ScriptRuntimeException.BadArgumentValueExpected(0, "type");
            }

            DynValue v = args[0];
            return DynValue.NewString(v.Type.ToLuaTypeString());
        }

        /// <summary>
        /// Implements Lua's <c>assert</c> helper (§6.1) by throwing when the first argument is falsy.
        /// </summary>
        /// <param name="executionContext">Execution context used for diagnostics.</param>
        /// <param name="args">
        /// Arguments passed to <c>assert</c>; index <c>0</c> is the test value and index <c>1</c> is the optional error message.
        /// </param>
        /// <returns>The original argument tuple when the assertion succeeds.</returns>
        /// <exception cref="ScriptRuntimeException">Thrown when the assertion fails.</exception>
        [NovaSharpModuleMethod(Name = "assert")]
        public static DynValue Assert(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            if (executionContext == null)
            {
                throw new ArgumentNullException(nameof(executionContext));
            }

            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            DynValue v = args[0];
            DynValue message = args[1];

            if (!v.CastToBool())
            {
                if (message.IsNil())
                {
                    throw new ScriptRuntimeException("assertion failed!"); // { DoNotDecorateMessage = true };
                }
                else
                {
                    throw new ScriptRuntimeException(message.ToPrintString()); // { DoNotDecorateMessage = true };
                }
            }

            return DynValue.NewTupleNested(args.GetArray());
        }

        /// <summary>
        /// Implements Lua's <c>collectgarbage</c> helper (§6.1) by forwarding the supported modes to the CLR GC.
        /// </summary>
        /// <param name="executionContext">Execution context supplied by the runtime.</param>
        /// <param name="args">Arguments describing the requested mode (nil/<c>"collect"</c>/<c>"restart"</c> trigger a GC).</param>
        /// <returns><see cref="DynValue.Nil"/> to match Lua's API surface.</returns>
        [NovaSharpModuleMethod(Name = "collectgarbage")]
        public static DynValue CollectGarbage(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            DynValue opt = args[0];

            string mode = opt.CastToString();

            if (mode == null || mode == "collect" || mode == "restart")
            {
#if PCL || ENABLE_DOTNET
                GC.Collect();
#else
                GC.Collect(2, GCCollectionMode.Forced);
#endif
            }

            return DynValue.Nil;
        }

        /// <summary>
        /// Implements Lua's <c>error</c> function (§6.1), raising a <see cref="ScriptRuntimeException"/> with the optional
        /// stack-level adjustment requested by the caller.
        /// </summary>
        /// <param name="executionContext">Execution context used to resolve coroutines and call frames for decoration.</param>
        /// <param name="args">
        /// Argument zero contains the error message; argument one optionally supplies the stack level used during decoration.
        /// </param>
        /// <returns>This method never returns because it always throws.</returns>
        /// <exception cref="ScriptRuntimeException">Always thrown to surface the Lua-visible error.</exception>
        [NovaSharpModuleMethod(Name = "error")]
        public static DynValue Error(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            executionContext = ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

            DynValue message = args.AsType(0, "error", DataType.String, false);
            DynValue level = args.AsType(1, "error", DataType.Number, true);

            Coroutine cor = executionContext.CallingCoroutine;

            WatchItem[] stacktrace = cor.GetStackTrace(0, executionContext.CallingLocation);

            ScriptRuntimeException e = new(message.String);

            if (level.IsNil())
            {
                level = DynValue.NewNumber(1); // Default
            }

            if (level.Number > 0 && level.Number < stacktrace.Length)
            {
                // Lua allows levels up to max. value of a double, while this has to be cast to int
                // Probably never will be a problem, just leaving this note here
                WatchItem wi = stacktrace[(int)level.Number];

                e.DecorateMessage(executionContext.Script, wi.Location);
            }
            else
            {
                e.DoNotDecorateMessage = true;
            }

            throw e;
        }

        /// <summary>
        /// Implements Lua's <c>tostring</c> helper (§6.1) by formatting values or invoking the <c>__tostring</c> metamethod.
        /// </summary>
        /// <param name="executionContext">Execution context used to resolve metamethod tail calls.</param>
        /// <param name="args">Arguments passed to <c>tostring</c>; the first value is converted to a Lua string.</param>
        /// <returns>A string representation of the supplied value.</returns>
        [NovaSharpModuleMethod(Name = "tostring")]
        public static DynValue ToString(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            if (executionContext == null)
            {
                throw new ArgumentNullException(nameof(executionContext));
            }

            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            if (args.Count < 1)
            {
                throw ScriptRuntimeException.BadArgumentValueExpected(0, "tostring");
            }

            DynValue v = args[0];
            DynValue tail = executionContext.GetMetamethodTailCall(v, "__tostring", v);

            if (tail == null || tail.IsNil())
            {
                return DynValue.NewString(v.ToPrintString());
            }

            tail.TailCallData.Continuation = new CallbackFunction(
                ToStringContinuation,
                "__tostring"
            );

            return tail;
        }

        /// <summary>
        /// Continuation that validates the result of a <c>__tostring</c> metamethod before returning it to Lua.
        /// </summary>
        /// <param name="executionContext">Execution context driving the metamethod invocation.</param>
        /// <param name="args">Arguments flowing out of the metamethod call.</param>
        /// <returns>The validated string result.</returns>
        internal static DynValue ToStringContinuation(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            DynValue b = args[0].ToScalar();

            if (b.IsNil())
            {
                return b;
            }

            if (b.Type != DataType.String)
            {
                throw new ScriptRuntimeException("'tostring' must return a string");
            }

            return b;
        }

        /// <summary>
        /// Implements Lua's <c>select</c> helper (§6.1), returning either the argument count or a slice of the varargs.
        /// </summary>
        /// <param name="executionContext">Execution context supplied by the runtime.</param>
        /// <param name="args">
        /// Arguments passed to <c>select</c>; index zero is the selector (<c>"#"</c> or a numeric offset), followed by the tuple.
        /// </param>
        /// <returns>A tuple containing the requested slice or a number describing the argument count.</returns>
        [NovaSharpModuleMethod(Name = "select")]
        public static DynValue Select(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

            if (args[0].Type == DataType.String && args[0].String == "#")
            {
                if (args[^1].Type == DataType.Tuple)
                {
                    return DynValue.NewNumber(args.Count - 1 + args[^1].Tuple.Length);
                }
                else
                {
                    return DynValue.NewNumber(args.Count - 1);
                }
            }

            DynValue vNum = args.AsType(0, "select", DataType.Number, false);
            int num = (int)vNum.Number;

            List<DynValue> values = new();

            if (num > 0)
            {
                for (int i = num; i < args.Count; i++)
                {
                    values.Add(args[i]);
                }
            }
            else if (num < 0)
            {
                num = args.Count + num;

                if (num < 1)
                {
                    throw ScriptRuntimeException.BadArgumentIndexOutOfRange("select", 0);
                }

                for (int i = num; i < args.Count; i++)
                {
                    values.Add(args[i]);
                }
            }
            else
            {
                throw ScriptRuntimeException.BadArgumentIndexOutOfRange("select", 0);
            }

            return DynValue.NewTupleNested(values.ToArray());
        }

        /// <summary>
        /// Implements Lua's <c>tonumber</c> helper (§6.1), converting values to doubles with optional radix parsing.
        /// </summary>
        /// <param name="executionContext">Execution context used for diagnostics.</param>
        /// <param name="args">
        /// Arguments describing the value to convert (index zero) and the optional numeric base (index one, 2-36).
        /// </param>
        /// <returns>
        /// A numeric <see cref="DynValue"/> when conversion succeeds; otherwise <see cref="DynValue.Nil"/>.
        /// </returns>
        [NovaSharpModuleMethod(Name = "tonumber")]
        public static DynValue ToNumber(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            executionContext = ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

            if (args.Count < 1)
            {
                throw ScriptRuntimeException.BadArgumentValueExpected(0, "tonumber");
            }

            DynValue e = args[0];
            DynValue b = args.AsType(1, "tonumber", DataType.Number, true);

            if (b.IsNil())
            {
                if (e.Type == DataType.Number)
                {
                    return e;
                }

                if (e.Type != DataType.String)
                {
                    return DynValue.Nil;
                }

                if (
                    double.TryParse(
                        e.String,
                        NumberStyles.Any,
                        CultureInfo.InvariantCulture,
                        out double d
                    )
                )
                {
                    return DynValue.NewNumber(d);
                }
                return DynValue.Nil;
            }
            else
            {
                DynValue numeral =
                    args[0].Type != DataType.Number
                        ? args.AsType(0, "tonumber", DataType.String, false)
                        : DynValue.NewString(args[0].Number.ToString(CultureInfo.InvariantCulture));

                double baseValue = b.Number;
                if (double.IsNaN(baseValue) || double.IsInfinity(baseValue))
                {
                    throw ScriptRuntimeException.BadArgument(
                        1,
                        "tonumber",
                        "integer",
                        "number",
                        false
                    );
                }

                if (Math.Truncate(baseValue) != baseValue)
                {
                    throw ScriptRuntimeException.BadArgument(
                        1,
                        "tonumber",
                        "integer",
                        "number",
                        false
                    );
                }

                int bb = (int)baseValue;

                if (bb < 2 || bb > 36)
                {
                    throw new ScriptRuntimeException(
                        "bad argument #2 to 'tonumber' (base out of range)"
                    );
                }

                ReadOnlySpan<char> numeralSpan = numeral.String.AsSpan().TrimWhitespace();

                if (numeralSpan.IsEmpty)
                {
                    return DynValue.Nil;
                }

                if (TryParseIntegerInBase(numeralSpan, bb, out double parsedValue))
                {
                    return DynValue.NewNumber(parsedValue);
                }

                return DynValue.Nil;
            }
        }

        private static bool TryParseIntegerInBase(
            ReadOnlySpan<char> text,
            int numberBase,
            out double value
        )
        {
            value = 0;
            ReadOnlySpan<char> span = text.TrimWhitespace();
            if (span.IsEmpty)
            {
                return false;
            }

            int index = 0;
            bool negative = false;

            if (span[index] == '+' || span[index] == '-')
            {
                negative = span[index] == '-';
                index++;
            }

            if (index >= span.Length)
            {
                return false;
            }

            double accumulator = 0;
            for (; index < span.Length; index++)
            {
                int digit = GetDigitValue(span[index]);

                if (digit < 0 || digit >= numberBase)
                {
                    return false;
                }

                accumulator = (accumulator * numberBase) + digit;
            }

            value = negative ? -accumulator : accumulator;
            return true;
        }

        private static int GetDigitValue(char candidate)
        {
            if (candidate >= '0' && candidate <= '9')
            {
                return candidate - '0';
            }

            if (candidate >= 'A' && candidate <= 'Z')
            {
                return candidate - 'A' + 10;
            }

            if (candidate >= 'a' && candidate <= 'z')
            {
                return candidate - 'a' + 10;
            }

            return -1;
        }

        /// <summary>
        /// Implements Lua's <c>print</c> function (§6.1) by formatting the arguments with tabs and forwarding them to
        /// the host-provided debug sink.
        /// </summary>
        /// <param name="executionContext">Current execution context, used to resolve the script's debug printer.</param>
        /// <param name="args">Arguments to format and print.</param>
        /// <returns><see cref="DynValue.Nil"/>, matching Lua's return contract.</returns>
        [NovaSharpModuleMethod(Name = "print")]
        public static DynValue Print(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            executionContext = ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

            StringBuilder sb = new();

            for (int i = 0; i < args.Count; i++)
            {
                if (args[i].IsVoid())
                {
                    break;
                }

                if (i != 0)
                {
                    sb.Append('\t');
                }

                sb.Append(args.AsStringUsingMeta(executionContext, i, "print"));
            }

            executionContext.Script.Options.DebugPrint(sb.ToString());

            return DynValue.Nil;
        }

        /// <summary>
        /// Implements Lua 5.4's <c>warn</c> helper by routing formatted arguments to <c>_WARN</c> or the debug printer.
        /// </summary>
        /// <param name="executionContext">Execution context used to access the host script and debug sink.</param>
        /// <param name="args">Arguments to format before invoking <c>_WARN</c> or printing.</param>
        /// <returns><see cref="DynValue.Nil"/>, matching Lua's return contract.</returns>
        [LuaCompatibility(LuaCompatibilityVersion.Lua54)]
        [NovaSharpModuleMethod(Name = "warn")]
        public static DynValue Warn(ScriptExecutionContext executionContext, CallbackArguments args)
        {
            executionContext = ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

            StringBuilder sb = new();

            for (int i = 0; i < args.Count; i++)
            {
                if (i != 0)
                {
                    sb.Append('\t');
                }

                sb.Append(args.AsStringUsingMeta(executionContext, i, "warn"));
            }

            string payload = sb.ToString();
            Script script = executionContext.Script;
            DynValue warnHandler = script.Globals.RawGet("_WARN");

            if (
                warnHandler != null
                && (
                    warnHandler.Type == DataType.Function
                    || warnHandler.Type == DataType.ClrFunction
                )
            )
            {
                script.Call(warnHandler, DynValue.NewString(payload));
            }
            else
            {
                Action<string> sink = script.Options.DebugPrint;

                if (sink != null)
                {
                    sink(payload);
                }
                else
                {
                    Console.Error.WriteLine(payload);
                }
            }

            return DynValue.Nil;
        }
    }
}
