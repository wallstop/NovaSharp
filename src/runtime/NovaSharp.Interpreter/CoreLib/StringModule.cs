// Disable warnings about XML documentation
namespace NovaSharp.Interpreter.CoreLib
{
#pragma warning disable 1591

    using System;
    using System.IO;
    using System.Text;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.CoreLib.StringLib;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Execution;
    using NovaSharp.Interpreter.Interop.Attributes;
    using NovaSharp.Interpreter.LuaPort;
    using NovaSharp.Interpreter.Modules;

    /// <summary>
    /// Implements Lua's `string` library (§6.4), providing formatting, pattern matching, and utility helpers.
    /// </summary>
    [NovaSharpModule(Namespace = "string")]
    public static class StringModule
    {
        public const string Base64DumpHeader = "NovaSharp_dump_b64::";

        /// <summary>
        /// Registers the `string` metatable so literal strings inherit the library functions.
        /// </summary>
        /// <param name="globalTable">Global table provided by the module host.</param>
        /// <param name="stringTable">Library table containing the exported functions.</param>
        public static void NovaSharpInit(Table globalTable, Table stringTable)
        {
            globalTable = ModuleArgumentValidation.RequireTable(globalTable, nameof(globalTable));
            stringTable = ModuleArgumentValidation.RequireTable(stringTable, nameof(stringTable));
            Table stringMetatable = new(globalTable.OwnerScript);
            stringMetatable.Set("__index", DynValue.NewTable(stringTable));
            globalTable.OwnerScript.SetTypeMetatable(DataType.String, stringMetatable);
        }

        /// <summary>
        /// Implements Lua `string.dump`, returning a base64-encoded binary chunk for a function (§6.4.2).
        /// </summary>
        /// <param name="executionContext">Current script execution context.</param>
        /// <param name="args">Arguments supplying the function to dump.</param>
        /// <returns>Serialized chunk as a string.</returns>
        [NovaSharpModuleMethod(Name = "dump")]
        public static DynValue Dump(ScriptExecutionContext executionContext, CallbackArguments args)
        {
            executionContext = ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));
            DynValue fn = args.AsType(0, "dump", DataType.Function, false);

            try
            {
                byte[] bytes;
                using (MemoryStream ms = new())
                {
                    executionContext.Script.Dump(fn, ms);
                    ms.Seek(0, SeekOrigin.Begin);
                    bytes = ms.ToArray();
                }
                string base64 = Convert.ToBase64String(bytes);
                return DynValue.NewString(Base64DumpHeader + base64);
            }
            catch (Exception ex)
            {
                throw new ScriptRuntimeException(ex.Message);
            }
        }

        /// <summary>
        /// Implements Lua `string.char`, converting numeric code-points into a string (§6.4.1).
        /// </summary>
        /// <param name="executionContext">Current script execution context.</param>
        /// <param name="args">Numeric or numeric-string arguments.</param>
        /// <returns>Concatenated characters.</returns>
        [NovaSharpModuleMethod(Name = "char")]
        public static DynValue CharFunction(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));
            StringBuilder sb = new(args.Count);

            for (int i = 0; i < args.Count; i++)
            {
                DynValue v = args[i];
                double d = 0d;

                if (v.Type == DataType.String)
                {
                    double? nullableNumber = v.CastToNumber();
                    if (nullableNumber == null)
                    {
                        args.AsType(i, "char", DataType.Number, false);
                    }
                    else
                    {
                        d = nullableNumber.Value;
                    }
                }
                else
                {
                    args.AsType(i, "char", DataType.Number, false);
                    d = v.Number;
                }

                int normalized = NormalizeByte(Math.Floor(d));
                sb.Append((char)normalized);
            }

            return DynValue.NewString(sb.ToString());
        }

        /// <summary>
        /// Implements Lua `string.byte`, returning byte values for a string slice (§6.4.1).
        /// </summary>
        /// <param name="executionContext">Current script execution context.</param>
        /// <param name="args">String plus optional start/end indices.</param>
        /// <returns>Tuple of byte values.</returns>
        [NovaSharpModuleMethod(Name = "byte")]
        public static DynValue Byte(ScriptExecutionContext executionContext, CallbackArguments args)
        {
            ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));
            DynValue vs = args.AsType(0, "byte", DataType.String, false);
            DynValue vi = args.AsType(1, "byte", DataType.Number, true);
            DynValue vj = args.AsType(2, "byte", DataType.Number, true);

            return PerformByteLike(vs, vi, vj, i => NormalizeByte(i));
        }

        /// <summary>
        /// NovaSharp extension mirroring `string.byte` but returning the raw Unicode code points.
        /// </summary>
        /// <param name="executionContext">Current script execution context.</param>
        /// <param name="args">String plus optional range.</param>
        /// <returns>Tuple of code points.</returns>
        [NovaSharpModuleMethod(Name = "unicode")]
        public static DynValue Unicode(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));
            DynValue vs = args.AsType(0, "unicode", DataType.String, false);
            DynValue vi = args.AsType(1, "unicode", DataType.Number, true);
            DynValue vj = args.AsType(2, "unicode", DataType.Number, true);

            return PerformByteLike(vs, vi, vj, i => i);
        }

        private static DynValue PerformByteLike(
            DynValue vs,
            DynValue vi,
            DynValue vj,
            Func<int, int> filter
        )
        {
            StringRange range = StringRange.FromLuaRange(vi, vj, null);
            string s = range.ApplyToString(vs.String);

            int length = s.Length;
            DynValue[] rets = new DynValue[length];

            if (length == 0)
            {
                return DynValue.Void;
            }

            for (int i = 0; i < length; ++i)
            {
                rets[i] = DynValue.NewNumber(filter((int)s[i]));
            }

            return DynValue.NewTuple(rets);
        }

        private static int NormalizeByte(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                return 0;
            }

            long truncated = (long)Math.Floor(value);
            int normalized = (int)(truncated % 256);
            if (normalized < 0)
            {
                normalized += 256;
            }

            return normalized;
        }

        /// <summary>
        /// Normalizes Lua substring indices (1-based/negative) into zero-based offsets.
        /// </summary>
        private static int? AdjustIndex(string s, DynValue vi, int defval)
        {
            if (vi.IsNil())
            {
                return defval;
            }

            int i = (int)Math.Round(vi.Number, 0);

            if (i == 0)
            {
                return null;
            }

            if (i > 0)
            {
                return i - 1;
            }

            return s.Length - i;
        }

        /// <summary>
        /// Internal helpers exposed to tests so Lua index normalization can be validated without duplicating logic.
        /// </summary>
        internal static class TestHooks
        {
            /// <summary>
            /// Mirrors <see cref="AdjustIndex"/> for unit tests.
            /// </summary>
            public static int? AdjustIndex(string s, DynValue vi, int defaultValue)
            {
                return StringModule.AdjustIndex(s, vi, defaultValue);
            }
        }

        /// <summary>
        /// Implements Lua `string.len`, returning the number of bytes in the string (§6.4.1).
        /// </summary>
        [NovaSharpModuleMethod(Name = "len")]
        public static DynValue Len(ScriptExecutionContext executionContext, CallbackArguments args)
        {
            ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));
            DynValue vs = args.AsType(0, "len", DataType.String, false);
            return DynValue.NewNumber(vs.String.Length);
        }

        /// <summary>
        /// Implements Lua `string.match`, returning captures for the first pattern match (§6.4.1).
        /// </summary>
        [NovaSharpModuleMethod(Name = "match")]
        public static DynValue Match(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));
            return executionContext.EmulateClassicCall(args, "match", KopiLuaStringLib.str_match);
        }

        /// <summary>
        /// Implements Lua `string.gmatch`, returning an iterator over pattern matches (§6.4.1).
        /// </summary>
        [NovaSharpModuleMethod(Name = "gmatch")]
        public static DynValue GMatch(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));
            return executionContext.EmulateClassicCall(args, "gmatch", KopiLuaStringLib.str_gmatch);
        }

        /// <summary>
        /// Implements Lua `string.gsub`, performing pattern substitution (§6.4.1).
        /// </summary>
        [NovaSharpModuleMethod(Name = "gsub")]
        public static DynValue GSub(ScriptExecutionContext executionContext, CallbackArguments args)
        {
            ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));
            return executionContext.EmulateClassicCall(args, "gsub", KopiLuaStringLib.str_gsub);
        }

        /// <summary>
        /// Implements Lua `string.find`, locating the first pattern occurrence (§6.4.1).
        /// </summary>
        [NovaSharpModuleMethod(Name = "find")]
        public static DynValue Find(ScriptExecutionContext executionContext, CallbackArguments args)
        {
            ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));
            return executionContext.EmulateClassicCall(args, "find", KopiLuaStringLib.str_find);
        }

        /// <summary>
        /// Implements Lua `string.lower`, converting characters to lower-case (§6.4.1).
        /// </summary>
        [NovaSharpModuleMethod(Name = "lower")]
        public static DynValue Lower(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));
            DynValue argS = args.AsType(0, "lower", DataType.String, false);
            return DynValue.NewString(InvariantString.ToLowerInvariantIfNeeded(argS.String));
        }

        /// <summary>
        /// Implements Lua `string.upper`, converting characters to upper-case (§6.4.1).
        /// </summary>
        [NovaSharpModuleMethod(Name = "upper")]
        public static DynValue Upper(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));
            DynValue argS = args.AsType(0, "upper", DataType.String, false);
            return DynValue.NewString(InvariantString.ToUpperInvariantIfNeeded(argS.String));
        }

        /// <summary>
        /// Implements Lua `string.rep`, repeating a string N times with an optional separator (§6.4.1).
        /// </summary>
        [NovaSharpModuleMethod(Name = "rep")]
        public static DynValue Rep(ScriptExecutionContext executionContext, CallbackArguments args)
        {
            ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));
            DynValue argS = args.AsType(0, "rep", DataType.String, false);
            DynValue argN = args.AsType(1, "rep", DataType.Number, false);
            DynValue argSep = args.AsType(2, "rep", DataType.String, true);

            if (String.IsNullOrEmpty(argS.String) || (argN.Number < 1))
            {
                return DynValue.NewString("");
            }

            string sep = (argSep.IsNotNil()) ? argSep.String : null;

            int count = (int)argN.Number;
            StringBuilder result = new(argS.String.Length * count);

            for (int i = 0; i < count; ++i)
            {
                if (i != 0 && sep != null)
                {
                    result.Append(sep);
                }

                result.Append(argS.String);
            }

            return DynValue.NewString(result.ToString());
        }

        /// <summary>
        /// Implements Lua `string.format`, producing formatted strings (printf-style) (§6.4.1).
        /// </summary>
        [NovaSharpModuleMethod(Name = "format")]
        public static DynValue Format(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));
            return executionContext.EmulateClassicCall(args, "format", KopiLuaStringLib.str_format);
        }

        /// <summary>
        /// Implements Lua `string.reverse`, reversing byte order (§6.4.1).
        /// </summary>
        [NovaSharpModuleMethod(Name = "reverse")]
        public static DynValue Reverse(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));
            DynValue argS = args.AsType(0, "reverse", DataType.String, false);

            if (String.IsNullOrEmpty(argS.String))
            {
                return DynValue.NewString("");
            }

            char[] elements = argS.String.ToCharArray();
            Array.Reverse(elements);

            return DynValue.NewString(new String(elements));
        }

        /// <summary>
        /// Implements Lua `string.sub`, returning a substring defined by Lua indices (§6.4.1).
        /// </summary>
        [NovaSharpModuleMethod(Name = "sub")]
        public static DynValue Sub(ScriptExecutionContext executionContext, CallbackArguments args)
        {
            ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));
            DynValue argS = args.AsType(0, "sub", DataType.String, false);
            DynValue argI = args.AsType(1, "sub", DataType.Number, true);
            DynValue argJ = args.AsType(2, "sub", DataType.Number, true);

            StringRange range = StringRange.FromLuaRange(argI, argJ, -1);
            string s = range.ApplyToString(argS.String);

            return DynValue.NewString(s);
        }

        /// <summary>
        /// NovaSharp helper: returns whether a string begins with the provided prefix (ordinal comparison).
        /// </summary>
        [NovaSharpModuleMethod(Name = "startswith")]
        public static DynValue StartsWith(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));
            DynValue argS1 = args.AsType(0, "startsWith", DataType.String, true);
            DynValue argS2 = args.AsType(1, "startsWith", DataType.String, true);

            if (argS1.IsNil() || argS2.IsNil())
            {
                return DynValue.False;
            }

            return DynValue.NewBoolean(
                argS1.String.StartsWith(argS2.String, StringComparison.Ordinal)
            );
        }

        /// <summary>
        /// NovaSharp helper: returns whether a string ends with the provided suffix (ordinal comparison).
        /// </summary>
        [NovaSharpModuleMethod(Name = "endswith")]
        public static DynValue EndsWith(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));
            DynValue argS1 = args.AsType(0, "endsWith", DataType.String, true);
            DynValue argS2 = args.AsType(1, "endsWith", DataType.String, true);

            if (argS1.IsNil() || argS2.IsNil())
            {
                return DynValue.False;
            }

            return DynValue.NewBoolean(
                argS1.String.EndsWith(argS2.String, StringComparison.Ordinal)
            );
        }

        /// <summary>
        /// NovaSharp helper: returns whether a string contains the provided substring (ordinal comparison).
        /// </summary>
        [NovaSharpModuleMethod(Name = "contains")]
        public static DynValue Contains(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));
            DynValue argS1 = args.AsType(0, "contains", DataType.String, true);
            DynValue argS2 = args.AsType(1, "contains", DataType.String, true);

            if (argS1.IsNil() || argS2.IsNil())
            {
                return DynValue.False;
            }

            return DynValue.NewBoolean(
                argS1.String.Contains(argS2.String, StringComparison.Ordinal)
            );
        }
    }
}
